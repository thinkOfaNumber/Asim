// Copyright (C) 2012, 2013  Power Water Corporation
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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PWC.Asim.Core.Utils
{
    public class SharedEventArgs : EventArgs
    {
        public double OldValue { get; set; }
        public double NewValue { get; set; }
    }

    public class Shared
    {
        private Func<double, double> _scaleFunction;
        /// <summary>
        /// A function with the definition:
        /// double Func(double input);
        /// which is called when a scaling modifier is required to be applied
        /// to this value.
        /// </summary>
        public Func<double, double> ScaleFunction
        {
            set
            {
                _scaleFunction = value;
                _val = _scaleFunction == null ? _unscaled : _scaleFunction(_unscaled);
            }
        }

        /// <summary>
        /// An action method which is called before the internal value is set,
        /// only when a variable changes value, with the definition:
        /// void Method(double oldVal, double newVal);
        /// </summary>
        public event EventHandler<SharedEventArgs> OnValueChanged;
        private double _val;
        private double _unscaled;
        public double Val
        {
            get { return _val; }
            set
            {
                if (OnValueChanged != null && _unscaled != value)
                {
                    OnValueChanged(this, new SharedEventArgs { OldValue = _unscaled, NewValue = value });
                }
                _unscaled = value;
                _val = _scaleFunction == null ? _unscaled : _scaleFunction(_unscaled);
            }
        }

        internal string Name;

        public Shared()
        {
            ScaleFunction = null;
        }
    }

    public sealed class SharedContainer
    {
        // singleton
        private static readonly SharedContainer instance = new SharedContainer();
        private SharedContainer() { }
        public static SharedContainer Instance
        {
            get
            {
                return instance;
            }
        }

        private readonly SortedDictionary<string, Shared> _sharedValues = new SortedDictionary<string, Shared>();

        public Shared GetOrNew(string name)
        {
            Shared s;
            if (!_sharedValues.TryGetValue(name, out s))
            {
                s = new Shared {Name = name};
                _sharedValues[name] = s;
            }
            return s;
        }

        public Shared GetOrDefault(string name)
        {
            Shared s;
            _sharedValues.TryGetValue(name, out s);
            return s;
        }

        public Shared GetExisting(string name)
        {
            return _sharedValues[name];
        }

        public IList<string> GetAllNames()
        {
            return new List<string>(_sharedValues.Keys);
        }

        public List<string> MatchGlobs(string[] globs)
        {
            Regex regex;
            var varList = new List<string>();

            foreach (string glob in globs)
            {
                regex = new Regex("^" + glob.Replace("*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                varList.AddRange(_sharedValues.Keys.Where(var => regex.IsMatch(var)));
            }
            return varList;
        }
    }
}
