﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolarLoadModel.Utils
{
    static public class Settings
    {
        public const int MAX_GENS = 8;
        public const int MAX_CFG = 256;
        public const double PerHourToSec = 1 / (60.0 * 60.0);
    }
}
