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

namespace PWC.Asim.Sim.Contracts
{
    public interface ISheddableLoad
    {
        /// <summary>
        /// Total / rated sheddable load
        /// </summary>
        double ShedLoadP { get; }
        /// <summary>
        /// Online sheddable load
        /// </summary>
        double ShedP { get; }
        /// <summary>
        /// Offline portion of sheddable load
        /// </summary>
        double ShedOffP { get; }
        /// <summary>
        /// Proportion of load that can be switched off soon
        /// </summary>
        double ShedSpinP { get; }

        void Run(ulong iteration);
        void Stop();
        void Start();
    }
}
