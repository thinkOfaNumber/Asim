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

namespace SolarLoadModel.Contracts
{
    public interface IDispatchLoad
    {
        /// <summary>
        /// Total / rated dispatchable load
        /// </summary>
        double DisLoadP { get; }
        /// <summary>
        /// Online dispatchable load
        /// </summary>
        double DisP { get; }
        /// <summary>
        /// Offline portion of dispatchable load
        /// </summary>
        double DisOffP { get; }
        /// <summary>
        /// Proportion of load that can be switched off soon
        /// </summary>
        double DisSpinP { get; }

        void Run();
        void Stop();
        void Start();
    }
}
