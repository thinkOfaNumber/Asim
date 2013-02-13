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

namespace SolarLoadModel.Contracts
{
    public class Delegates
    {
        /// <summary>
        /// The basic solar controller algorithm used to determine the setpoint
        /// for the next iteration.
        /// </summary>
        /// <param name="lastSetP">Last setP as output by this algorith, or 0 at the beginning.</param>
        /// <param name="genP">Actual generator output.</param>
        /// <param name="genIdealP">Ideal generator output for online generators.</param>
        /// <param name="loadP">Actual system load.</param>
        /// <returns>The new setpoint to feed to the ramp limit</returns>
        public delegate double SolarController(double lastSetP, double genP, double genIdealP, double loadP);
    }
}
