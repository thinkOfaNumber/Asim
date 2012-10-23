using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SolarLoadModel.Contracts;
using SolarLoadModel.Utils;

namespace SolarLoadModel.Actors
{
    struct Variable
    {
        public double Min;
        public ulong MinT;
        public double Max;
        public ulong MaxT;
        public double Ave;
        public bool DoStats;
        public Shared svar;
        public double Val
        {
            get { return svar.Val; }
        }
    }

    public class OutputData : IActor
    {
        private readonly System.IO.StreamWriter _file;
        private readonly string _filename;
        private readonly string[] _varGlobs;
        private int _nvars;
        private Variable[] _outVars;
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

            for (int i = 0; i < _nvars; i++)
            {
                if (_outVars[i].DoStats)
                {
                    if (_initStats)
                    {
                        _outVars[i].Max = _outVars[i].Min = _outVars[i].Ave = _outVars[i].Val;
                        _outVars[i].MinT = _outVars[i].MaxT = iteration;
                    }
                    else
                    {
                        if (_outVars[i].Val < _outVars[i].Min)
                        {
                            _outVars[i].Min = _outVars[i].Val;
                            _outVars[i].MinT = iteration;
                        }
                        else if (_outVars[i].Val > _outVars[i].Max)
                        {
                            _outVars[i].Max = _outVars[i].Val;
                            _outVars[i].MaxT = iteration;
                        }
                        // use ave to store the sum
                        _outVars[i].Ave = _outVars[i].Ave + _outVars[i].Val;
                    }
                }
            }
            _initStats = false;

            if (write)
            {
                _row.Clear();
                _row.Append(iteration);
                for (int i = 0; i < _nvars; i++)
                {
                    if (_outVars[i].DoStats)
                    {
                        _outVars[i].Ave = _outVars[i].Ave/_outputEvery;
                        _row.Append(',');
                        _row.Append(_outVars[i].Min);
                        _row.Append(',');
                        _row.Append(_outVars[i].MinT);
                        _row.Append(',');
                        _row.Append(_outVars[i].Max);
                        _row.Append(',');
                        _row.Append(_outVars[i].MaxT);
                        _row.Append(',');
                        _row.Append(_outVars[i].Ave);
                    }
                    else
                    {
                        _row.Append(',');
                        _row.Append(_outVars[i].Val);
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

            _outVars = new Variable[_nvars];
            for (int i = 0; i < _nvars; i++)
            {
                _outVars[i].svar = SharedContainer.GetOrNew(varList[i]);
                _outVars[i].DoStats = _doStats && !varList[i].EndsWith("Cnt");
            }

            for (int i = 0; i < _nvars; i++)
            {
                if (_outVars[i].DoStats)
                {
                    _row.Append(',');
                    _row.Append(varList[i]);
                    _row.Append("_min,");
                    _row.Append(varList[i]);
                    _row.Append("_minT,");
                    _row.Append(varList[i]);
                    _row.Append("_max,");
                    _row.Append(varList[i]);
                    _row.Append("_maxT,");
                    _row.Append(varList[i]);
                    _row.Append("_ave");
                }
                else
                {
                    _row.Append(',');
                    _row.Append(varList[i]);
                }
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
