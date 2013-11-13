using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PWC.Asim.Core.Utils
{
    public static class Util
    {
        public static double Limit(double input, double min, double max)
        {
            if (min > max)
                throw new ArgumentOutOfRangeException("min");
            if (input < min)
                input = min;
            else if (input > max)
                input = max;
            return input;
        }
    }
}
