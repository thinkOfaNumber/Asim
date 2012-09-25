using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SolarLoadModel.Contracts;
using SolarLoadModel.Utils;

namespace SolarLoadModel.Actors
{
    public class OutputData : IActor
    {
        private readonly System.IO.StreamWriter _file;
        private readonly string _filename;
        private readonly string[] _varGlobs;
        private int _nvars;
        private double[] _min;
        private double[] _max;
        private double[] _ave;
        /// <summary>
        /// list of references to dictionary items
        /// </summary>
        private Shared[] _val;
        private StringBuilder _row;
        private readonly uint _outputEvery;
        private readonly bool _doStats;
        private bool _initStats = true;

        public OutputData(string filename, string[] vars, uint outputEvery = 1)
        {
            _filename = filename;
            try
            {
                _file = new System.IO.StreamWriter(_filename);
            }
            catch(Exception e)
            {
                throw new Exception("Could not write to file '" + _filename + "'. " + e.Message, e);
            }
            _varGlobs = vars;
            _outputEvery = outputEvery;
            _doStats = outputEvery > 1;
        }

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            // write output every "_outputEvery" samples, or always if that is 1.
            bool write = !_doStats || ((iteration % _outputEvery) == (_outputEvery - 1));

            if (_doStats)
            {
                for (int i = 0; i < _nvars; i++)
                {
                    if (_initStats)
                    {
                        _max[i] = _min[i] = _ave[i] = _val[i].Val;
                    }
                    else
                    {
                        _max[i] = Math.Max(_max[i], _val[i].Val);
                        _min[i] = Math.Min(_min[i], _val[i].Val);
                        // use ave to store the sum
                        _ave[i] = _ave[i] + _val[i].Val;
                    }
                }
                _initStats = false;
            }

            if (write)
            {
                _row.Clear();
                _row.Append(iteration);
                for (int i = 0; i < _nvars; i++)
                {
                    if (_doStats)
                    {
                        _ave[i] = _ave[i] / _outputEvery;
                        _row.Append(',');
                        _row.Append(_min[i]);
                        _row.Append(',');
                        _row.Append(_max[i]);
                        _row.Append(',');
                        _row.Append(_ave[i]);
                    }
                    else
                    {
                        _row.Append(',');
                        _row.Append(_val[i].Val);
                    }
                }

                _initStats = true;
                _file.WriteLine(_row);
            }
        }

        public void Init()
        {
            _row = new StringBuilder("t");

            // by now all vars will exist in varPool, so expand globs
            Regex regex;
            var varList = new List<string>();

            foreach (string glob in _varGlobs)
            {
                regex = new Regex("^" + glob.Replace("*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                varList.AddRange(SharedContainer.GetAllNames().Where(var => regex.IsMatch(var)));
            }
            _nvars = varList.Count;
            // Console.WriteLine("Output vars: " + string.Join(",", _vars));

            _val = new Shared[_nvars];
            for (int i = 0; i < _nvars; i ++)
            {
                _val[i] = SharedContainer.GetOrNew(varList[i]);
            }

            if (_doStats)
            {
                _min = new double[_nvars];
                _max = new double[_nvars];
                _ave = new double[_nvars];
                for (int i = 0; i < _nvars; i++)
                {
                    _row.Append(',');
                    _row.Append(varList[i]);
                    _row.Append("_min,");
                    _row.Append(varList[i]);
                    _row.Append("_max,");
                    _row.Append(varList[i]);
                    _row.Append("_ave");
                }
            }
            else
            {
                _row.Append(',');
                _row.Append(string.Join(",", varList));
            }
            _file.WriteLine(_row);
        }

        public void Finish()
        {
            try
            {
                _file.Flush();
                _file.Close();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        #endregion
    }
}
