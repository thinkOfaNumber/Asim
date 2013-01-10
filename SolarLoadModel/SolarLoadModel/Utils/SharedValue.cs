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
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
