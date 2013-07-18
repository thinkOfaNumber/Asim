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

namespace PWC.Asim.Algorithms.PvSimple
{
    public class SolarController
    {
        public double Control(double pvAvailP, double lastSetP,
            double genP, double genSpinP, double genIdealP,
            double loadP, double statSpinSetP, double switchDownP)
        {
            // calculate desired setpoint
            return Math.Max(0, statSpinSetP);
        }
    }
}
