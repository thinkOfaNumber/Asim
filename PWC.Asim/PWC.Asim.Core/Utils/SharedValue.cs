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
        private double? _multiplier;
        private double? _offset;

        /// <summary>
        /// Set the multiplier and offset values for scaling this value when called by the
        /// SetWithScale() function.
        /// </summary>
        /// <param name="multiplier">multiplier</param>
        /// <param name="offset">offset</param>
        public void ScaleFunction(double? multiplier, double? offset)
        {
            if (multiplier.HasValue && offset.HasValue)
            {
                if (_multiplier.HasValue && _offset.HasValue)
                    // undo old scale
                    _val = (_val - _offset.Value)/_multiplier.Value;

                // apply new scale
                _val = (_val * multiplier.Value) + offset.Value;
                _multiplier = multiplier;
                _offset = offset;
            }
            else
            {
                _multiplier = null;
                _offset = null;
            }
        }

        /// <summary>
        /// An action method which is called before the internal value is set,
        /// only when a variable changes value, with the definition:
        /// void Method(double oldVal, double newVal);
        /// </summary>
        public event EventHandler<SharedEventArgs> OnValueChanged;

        private double _val;

        /// <summary>
        /// The setter & getter for the value of this Shared object.
        /// </summary>
        public double Val
        {
            get { return _val; }
            set
            {
                ValueChangedEvent(value);
                _val = value;
            }
        }

        /// <summary>
        /// This function should generally only be called from the input data file loader, where
        /// any input data should be scaled.  All normal set operations shouldn't keep scaling the
        /// same value and may use the Val property.
        /// </summary>
        /// <param name="value">value to set according to ScaleFunction</param>
        public void SetWithScale(double value)
        {
            ValueChangedEvent(value);
            if (_multiplier.HasValue && _offset.HasValue)
                _val = (value*_multiplier.Value) + _offset.Value;
            else
                _val = value;
        }

        private void ValueChangedEvent(double newValue)
        {
            if (OnValueChanged != null && _val != newValue)
            {
                OnValueChanged(this, new SharedEventArgs { OldValue = _val, NewValue = newValue });
            }
        }

        internal string Name;
    }

    public sealed class SharedContainer
    {
        // singleton
        private static readonly SharedContainer instance = new SharedContainer();
        private SharedContainer() { }

        /// <summary>
        /// Use this property to get an instance of the SharedContainer singleton for creating
        /// and accessing Shared values.
        /// </summary>
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
            // ReSharper disable once TooWideLocalVariableScope
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
