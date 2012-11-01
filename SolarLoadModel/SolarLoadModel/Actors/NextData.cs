using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SolarLoadModel.Contracts;
using SolarLoadModel.Exceptions;
using SolarLoadModel.Utils;

namespace SolarLoadModel.Actors
{
    /// <summary>
    /// Reads from input files with format:
    /// t,'name1','name2',...
    /// time,'value1','value2',... 
    /// 
    /// Time stamps can be written in either IS8601, seconds since the Epoch (Jan 1, 1970),
    /// seconds since the simulation start, or 'human readable' non-ambiguous time with automatic
    /// detection.  Typical time stamps include values such as 1997-07-16T19:20:30.45 or
    /// 12345566.849 or 100.
    /// 
    /// If the first time sample is a "seconds" value, and it is less that the simulation start
    /// time since the Epoch, it is interpreted as relative to the start of the simulation.
    /// </summary>
    public class NextData : IActor
    {
        private readonly IList<string> _headers = new List<string>();
        private string[] _cells;
        private Shared[] _values;

        private readonly System.IO.StreamReader _file;
        private readonly string _filename;
        private UInt64 _nextT;
        private string _nextline;
        private ulong _lineNo;
        private int _headerCount;

        private DateFormat _dateFormat;
        private readonly DateTime _simStartTime;
        private readonly ulong _simOffset;

        public NextData(string filename, DateTime? simStartTime = null)
        {
            _filename = filename;
            _file = new System.IO.StreamReader(_filename);
            _simStartTime = simStartTime.HasValue ? simStartTime.Value : Settings.Epoch;
            _simOffset = Convert.ToUInt64((_simStartTime - Settings.Epoch).TotalSeconds);
        }

        #region Implementation of IActor
        public void Run(ulong iteration)
        {
            if (iteration != _nextT || _nextline == null)
                return;

            try
            {
                _cells = _nextline.Split(',');
                for (int i = 0; i < _headerCount; i++)
                {
                    // ignore first cell
                    try
                    {
                        _values[i].Val = Convert.ToDouble(_cells[i + 1]);
                    }
                    catch (Exception e)
                    {
                        throw new SimulationException(string.Format("Expected Number at Cell {0} line {1} of {2}, got '{3}'", i + 1, _lineNo, _filename, _cells[i]), e);
                    }
                }

                _nextline = ReadLine();
                if (_nextline != null)
                {
                    _nextT = DateToUInt64(_nextline.Substring(0, _nextline.IndexOf(',')));
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
            // get headers
            _nextline = ReadLine();
            List<string> data = _nextline.Split(',').ToList();
            if (!data[0].Equals("t"))
            {
                throw new FormatException(_filename + ": Cell 0,0 must contain 't' to be a valid input file");
            }
            if (data.Count <= 1)
            {
                throw new FormatException(_filename + ": Header row must contain at least one variable besides 't'.");
            }
            data.RemoveAt(0);
            data.ForEach(_headers.Add);
            _headerCount = _headers.Count;
            _values = new Shared[_headerCount];

            // init variables in this file
            for (int i = 0; i < _headerCount; i++)
            {
                _values[i] = SharedContainer.GetOrNew(_headers.ElementAt(i));
                _values[i].Val = 0;
            }

            // get first data row
            _nextline = ReadLine();
            string time = _nextline.Substring(0, _nextline.IndexOf(','));
            _dateFormat = GetDateFormat(time);
            _nextT = DateToUInt64(time);
        }

        public void Finish()
        {
            
        }

        #endregion

        private string ReadLine()
        {
            var s = _file.ReadLine();
            _lineNo++;
            return s;
        }

        private UInt64 DateToUInt64(string time)
        {
            try
            {
                switch (_dateFormat)
                {
                    case DateFormat.RelativeToEpoch:
                        return Convert.ToUInt64(time) - _simOffset;
                        break;

                    case DateFormat.RelativeToSim:
                        return Convert.ToUInt64(time);
                        break;

                    case DateFormat.Other:
                        var datetime = Convert.ToDateTime(time);
                        return Convert.ToUInt64((datetime - _simStartTime).TotalSeconds);
                        break;
                }
            }
            catch (Exception e)
            {
                throw new SimulationException(_filename + ": Couldn't parse date on line " + _lineNo, e);
            }
            return 0;
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
