﻿// Copyright (C) 2012, 2013  Power Water Corporation
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
using PWC.Asim.Sim.Contracts;
using PWC.Asim.Sim.Utils;

namespace PWC.Asim.Sim.Actors
{
    /// <summary>
    /// The Load class manages changes to station load due to the maximum load
    /// limit (LoadMaxLimP) and sheddable load online.  The simulator load
    /// is LoadSetP.
    /// </summary>
    class Load : IActor
    {
        private readonly Shared _loadMaxLimP = SharedContainer.GetOrNew("LoadMaxLimP");
        private readonly Shared _loadP = SharedContainer.GetOrNew("LoadP");
        private readonly Shared _loadSetP = SharedContainer.GetOrNew("LoadSetP");
        private readonly Shared _shedOffP = SharedContainer.GetOrNew("ShedOffP");

        public void Init()
        {
            
        }

        public void Run(ulong iteration)
        {
            // limit load based on LoadMaxLimP
            double realLoadP = _loadMaxLimP.Val > 0 ? Math.Min(_loadP.Val, _loadMaxLimP.Val) : _loadP.Val;

            // simulate sheddable load switching off by substracting offline sheddable load component
            realLoadP -= _shedOffP.Val;

            _loadSetP.Val = realLoadP;
        }

        public void Finish()
        {
        }
    }
}