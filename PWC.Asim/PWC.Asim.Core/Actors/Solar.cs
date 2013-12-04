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
    class Solar : IActor
    {
        private readonly SharedContainer _sharedVars = SharedContainer.Instance;
        private readonly Shared _pvP;
        private readonly Shared _pvAvailP;
        private readonly Shared _pvSetP;
        private readonly Shared _pvSpillP;
        private readonly Shared _pvE;
        private readonly Shared _pvAvailE;
        private readonly Shared _pvSpillE;
        private readonly Shared _pvSetMaxDownP;
        private readonly Shared _pvSetMaxUpP;
        private readonly Shared _pvMaxLimP;
        private readonly Shared _pvSetLimitSpinPct;
        private readonly Shared _pvSetLimitSpinpPct;

        private readonly Shared _loadP;
        private readonly Shared _statSpinSetP;
        private readonly Shared _statBlack;
        private readonly Shared _statHystP;
        private readonly Shared _genP;
        private readonly Shared _genIdealP;
        private readonly Shared _genSpinP;
        private readonly Shared _genLowP;
        private readonly Shared _battP;

        readonly Delegates.SolarController _solarController;

        public Solar(Delegate solarController)
        {
            _solarController = (Delegates.SolarController)solarController;
            _pvP = _sharedVars.GetOrNew("PvP");
            _pvAvailP = _sharedVars.GetOrNew("PvAvailP");
            _pvSetP = _sharedVars.GetOrNew("PvSetP");
            _pvSpillP = _sharedVars.GetOrNew("PvSpillP");
            _pvE = _sharedVars.GetOrNew("PvE");
            _pvAvailE = _sharedVars.GetOrNew("PvAvailE");
            _pvSpillE = _sharedVars.GetOrNew("PvSpillE");
            _pvSetMaxDownP = _sharedVars.GetOrNew("PvSetMaxDownP");
            _pvSetMaxUpP = _sharedVars.GetOrNew("PvSetMaxUpP");
            _pvMaxLimP = _sharedVars.GetOrNew("PvMaxLimP");
            _pvSetLimitSpinPct = _sharedVars.GetOrNew("PvSetLimitSpinPct");
            _pvSetLimitSpinpPct = _sharedVars.GetOrNew("PvSetLimitSpinpPct");

            _loadP = _sharedVars.GetOrNew("LoadSetP");
            _statSpinSetP = _sharedVars.GetOrNew("StatSpinSetP");
            _statBlack = _sharedVars.GetOrNew("StatBlack");
            _statHystP = _sharedVars.GetOrNew("StatHystP");
            _genP = _sharedVars.GetOrNew("GenP");
            _genIdealP = _sharedVars.GetOrNew("GenIdealP");
            _genSpinP = _sharedVars.GetOrNew("GenSpinP");
            _genLowP = _sharedVars.GetOrNew("GenLowP");
            _battP = _sharedVars.GetOrNew("BattP");
        }

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            if (_pvMaxLimP.Val > 0)
                _pvAvailP.Val = Math.Min(_pvAvailP.Val, _pvMaxLimP.Val);

            // calculate desired setpoint
            double setP = _solarController(_pvAvailP.Val, _pvSetP.Val,
                _genP.Val, _genSpinP.Val,
                _genIdealP.Val, _loadP.Val - _battP.Val, _statSpinSetP.Val,
                // magic number 5 is to prevent switching on the "fence line"
                Math.Max(0, _genLowP.Val - _statHystP.Val - _statSpinSetP.Val + 5.0D));

            // apply Spinning reserve limits (PWCSLMS-41)
            if (_pvSetLimitSpinPct.Val > 0 && _pvSetLimitSpinPct.Val <= 100)
            {
                // todo: adding an offset of 5 to the GenSpinP limit here smooths the behaviour a bit but it's not an ideal solution
                setP = Math.Min(setP, _genSpinP.Val * _pvSetLimitSpinPct.Val * Settings.Percent);
            }
            if (_pvSetLimitSpinpPct.Val > 0 && _pvSetLimitSpinpPct.Val <= 100)
            {
                setP = Math.Min(setP, _statSpinSetP.Val * _pvSetLimitSpinpPct.Val * Settings.Percent);
            }

            // calculate delta and ramp limits
            double deltaSetP = setP - _pvP.Val;
            if (deltaSetP > 0 && _pvSetMaxUpP.Val != 0)
            {
                deltaSetP = Math.Min(deltaSetP, _pvSetMaxUpP.Val);
            }
            if (deltaSetP < 0 && _pvSetMaxDownP.Val != 0)
            {
                deltaSetP = Math.Max(deltaSetP, -_pvSetMaxDownP.Val);
            }

            // apply ramp limited setpoint
            setP = Math.Max(0, _pvP.Val + deltaSetP);
            // limit setpoint to available solar power
            setP = Math.Min(setP, _pvAvailP.Val);
            // solar trips off in case of a black station
            if (_statBlack.Val > 0)
                setP = 0;

            // assume solar farm outputs this immediatly
            _pvP.Val = _pvSetP.Val = setP;

            // calculate spill
            _pvSpillP.Val = _pvAvailP.Val - _pvP.Val;

            // calculate energy
            _pvE.Val += (_pvP.Val * Settings.PerHourToSec);
            _pvAvailE.Val += (_pvAvailP.Val * Settings.PerHourToSec);
            _pvSpillE.Val += (_pvSpillP.Val * Settings.PerHourToSec);
        }

        public void Init()
        {
            _pvSetP.Val = 0;
        }

        public void Finish()
        {

        }

        #endregion

        public static double DefaultSolarController(double pvAvailP, double lastSetP,
            double genP, double genSpinP, double genIdealP,
            double loadP, double statSpinSetP, double switchDownP)
        {
            // calculate desired setpoint.  Note this won't share with other
            // energy providers.
            double setP = Math.Max(0, loadP - genIdealP);

            // limit setpoint to total station load
            return Math.Min(setP, loadP);
        }
    }
}
