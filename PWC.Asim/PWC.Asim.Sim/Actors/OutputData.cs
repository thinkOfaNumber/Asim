﻿// Copyright (C) 2012, 2013  Power Water Corporation
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
using System.Text;
using PWC.Asim.Sim.Contracts;
using PWC.Asim.Sim.Utils;

namespace PWC.Asim.Sim.Actors
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
        private readonly DateFormat _outputFormat;
        private readonly DateTime _simStartTime;
        private ulong _iteration;

        public OutputData(string filename, string[] vars, uint outputEvery = 1, DateTime? simStartTime = null, DateFormat outputFormat = DateFormat.Other)
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
            _outputFormat = outputFormat;
            _simStartTime = simStartTime.HasValue ? simStartTime.Value : Settings.Epoch;
        }

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            // write output every "_outputEvery" samples, or always if that is 1.
            bool write = !_doStats || iteration == 0 || ((iteration % _outputEvery) == 0);
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
            var varList = SharedContainer.MatchGlobs(_varGlobs);
            _nvars = varList.Count;

            _outVars = new Variable[_nvars];
            for (int i = 0; i < _nvars; i++)
            {
                _outVars[i].svar = SharedContainer.GetOrNew(varList[i]);
                _outVars[i].DoStats = _doStats && !varList[i].EndsWith("Cnt") && !varList[i].EndsWith("E");
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
            if (!_initStats)
            {
                WriteLine();
            }
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

        private void CalculateStats()
        {
            for (int i = 0; i < _nvars; i++)
            {
                if (_outVars[i].DoStats)
                {
                    if (_initStats)
                    {
                        _outVars[i].Max = _outVars[i].Min = _outVars[i].Ave = _outVars[i].Val;
                        _outVars[i].MinT = _outVars[i].MaxT = _iteration;
                    }
                    else
                    {
                        if (_outVars[i].Val < _outVars[i].Min)
                        {
                            _outVars[i].Min = _outVars[i].Val;
                            _outVars[i].MinT = _iteration;
                        }
                        else if (_outVars[i].Val > _outVars[i].Max)
                        {
                            _outVars[i].Max = _outVars[i].Val;
                            _outVars[i].MaxT = _iteration;
                        }
                        // use ave to store the sum
                        _outVars[i].Ave = _outVars[i].Ave + _outVars[i].Val;
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
                if (_outVars[i].DoStats)
                {
                    _outVars[i].Ave = _outVars[i].Ave / _outputEvery;
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
}