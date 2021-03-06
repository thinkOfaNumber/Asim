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

namespace PWC.Asim.Core.Utils
{
    static public class Settings
    {
        public const int MAX_GENS = 8;
        public const int MAX_CFG = 1 << MAX_GENS;
        public const int FuelCurvePoints = 5;
        public const double PerHourToSec = 1.0D / (60.0D * 60.0D);
        public const int SecondsInAMinute = 60;
        public const int SecondsInAnHour  = 60 * SecondsInAMinute;
        public const int SecondsInADay    = 24 * SecondsInAnHour;
        public const int SecondsInAYear   = 365 * SecondsInADay;
        /// <summary>
        /// The Unix Epoch
        /// </summary>
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        /// <summary>
        /// Seconds to wait between starting and going online
        /// </summary>
        public const int GenStartStopDelay = 60;
        public const int MaxSvcIntervals = 6;
        /// <summary>
        /// Equivalent to (1 / 100) for faster percent calculation.  Multiply by this
        /// instead of dividing by 100.
        /// </summary>
        public const double Percent = 1.0D/100.0D;
        /// <summary>
        /// For the purposes of floating point comparision and "off" values, doubles
        /// less than this +/-value can be considered to be zero
        /// </summary>
        public const double Insignificant = 0.01;
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
