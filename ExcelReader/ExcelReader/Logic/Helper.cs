using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExcelReader.Logic
{
    public class Helper
    {
        public static bool IsFalse(string s)
        {
            var test = s.ToLower();
            return (test == "false" || test == "f" ||
                    test == "no" || test == "n" ||
                    test == "0");
        }
    }
}
