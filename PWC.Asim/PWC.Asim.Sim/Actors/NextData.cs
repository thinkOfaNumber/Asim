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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PWC.Asim.Sim.Contracts;
using PWC.Asim.Sim.Exceptions;
using PWC.Asim.Sim.Utils;

namespace PWC.Asim.Sim.Actors
{
    /// <summary>
    /// Reads from input files with format:
    /// t,'name1','name2',...
    /// time,'value1','value2',... 
    /// 
    /// Time stamps can be written in either ISO8601, seconds since the Epoch (Jan 1, 1970),
    /// seconds since the simulation start, or 'human readable' non-ambiguous time with automatic
    /// detection.  Typical time stamps include values such as 1997-07-16T19:20:30.45 or
    /// 12345566.849 or 100.
    /// 
    /// If the first time sample is a "seconds" value, and it is less that the simulation start
    /// time since the Epoch, it is interpreted as relative to the start of the simulation.
    /// </summary>
    public class NextData : IActor
    {
        struct ValueContainer
        {
            public bool DoScale;
            public Shared Update;
        };

        private string[] _cells;
        private ValueContainer[] _values;

        private readonly FileStream  _file;
        private StreamReader _stream;

        private readonly string _filename;
        private Int64 _nextT;
        private UInt64 _offset;
        private UInt64 _period;
        private readonly bool _recycle;
        private string _nextline;
        private ulong _lineNo;
        private int _columnCount;
        private readonly char[] _scaleChars = new[] { '*', '+' };
        
        private DateFormat _dateFormat;
        private readonly DateTime _simStartTime;
        private readonly ulong _simOffset;

        public NextData(string filename, DateTime? simStartTime = null, bool recycle = false)
        {
            _filename = filename;
            _recycle = recycle;
            _file = new FileStream(_filename, FileMode.Open, FileAccess.Read);
            _stream = new StreamReader(_file);
            _simStartTime = simStartTime.HasValue ? simStartTime.Value : Settings.Epoch;
            _simOffset = Convert.ToUInt64((_simStartTime - Settings.Epoch).TotalSeconds);
        }

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            if (iteration != (ulong)_nextT || _nextline == null)
                return;

            try
            {
                _cells = _nextline.Split(',');
                for (int i = 0; i < _columnCount; i++)
                {
                    try
                    {
                        if (_values[i].DoScale)
                        {
                            string[] func = _cells[i + 1].Split(_scaleChars, StringSplitOptions.RemoveEmptyEntries);
                            // update the translation function which will be used to translate this
                            // variable the next time it is read.  Essentially it is (d * x + k)
                            _values[i].Update.ScaleFunction = v => v * Convert.ToDouble(func[0]) + Convert.ToDouble(func[1]);
                        }
                        else
                        {
                            // ignore first cell
                            // note the scaling is not done here
                            _values[i].Update.Val = Convert.ToDouble(_cells[i + 1]);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new SimulationException(
                            string.Format("Expected Number at Cell {0} line {1} of {2}, got '{3}'", i + 1, _lineNo, _filename, _cells[i + 1]),
                            e);
                    }
                }

                ReadLine();
                if (_nextline != null)
                {
                    var nextT = DateToInt64(_nextline.Substring(0, _nextline.IndexOf(',')));
                    if (nextT <= _nextT)
                        throw new Exception("Time stood still or went backwards!");
                    _nextT = nextT;
                }
            }
            catch (SimulationException)
            {
                throw;
            }
            catch(Exception e)
            {
                throw new SimulationException(string.Format("Error parsing line {0} of {1}:\n{2}", _lineNo, _filename, e.Message), e);
            }
        }

        public void Init()
        {
            _period = 1;
            _offset = 0;
            // get headers
            ReadLine();
            List<string> headers = _nextline.Split(',').ToList();
            if (!headers[0].Equals("t"))
            {
                throw new FormatException(_filename + ": Cell 0,0 must contain 't' to be a valid input file");
            }
            if (headers.Count <= 1)
            {
                throw new FormatException(_filename + ": Header row must contain at least one variable besides 't'.");
            }
            headers.RemoveAt(0);
            _columnCount = headers.Count;
            _values = new ValueContainer[_columnCount];

            for (int i = 0; i < _columnCount; i++)
            {
                if (headers[i][0] == '>')
                {
                    _values[i].DoScale = true;
                    _values[i].Update = SharedContainer.GetOrNew(headers[i].Substring(1));
                    _values[i].Update.ScaleFunction = v => v;
                }
                else
                {
                    _values[i].Update = SharedContainer.GetOrNew(headers[i]);
                    _values[i].Update.Val = 0;
                }
            }

            // get first data row
            do
            {
                ReadLine();
                if (_nextline == null)
                    break;
                string time = _nextline.Substring(0, _nextline.IndexOf(','));
                _dateFormat = GetDateFormat(time);
                _nextT = DateToInt64(time);
                // times less than zero represent a start time after the beginning
                // of the data file's first sample, so ignore up to "zero"
            } while (_nextT < 0);
        }

        public void Finish()
        {
            
        }

        #endregion

        /// <summary>
        /// Read the next line from the input file, and parse the date from it.
        /// The line is stored in _nextline, and the date in _nextT.
        /// </summary>
        /// <returns></returns>
        private void ReadLine()
        {
            _nextline = _stream.ReadLine();
            _lineNo++;

            if (_nextline == null && _recycle)
            {
                _file.Seek(0, SeekOrigin.Begin);
                _stream = new StreamReader(_file);
                _stream.ReadLine(); // ignore header row
                _nextline = _stream.ReadLine();
                _lineNo = 1;
                _offset = (ulong)(_nextT + (long)_period);
            }
        }

        /// <summary>
        /// converts a time string to the number of seconds since the start of
        /// the simulation
        /// </summary>
        /// <param name="time">string time to convert</param>
        /// <returns></returns>
        private Int64 DateToInt64(string time)
        {
            Int64 seconds = 0;
            try
            {
                switch (_dateFormat)
                {
                    case DateFormat.RelativeToEpoch:
                        seconds = Convert.ToInt64(time) - (long)_simOffset;
                        break;

                    case DateFormat.RelativeToSim:
                        seconds = Convert.ToInt64(time);
                        break;

                    case DateFormat.Other:
                        var datetime = Convert.ToDateTime(time);
                        seconds = Convert.ToInt64((datetime - _simStartTime).TotalSeconds);
                        break;
                }
            }
            catch (Exception e)
            {
                throw new SimulationException(_filename + ": Couldn't parse date on line " + _lineNo, e);
            }
            if (seconds != 0)
            {
                _period = (ulong)(seconds - (_nextT - (long)_offset));
            }
            return seconds + (long)_offset;
        }

        private DateFormat GetDateFormat(string s)
        {
            double var;
            if (Double.TryParse(s, out var))
            {
                if (var < _simOffset)
                    return DateFormat.RelativeToSim;
                else
                    return DateFormat.RelativeToEpoch;
            }
            return DateFormat.Other;
        }
    }
}