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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PWC.Asim.Core.Contracts;
using PWC.Asim.Core.Utils;

namespace PWC.Asim.Core.Actors
{
    [Flags]
    enum OutputStats
    {
        [Description("Minimum")]
        Min = 0x1,
        [Description("Time of Minimum")]
        MinT = 0x2,
        [Description("Maximum")]
        Max = 0x4,
        [Description("Time of Maximum")]
        MaxT = 0x8,
        [Description("Average")]
        Ave = 0x10,
        [Description("Actual")]
        Act   = 0x20,
        [Description("All Statistics")]
        All = 0x1F
    }

    struct Variable
    {
        public double Min;
        public ulong MinT;
        public double Max;
        public ulong MaxT;
        public double Ave;
        public Shared Act;
        public OutputStats Stats;
    }

    public class OutputData : IActor
    {
        private readonly FileStream _file;
        private readonly StreamWriter _stream;
        private readonly string _filename;
        private readonly string[] _varGlobs;
        private int _nvars;
        private Variable[] _outVars;
        private StringBuilder _row;
        private readonly uint _outputEvery;
        private bool _initStats = true;
        private readonly DateFormat _outputFormat;
        private readonly DateTime _simStartTime;
        private ulong _iteration;
        private readonly SharedContainer _sharedVars = SharedContainer.Instance;

        public OutputData(string filename, string[] vars, uint outputEvery = 1, DateTime? simStartTime = null, DateFormat outputFormat = DateFormat.Other)
        {
            _filename = filename;
            try
            {
                _file = new FileStream(_filename, FileMode.Create, FileAccess.Write);
                var buf = new BufferedStream(_file);
                _stream = new StreamWriter(buf);
            }
            catch(Exception e)
            {
                throw new Exception("Could not write to file '" + _filename + "'. " + e.Message, e);
            }
            _varGlobs = vars;
            _outputEvery = outputEvery;
            _outputFormat = outputFormat;
            _simStartTime = simStartTime.HasValue ? simStartTime.Value : Settings.Epoch;
        }

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            // write output every "_outputEvery" samples, or always if that is 1.
            bool write = _outputEvery == 1 || iteration == 0 || ((iteration % _outputEvery) == 0);
            _iteration = iteration;

            CalculateStats();
            if (write)
            {
                WriteLine();
            }
        }

        public void Init()
        {
            _row = new StringBuilder("t");

            // by now all vars will exist in varPool, so expand globs
            var varStats = new List<string[]>();
            var regex = new Regex(@"^(.*?)(\{(.*)\})?$", RegexOptions.IgnoreCase);
            foreach (string varGlob in _varGlobs)
            {
                var match = regex.Match(varGlob);
                var statistics = match.Groups[3].Success ? match.Groups[3].ToString() : "All";

                varStats.AddRange(_sharedVars.MatchGlobs(new[] { match.Groups[1].ToString() })
                                                 .Select(variable => new[]
                                                     {
                                                         variable, // the variable name
                                                         statistics // the statistics for this variable
                                                     })
                    );
            }
            _nvars = varStats.Count;

            _outVars = new Variable[_nvars];
            for (int i = 0; i < _nvars; i++)
            {
                var variableName = varStats[i][0];
                _outVars[i].Act = _sharedVars.GetOrNew(variableName);
                if (_outputEvery == 1)
                    _outVars[i].Stats = OutputStats.Act;
                else if (variableName.EndsWith("Cnt") || variableName.EndsWith("E"))
                {
                    _outVars[i].Stats = OutputStats.Act;
                }
                else
                {
                    _outVars[i].Stats = 0;
                    varStats[i][1].Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                        .ToList()
                        .ForEach(s=> _outVars[i].Stats |= (OutputStats)Enum.Parse(typeof(OutputStats), s, true));
                }
                if (_outVars[i].Stats == 0)
                    _outVars[i].Stats = OutputStats.All;

                var stat = _outVars[i].Stats;

                if ((stat & OutputStats.Min) > 0)
                {
                    _row.Append(',');
                    _row.Append(variableName);
                    _row.Append("_min");
                }
                if ((stat & OutputStats.MinT) > 0)
                {
                    _row.Append(',');
                    _row.Append(variableName);
                    _row.Append("_minT");
                }
                if ((stat & OutputStats.Max) > 0)
                {
                    _row.Append(',');
                    _row.Append(variableName);
                    _row.Append("_max");
                }
                if ((stat & OutputStats.MaxT) > 0)
                {
                    _row.Append(',');
                    _row.Append(variableName);
                    _row.Append("_maxT");
                }
                if ((stat & OutputStats.Ave) > 0)
                {
                    _row.Append(',');
                    _row.Append(variableName);
                    _row.Append("_ave");
                }
                if ((stat & OutputStats.Act) > 0)
                {
                    _row.Append(',');
                    _row.Append(variableName);
                }
            }

            _stream.WriteLine(_row);
        }

