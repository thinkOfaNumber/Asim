using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SolarLoadModel.Contracts;

namespace SolarLoadModel.Actors
{
    public class NextData : IActor
    {
        private readonly IList<string> _headers = new List<string>();
        private double[] _row;
        private string[] _cells;

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

        ~NextData()
        {
            _file.Close();
        }

        public void Run(Dictionary<string, double> varPool, ulong iteration)
        {
            try
            {
                if (iteration == _nextT && _nextline != null)
                {
                    _cells = _nextline.Split(',');
                    for (int i = 1; i < _cells.Count(); i++)
                    {
                        // ignore first cell
                        _row[i - 1] = Convert.ToDouble(_cells[i]);
                    }

                    _nextline = ReadLine();
                    if (_nextline != null)
                    {
                        _nextT = Convert.ToUInt64(_nextline.Substring(0, _nextline.IndexOf(',')));
                    }
                }

                for (int j = 0; j < _headerCount; j++)
                {
                    varPool[_headers.ElementAt(j)] = _row[j];
                }
            }
            catch(Exception e)
            {
                throw new FormatException(string.Format("Error parsing line {0} of {1}:\n{2}", _lineNo, _filename, e.Message), e);
            }
        }

        public void Init(Dictionary<string, double> varPool)
        {
            // get headers
            _nextline = ReadLine();
            List<string> data = _nextline.Split(',').ToList();
            if (!data[0].Equals("t"))
            {
                throw new FormatException("Cell 0,0 must contain 't' to be a valid input file");
            }
            if (data.Count <= 1)
            {
                throw new FormatException("Header row must contain at least one variable besides 't'.");
            }
            data.RemoveAt(0);
            data.ForEach(_headers.Add);
            _headerCount = _headers.Count;
            _row = new double[_headerCount];

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
