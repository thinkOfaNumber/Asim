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
}
