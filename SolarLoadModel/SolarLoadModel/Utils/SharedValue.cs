using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SolarLoadModel.Utils
{
    public class Shared
    {
        public double Val { get; set; }
    }

    public class SharedContainer
    {
        private static readonly SortedDictionary<string, Shared> SharedValues = new SortedDictionary<string, Shared>();

        public static Shared GetOrNew(string name)
        {
            Shared s;
            if (!SharedValues.TryGetValue(name, out s))
            {
                s = new Shared();
                SharedValues[name] = s;
            }
            return s;
        }

        public static Shared GetExisting(string name)
        {
            return SharedValues[name];
        }

        public static IList<string> GetAllNames()
        {
            return new List<string>(SharedValues.Keys);
        }
    }
}
