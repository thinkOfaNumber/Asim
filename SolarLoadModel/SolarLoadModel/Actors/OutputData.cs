using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SolarLoadModel.Contracts;
using SolarLoadModel.Exceptions;

namespace SolarLoadModel.Actors
{
    public class OutputData : IActor
    {
        private readonly System.IO.StreamWriter _file;
        private readonly string _filename;
        private readonly string[] _varGlobs;
        private string[] _vars;
        private int _nvars;
        private double[] _min;
        private double[] _max;
        private double[] _ave;
        private double[] _val;
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

        public void Run(Dictionary<string, double> varPool, ulong iteration)
        {
            // write output every "_outputEvery" samples, or always if that is 1.
            bool write = !_doStats || ((iteration % _outputEvery) == (_outputEvery - 1));

            for (int i = 0; i < _nvars; i++)
            {
                // cache current value to avoid looking up constantly
                try
                {
                    _val[i] = varPool[_vars[i]];
                }
                catch
                {
                    throw new VarNotFoundException(string.Format("Couldn't write {0} to file {1} because it wasn't in the dictionary.", _vars[i], _filename));
                }
            }
            if (_doStats)
            {
                for (int i = 0; i < _nvars; i++)
                {
                    if (_initStats)
                    {
                        _max[i] = _min[i] = _ave[i] = _val[i];
                    }
                    else
                    {
                        _max[i] = Math.Max(_max[i], _val[i]);
                        _min[i] = Math.Min(_min[i], _val[i]);
                        // use ave to store the sum
                        _ave[i] = _ave[i] + _val[i];
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
                        _row.Append(_val[i]);
                    }
                }

                _initStats = true;
                _file.WriteLine(_row);
            }
        }

        public void Init(Dictionary<string, double> varPool)
        {
            _row = new StringBuilder("t");

            // by now all vars will exist in varPool, so expand globs
            Regex regex;
            var varList = new HashSet<string>();
            foreach (string glob in _varGlobs)
            {
                regex = new Regex("^" + glob.Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                //varList.AddRange(varPool.Keys.Where(var => regex.IsMatch(var)));
                foreach (string var in varPool.Keys.Where(var => regex.IsMatch(var)))
                {
                    varList.Add(var);
                }
            }
            _vars = varList.ToArray();
            _nvars = varList.Count;

            _val = new double[_nvars];
            if (_doStats)
            {
                _min = new double[_nvars];
                _max = new double[_nvars];
                _ave = new double[_nvars];
                for (int i = 0; i < _nvars; i++)
                {
                    _row.Append(',');
                    _row.Append(_vars[i]);
                    _row.Append("_min,");
                    _row.Append(_vars[i]);
                    _row.Append("_max,");
                    _row.Append(_vars[i]);
                    _row.Append("_ave");
                }
            }
            else
            {
                _row.Append(',');
                _row.Append(string.Join(",", _vars));
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
