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
    /// <summary>
    /// The Load class manages changes to station load due to the maximum load
    /// limit (LoadMaxLimP) and sheddable load online.  The simulator load
    /// is LoadSetP.
    /// </summary>
    public class Load : IActor
    {
        private readonly SharedContainer _sharedVars = SharedContainer.Instance;
        private readonly Shared _loadMaxLimP;
        private readonly Shared _loadP;
        private readonly Shared _loadSetP;
        private readonly Shared _shedOffP;
        private readonly Shared _loadMaxUpP;
        private readonly Shared _loadMaxDownP;
        private readonly Shared _battP;
        private double _oldLoadP;
        private double _deltaLoadP;
        private double _simLoadP;

        public Load()
        {
            _loadMaxLimP = _sharedVars.GetOrNew("LoadMaxLimP");
            _loadP = _sharedVars.GetOrNew("LoadP");
            _loadSetP = _sharedVars.GetOrNew("LoadSetP");
            _shedOffP = _sharedVars.GetOrNew("ShedOffP");
            _loadMaxUpP = _sharedVars.GetOrNew("LoadMaxUpP");
            _loadMaxDownP = _sharedVars.GetOrNew("LoadMaxDownP");
            _battP = _sharedVars.GetOrNew("BattP");
        }

        public void Init() { }

        public void Read(ulong iteration) { }

        public void Write(ulong iteration) { }

        public void Run(ulong iteration)
        {
            // limit load based on LoadMaxLimP
            _simLoadP = _loadMaxLimP.Val > 0 ? Math.Min(_loadP.Val, _loadMaxLimP.Val) : _loadP.Val;

            // limit load rate of change
            _deltaLoadP = _simLoadP - _oldLoadP;
            if (_loadMaxUpP.Val > 0 && _deltaLoadP > _loadMaxUpP.Val)
            {
                _deltaLoadP = _loadMaxUpP.Val;
            }
            if (_loadMaxDownP.Val > 0 && -_deltaLoadP > _loadMaxDownP.Val)
            {
                _deltaLoadP = -_loadMaxDownP.Val;
            }
            _simLoadP = _oldLoadP = _oldLoadP + _deltaLoadP;

            // simulate sheddable load switching off by substracting offline sheddable load component
            _simLoadP -= _shedOffP.Val;

            // simulate battery in / out
            //_simLoadP -= _battP.Val;

            _loadSetP.Val = _simLoadP;
        }

        public void Finish()
        {
        }
    }
}
