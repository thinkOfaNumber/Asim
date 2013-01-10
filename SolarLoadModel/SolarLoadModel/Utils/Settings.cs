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

namespace SolarLoadModel.Utils
{
    static public class Settings
    {
        public const int MAX_GENS = 8;
        public const int MAX_CFG = 1 << MAX_GENS;
        public const int FuelCurvePoints = 5;
        public const double PerHourToSec = 1 / (60.0 * 60.0);
        public const int SecondsInAMinute = 60 * 60;
        public const int SecondsInAnHour  = 60 * SecondsInAMinute;
        public const int SecondsInADay    = 24 * SecondsInAnHour;
        public const int SecondsInAYear   = 365 * SecondsInADay;
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
    }

    public enum DateFormat
    {
        RelativeToEpoch,
        RelativeToSim,
        Other
    }

    public enum ExitCode
    {
        Success = 0,
        Failure = 1
    }
}
