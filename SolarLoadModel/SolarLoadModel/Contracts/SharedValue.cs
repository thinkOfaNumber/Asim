using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SolarLoadModel.Contracts
{
    public class SharedValue
    {
        //private static readonly Dictionary<string, SharedValue> SharedValues = new Dictionary<string, SharedValue>();

        //private readonly SharedValue _thisRef;

        //public double Val {
        //    get
        //    {
        //        return _thisRef.Val;
        //    }
        //    set
        //    {
        //        _thisRef.Val = value;
        //    }
        //}
        //public string Name { get; private set; }

        //public SharedValue(string name)
        //{
        //    Name = name;
        //    if (!SharedValues.TryGetValue(name, out _thisRef))
        //    {
        //        _thisRef = new SharedValue();
        //        SharedValues[name] = _thisRef;
        //    }
        //}

        //private SharedValue()
        //{
            
        //}
        public double Val { get; set; }
    }
}
