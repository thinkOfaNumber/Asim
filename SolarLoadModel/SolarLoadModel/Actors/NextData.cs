using System;
using System.Collections.Generic;
using System.Linq;
using SolarLoadModel.Contracts;
using SolarLoadModel.Exceptions;
using SolarLoadModel.Utils;

namespace SolarLoadModel.Actors
{
    public class NextData : IActor
    {
        private readonly IList<string> _headers = new List<string>();
        private double[] _row;
        private string[] _cells;
        private Shared[] _values;

        private readonly System.IO.StreamReader _file;
        private readonly string _filename;
        private UInt64 _nextT;
        private string _nextline;
        private ulong _lineNo;
        private int _headerCount;

        #region Implementation of IActor
                public NextData(string filename)
        {
            _filename = filename;
            _file = new System.IO.StreamReader(_filename);
        }

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
                    _nextT = Convert.ToUInt64(_nextline.Substring(0, _nextline.IndexOf(',')));
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
            _row = new double[_headerCount];
            _values = new Shared[_headerCount];

            // init variables in this file
            for (int i = 0; i < _headerCount; i++)
            {
                _values[i] = SharedContainer.GetOrNew(_headers.ElementAt(i));
                _values[i].Val = 0;
            }

            // get first data row
            _nextline = ReadLine();
            _nextT = Convert.ToUInt64(_nextline.Substring(0, _nextline.IndexOf(',')));
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
    }
}
