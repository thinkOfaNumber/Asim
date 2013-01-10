﻿// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of the Solar Load Model - A Renewable Energy Power Station
// Control System Simulator
//
// The Solar Load Model is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Foobar is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Foobar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly Shared _pvECnt = SharedContainer.GetOrNew("PvECnt");
        private readonly Shared _pvSetMaxDownP = SharedContainer.GetOrNew("PvSetMaxDownP");
        private readonly Shared _pvSetMaxUpP = SharedContainer.GetOrNew("PvSetMaxUpP");

        private readonly Shared _statSpinP = SharedContainer.GetOrNew("StatSpinP");
        private readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private readonly Shared _genIdealP = SharedContainer.GetOrNew("GenIdealP");

        private double _deltaSetP = 0;

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            // calculate desired setpoint
            _deltaSetP = Math.Min(_statSpinP.Val, _genP.Val - _genIdealP.Val);

            if (_deltaSetP > 0)
            {
                _deltaSetP = Math.Min(_deltaSetP, _pvSetMaxUpP.Val);
            }
            if (_deltaSetP < 0)
            {
                _deltaSetP = Math.Max(_deltaSetP, -_pvSetMaxDownP.Val);
            }

            // apply ramp limited setpoint
            _pvSetP.Val = Math.Max(0, _pvSetP.Val + _deltaSetP);
            // limit setpoint to available solar power
            _pvSetP.Val = Math.Min(_pvSetP.Val, _pvAvailP.Val);

            // assume solar farm outputs this immediatly
            _pvP.Val = _pvSetP.Val;

            // calculate spill
            _pvSpillP.Val = _pvAvailP.Val - _pvP.Val;

            // calculate energy
            _pvECnt.Val += (_pvP.Val * Settings.PerHourToSec);
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
