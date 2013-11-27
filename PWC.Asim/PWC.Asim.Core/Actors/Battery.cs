using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PWC.Asim.Core.Contracts;
using PWC.Asim.Core.Utils;

namespace PWC.Asim.Core.Actors
{
    public enum BatteryState
    {
        Charging = 1,
        CanDischarge = 2
    }
    public class Battery : IActor
    {
        private readonly SharedContainer _sharedVars = SharedContainer.Instance;

        private readonly Shared _battMaxE; // high charge level at which to be able to provide diesel off
        private readonly Shared _battMinE; // low charge level at which to require diesel to recharge
        private readonly Shared _battE; // current usable energy in the battery
        private readonly Shared _battMaxP; // maximum output power
        private readonly Shared _battMinP; // maximum recharge power
        private readonly Shared _battEfficiencyPct; // battery charging efficency in percent
        private readonly Shared _battSetP; // the current setpoint for the battery
        private readonly Shared _battImportedE; // imported energy counter
        private readonly Shared _battExportedE; // export energy counter
        private readonly Shared _battP; // current battery power (+ve export, -ve import)
        private readonly Shared _battRechargeSetP; // setpoint to charge at
        private readonly Shared _battSt; // current State (1=Charging, 2=Discharging)
        private BatteryState _batteryState = BatteryState.Charging;

        private readonly Shared _genSpinP;
        private readonly Shared _pvSpillP;

        public Battery()
        {
            _battMaxE = _sharedVars.GetOrNew("BattMaxE");
            _battMinE = _sharedVars.GetOrNew("BattMinE");
            _battE = _sharedVars.GetOrNew("BattE");
            _battMaxP = _sharedVars.GetOrNew("BattMaxP");
            _battMinP = _sharedVars.GetOrNew("BattMinP");
            _battEfficiencyPct = _sharedVars.GetOrNew("BattEfficiencyPct");
            _battSetP = _sharedVars.GetOrNew("BattSetP");
            _battImportedE = _sharedVars.GetOrNew("BattImportedE");
            _battExportedE = _sharedVars.GetOrNew("BattExportedE");
            _battP = _sharedVars.GetOrNew("BattP");
            _battSt = _sharedVars.GetOrNew("BattSt");
            _battRechargeSetP = _sharedVars.GetOrNew("BattRechargeSetP");
            _genSpinP = _sharedVars.GetOrNew("GenSpinP");
            _pvSpillP = _sharedVars.GetOrNew("PvSpillP");
        }


        public void Init()
        {

        }

        public void Run(ulong iteration)
        {
            if (Station.IsBlack)
            {
                _batteryState = BatteryState.Charging;
                _battP.Val = 0;
                return;
            }

            _battSt.Val = Convert.ToDouble(_batteryState);

            double battP = Util.Limit(_battSetP.Val, _battMinP.Val, _battMaxP.Val); // user defined limits
            battP = Math.Min(battP, _battE.Val*Settings.SecondsInAnHour); // can't output more E than is stored
            battP = Math.Min(battP, _genSpinP.Val + _pvSpillP.Val); // limit to actual available power
            battP = Math.Min(battP, Math.Max(0, (_battMaxE.Val - _battE.Val) * Settings.SecondsInAnHour)); // limit to max capacity. todo: this doesn't account for effeciency < 100%

            _battE.Val += -battP * _battEfficiencyPct.Val * Settings.Percent * Settings.PerHourToSec;
            _battE.Val = Math.Max(0, _battE.Val); // E can't be negative

            if (battP >= 0)
            {
                _battExportedE.Val += battP * Settings.PerHourToSec;
            }
            else
            {
                _battImportedE.Val += -battP * Settings.PerHourToSec;
            }
            _battP.Val = battP;
            NextState();
        }

        private void NextState()
        {
            if (_batteryState == BatteryState.Charging && _battE.Val >= _battMaxE.Val)
            {
                _batteryState = BatteryState.CanDischarge;
            }
            else if (_batteryState == BatteryState.CanDischarge && _battE.Val <= _battMinE.Val)
            {
                _batteryState = BatteryState.Charging;
            }
        }

        public void Finish()
        {
        }

    }
}
