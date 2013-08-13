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
using System.IO;
using System.Linq;
using PWC.Asim.Core.Contracts;
using PWC.Asim.Core.Exceptions;
using PWC.Asim.Core.Utils;

namespace PWC.Asim.Core.Actors
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
        private ulong _lineNo;
        private int _columnCount;
        private readonly char[] _scaleChars = new[] { '*', '+' };
        private bool _noMoreData = false;
        
        private DateFormat? _dateFormat;
        private readonly DateTime _simStartTime;
        private readonly ulong _simOffset;
        private readonly SharedContainer _sharedVars = SharedContainer.Instance;

        public NextData(string filename, DateTime? simStartTime = null, bool recycle = false)
        {
            _filename = filename;
            _recycle = recycle;
            _file = new FileStream(_filename, FileMode.Open, FileAccess.Read);
            var buf = new BufferedStream(_file);
            _stream = new StreamReader(buf);
            _simStartTime = simStartTime.HasValue ? simStartTime.Value : Settings.Epoch;
            _simOffset = Convert.ToUInt64((_simStartTime - Settings.Epoch).TotalSeconds);
        }

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            if (iteration != (ulong)_nextT || _noMoreData)
                return;

            try
            {
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

                ReadTo(); // read next line
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
            _lineNo = 1;
            List<string> headers = _stream.ReadLine().Split(',').ToList();
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
                if (string.IsNullOrWhiteSpace(headers[i]))
                {
                    throw new FormatException(_filename + ": Header row column " + (i+1) + " is empty.");
                }
                else if (headers[i][0] == '>')
                {
                    _values[i].DoScale = true;
                    _values[i].Update = _sharedVars.GetOrNew(headers[i].Substring(1));
                    _values[i].Update.ScaleFunction = v => v;
                }
                else
                {
                    _values[i].Update = _sharedVars.GetOrNew(headers[i]);
                    _values[i].Update.Val = 0;
                }
            }

            // get first data row
            _nextT = -1;
            ReadTo(true);
        }

        public void Finish()
        {
            
        }

        #endregion

        /// <summary>
        /// Read up to the next matching line from the input file, and parse the date from it.
        /// The cells are stored in _cells, and the date in _nextT.
        /// </summary>
        /// <param name="readToIterationStart">A boolean where TRUE will read up all data until the
        /// iteratoin start time, and FALSE will just read one line.</param>
        private void ReadTo(bool readToIterationStart = false)
        {
            long iterTime = 0;
            do
            {
                var nextline = _stream.ReadLine();
                if (nextline == null && _recycle)
                {
                    _file.Seek(0, SeekOrigin.Begin);
                    _stream.ReadLine(); // ignore header row
                    nextline = _stream.ReadLine();

                    _lineNo = 1;
                    _offset = (ulong) (_nextT + (long) _period);
                }
                if (nextline == null)
                {
                    _noMoreData = true;
                    break;
                }

                _cells = nextline.Split(',');

                iterTime = DateToInt64(_cells[0]);
                _noMoreData = false;
            } while ((readToIterationStart && iterTime < 0) || iterTime <= _nextT);
            if (!_noMoreData)
                _nextT = iterTime;
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
            _dateFormat = _dateFormat ?? GetDateFormat(time);
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
                throw new SimulationException(_filename + ": Couldn't parse date on line " + _lineNo + ": " + time, e);
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
