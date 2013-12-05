// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of "Asim" - A Renewable Energy Power Station
// Control System Simulator
//
// Asim is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
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

        private readonly Shared _battRatedE; // rated 100% charge level of battery
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
        private readonly Shared _battSt; // current State (1=Charging, 2=Discharging)
        private BatteryState _batteryState = BatteryState.Charging;

        private readonly Shared _genSpinP;
        private readonly Shared _pvAvailP;
        private readonly Shared _loadP;

        public Battery()
        {
            _battRatedE = _sharedVars.GetOrNew("BattRatedE");
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
            _genSpinP = _sharedVars.GetOrNew("GenSpinP");
            _pvAvailP = _sharedVars.GetOrNew("PvAvailP");
            _loadP = _sharedVars.GetOrNew("LoadSetP");
        }


        public void Init() { }

        public void Read(ulong iteration) { }

        public void Write(ulong iteration) { }

        public void Run(ulong iteration)
        {
            if (Station.IsBlack)
            {
                _batteryState = BatteryState.Charging;
                _battP.Val = 0;
                _battSt.Val = Convert.ToDouble(_batteryState);
                return;
            }

            _battSt.Val = Convert.ToDouble(_batteryState);

            double battP = Util.Limit(_battSetP.Val, _battMinP.Val, _battMaxP.Val); // user defined limits

            if (battP >= 0) // producing
            {
                battP = Math.Min(battP, _battE.Val * Settings.SecondsInAnHour); // can't output more E than is stored

                _battExportedE.Val += battP * Settings.PerHourToSec;
            }
            else // charging
            {
                battP = Math.Max(battP, -(_genSpinP.Val + _pvAvailP.Val - _loadP.Val)); // limit charge to actual available power
                battP = Math.Max(battP, -(_battRatedE.Val - _battE.Val) * Settings.SecondsInAnHour); // limit to max capacity. todo: this doesn't account for effeciency < 100%

                _battImportedE.Val += -battP * Settings.PerHourToSec;
            }
            _battE.Val += -battP * _battEfficiencyPct.Val * Settings.Percent * Settings.PerHourToSec;
            _battE.Val = Math.Max(0, _battE.Val); // E can't be negative
            _battP.Val = battP;
            NextState();
        }

        private void NextState()
        {
            if (_batteryState == BatteryState.Charging && _battE.Val > _battMaxE.Val)
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