        public void Finish()
        {
            if (!_initStats)
            {
                WriteLine();
            }
            try
            {
                _stream.Flush();
                _stream.Close();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        #endregion

        private void CalculateStats()
        {
            for (int i = 0; i < _nvars; i++)
            {
                if ((_outVars[i].Stats & OutputStats.All) > 0)
                {
                    if (_initStats)
                    {
                        _outVars[i].Max = _outVars[i].Min = _outVars[i].Ave = _outVars[i].Act.Val;
                        _outVars[i].MinT = _outVars[i].MaxT = _iteration;
                    }
                    else
                    {
                        if (_outVars[i].Act.Val < _outVars[i].Min)
                        {
                            _outVars[i].Min = _outVars[i].Act.Val;
                            _outVars[i].MinT = _iteration;
                        }
                        else if (_outVars[i].Act.Val > _outVars[i].Max)
                        {
                            _outVars[i].Max = _outVars[i].Act.Val;
                            _outVars[i].MaxT = _iteration;
                        }
                        // use ave to store the sum
                        _outVars[i].Ave = _outVars[i].Ave + _outVars[i].Act.Val;
                    }
                }
            }
            _initStats = false;
        }

        private void WriteLine()
        {
            _row.Clear();
            switch (_outputFormat)
            {
                case DateFormat.RelativeToEpoch:
                    _row.Append((_simStartTime.AddSeconds(_iteration) - Settings.Epoch).TotalSeconds);
                    break;
                case DateFormat.RelativeToSim:
                    _row.Append(_iteration);
                    break;
                default:
                    _row.Append(_simStartTime.AddSeconds(_iteration).ToString("yyyy-MM-dd HH:mm:ss"));
                    break;
            }
            for (int i = 0; i < _nvars; i++)
            {
                var stat = _outVars[i].Stats;

                if ((stat & OutputStats.Min) > 0)
                {
                    _row.Append(',');
                    _row.Append(_outVars[i].Min);
                }
                if ((stat & OutputStats.MinT) > 0)
                {
                    _row.Append(',');
                    _row.Append(_outVars[i].MinT);
                }
                if ((stat & OutputStats.Max) > 0)
                {
                    _row.Append(',');
                    _row.Append(_outVars[i].Max);
                }
                if ((stat & OutputStats.MaxT) > 0)
                {
                    _row.Append(',');
                    _row.Append(_outVars[i].MaxT);
                }
                if ((stat & OutputStats.Ave) > 0)
                {
                    _outVars[i].Ave = _outVars[i].Ave / _outputEvery;
                    _row.Append(',');
                    _row.Append(_outVars[i].Ave);
                }
                if ((stat & OutputStats.Act) > 0)
                {
                    _row.Append(',');
                    _row.Append(_outVars[i].Act.Val);
                }
            }

            _initStats = true;
            _stream.WriteLine(_row);
        }
    }
}
