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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PWC.Asim.Sim.Contracts;
using PWC.Asim.Sim.Utils;

namespace PWC.Asim.Sim.Actors
{
    class ScaleValues : IActor
    {
        private List<Shared> _toScale;

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            if (_toScale == null)
            {
                _toScale = new List<Shared>();
                var names = SharedContainer.GetAllNames();
                foreach (string name in names)
                {
                    var s = SharedContainer.GetExisting(name);
                    if (s.ScaleFunction != null)
                    {
                        _toScale.Add(s);
                    }
                }
            }

            foreach (var shared in _toScale)
            {
                if (shared.ScaleFunction != null)
                {
                    shared.Val = shared.ScaleFunction(shared.Val);
                    shared.ScaleFunction = null;
                }
            }
        }

        public void Init()
        {
        }

        public void Finish()
        {
        }

        #endregion
    }
}
