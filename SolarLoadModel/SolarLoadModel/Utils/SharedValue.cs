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
        public Func<double, double> ScaleFunction { get; set; }
        public double Val { get; set; }

        internal string Name;
        public Shared()
        {
            ScaleFunction = null;
        }

        //public static double operator +(Shared s1, Shared s2)
        //{
        //    return s1.Val + s2.Val;
        //}

        //public static double operator -(Shared s1, Shared s2)
        //{
        //    return s1.Val - s2.Val;
        //}

        //public static double operator *(Shared s1, Shared s2)
        //{
        //    return s1.Val * s2.Val;
        //}

        //public static double operator /(Shared s1, Shared s2)
        //{
        //    return s1.Val / s2.Val;
        //}
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
                s.Name = name;
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
