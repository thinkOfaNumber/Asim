// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of the Solar Load Model - A Renewable Energy Power Station
// Control System Simulator
//
// The Solar Load Model is free software: you can redistribute it and/or modify
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
using SolarLoadModel.Contracts;
using SolarLoadModel.Utils;

namespace SolarLoadModel.Actors
{
    class Solar : IActor
    {
        private readonly Shared _pvP = SharedContainer.GetOrNew("PvP");
        private readonly Shared _pvAvailP = SharedContainer.GetOrNew("PvAvailP");
        private readonly Shared _pvSetP = SharedContainer.GetOrNew("PvSetP");
        private readonly Shared _pvSpillP = SharedContainer.GetOrNew("PvSpillP");
        private readonly Shared _pvE = SharedContainer.GetOrNew("PvE");
        private readonly Shared _pvAvailE = SharedContainer.GetOrNew("PvAvailE");
        private readonly Shared _pvSpillE = SharedContainer.GetOrNew("PvSpillE");
        private readonly Shared _pvSetMaxDownP = SharedContainer.GetOrNew("PvSetMaxDownP");
        private readonly Shared _pvSetMaxUpP = SharedContainer.GetOrNew("PvSetMaxUpP");
        private readonly Shared _pvMaxLimP = SharedContainer.GetOrNew("PvMaxLimP");

        private readonly Shared _loadP = SharedContainer.GetOrNew("LoadP");
        private readonly Shared _statSpinP = SharedContainer.GetOrNew("StatSpinP");
        private readonly Shared _statBlack = SharedContainer.GetOrNew("StatBlack");
        private readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private readonly Shared _genIdealP = SharedContainer.GetOrNew("GenIdealP");
        private readonly Shared _genLowP = SharedContainer.GetOrNew("GenLowP");
        
        #region Implementation of IActor

        public void Read(ulong iteration)
        {
        }

        public void Run(ulong iteration)
        {
            if (_pvMaxLimP.Val > 0)
                _pvAvailP.Val = Math.Min(_pvAvailP.Val, _pvMaxLimP.Val);

            // calculate desired setpoint
            double setP;
            //if (_genLowP.Val != 0 && _loadP.Val - _pvAvailP.Val < _genLowP.Val)
            //    setP = _pvAvailP.Val;
            //else
                setP = Math.Max(0, _genP.Val - _genIdealP.Val);
            // limit setpoint to total station load
            setP = Math.Min(setP, _loadP.Val);

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
            _pvSetP.Val = Math.Max(0, _pvP.Val + deltaSetP);
            // limit setpoint to available solar power
            _pvSetP.Val = Math.Min(_pvSetP.Val, _pvAvailP.Val);
            // solar trips off in case of a black station
            if (_statBlack.Val > 0)
                _pvSetP.Val = 0;

            // assume solar farm outputs this immediatly
            _pvP.Val = _pvSetP.Val;

            // calculate spill
            _pvSpillP.Val = _pvAvailP.Val - _pvP.Val;

            // calculate energy
            _pvE.Val += (_pvP.Val * Settings.PerHourToSec);
            _pvAvailE.Val += (_pvAvailP.Val * Settings.PerHourToSec);
            _pvSpillE.Val += (_pvSpillP.Val * Settings.PerHourToSec);
        }

        public void Write(ulong iteration)
        {
        }

        public void Init()
        {
            _pvSetP.Val = 0;
        }

        public void Finish()
        {

        }

        #endregion
    }
}
