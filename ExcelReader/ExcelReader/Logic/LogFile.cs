using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Office.Interop.Excel;

namespace ExcelReader.Logic
{
    public class LogFile
    {
        private Dictionary<string, Dictionary<long, string>> _logInformation = new Dictionary<string, Dictionary<long, string>>();
        private ConfigSettings _settings;
        private Workbook _workBook;

        public LogFile(Workbook workbook, ConfigSettings settings)
        {
            _settings = settings;
            _workBook = workbook;
        }

        public void Run()
        {
            if (_workBook != null)
            {
                if (_settings.LogInformation != null &&
                    _settings.LogInformation.Globs != null &&
                    _settings.LogInformation.Globs.Any())
                {
                    // process each of the append information
                    _logInformation = new Dictionary<string, Dictionary<long, string>>();
                    RetrieveInformation();
                    ProcessInformation();
                }
            }
        }

        private long FindMaxTime()
        {
            long maxValue = 0;
            if (_logInformation != null && _logInformation.Any())
            {
                maxValue = _logInformation.Values.ToList().Max(v => v.Keys.Max());
            }
            return maxValue;
        }

        private void ProcessInformation()
        {
            StringBuilder currentLine = new StringBuilder();
            StringBuilder information = new StringBuilder(DateTime.Now.ToString());

            if (!string.IsNullOrEmpty(_settings.CommunityName))
            {
                information.Append(" - Community Name: ");
                information.Append(_settings.CommunityName);
            }
            information.Append(Environment.NewLine);
            information.Append("t," + string.Join(",", _logInformation.Keys));
            information.Append(Environment.NewLine);

            long maxNumber = FindMaxTime();
            string theValue;

            for (long i = 0; i <= maxNumber; i++)
            {
                currentLine.Clear();
                currentLine.Append(i);
                bool writeRow = false;

                foreach (KeyValuePair<string, Dictionary<long, string>> keyValuePair in _logInformation)
                {
                    currentLine.Append(",");
                    if (keyValuePair.Value.TryGetValue(i, out theValue))
                    {
                        currentLine.Append(theValue);
                        writeRow = true;
                    }
                }

                if (writeRow)
                {
                    information.Append(currentLine);
                }
            }
        }

        private void RetrieveInformation()
        {
            if (_workBook != null)
            {
                // create regex globs
                List<Regex> regexPatterns = _settings.LogInformation.Globs.Select(glob => new Regex("^" + glob.Replace("*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();

                foreach (Worksheet sheet in _workBook.Sheets)
                {
                    if (sheet.Cells[1,1].Value.ToString() != "t")
                    {
                       continue;
                    }

                    Range excelRange = sheet.UsedRange;
                    object[,] valueArray = (object[,])excelRange.Value[XlRangeValueDataType.xlRangeValueDefault];
                    
                    List<List<string>> values = new List<List<string>>();
                    int rows = valueArray.GetLength(0);

                    for (int i = 0; i < rows; i++)
                    {
                        //values.Add((List<string>)valueArray.GetValue(i));
                    }

                    int columns = valueArray.GetLength(1);
                    

                    for (int i = 1; i < columns; i++)
                    {
                        string headerValue = values[1][i];

                        if (!regexPatterns.Any(glob => glob.IsMatch(headerValue)))
                        {
                            
                        }
                    }

                }
            }
        }
    }
}
