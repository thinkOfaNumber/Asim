using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ExcelReader.Interface;

namespace ExcelReader.Logic
{
    public class LogFile
    {
        private readonly ConfigSettings _settings;
        private readonly List<MyWorksheet> _workBook;

        public LogFile(List<MyWorksheet> workbook, ConfigSettings settings)
        {
            _settings = settings;
            _workBook = workbook;
        }

        public void Run()
        {
            if (_workBook != null)
            {
                if (_settings.LogInformation != null &&
                    !string.IsNullOrEmpty(_settings.LogInformation.LogFile) &&
                    _settings.LogInformation.Globs != null &&
                    _settings.LogInformation.Globs.Any())
                {
                    foreach (MyWorksheet sheet in _workBook)
                    {
                        List<List<string>> sheetInformation = RetrieveInformation(sheet);
                        if (sheetInformation != null && sheetInformation.Any())
                        {
                            ProcessInformation(sheetInformation);
                        }
                    }
                }
            }
        }
        
        private void ProcessInformation(List<List<string>> sheetInformation)
        {
            StringBuilder information = new StringBuilder(DateTime.Now.ToString());

            if (!string.IsNullOrEmpty(_settings.CommunityName))
            {
                information.Append(" - Community Name: ");
                information.Append(_settings.CommunityName);
            }
            information.Append(Environment.NewLine);
            foreach (List<string> list in sheetInformation)
            {
                information.Append(string.Join(",", list));
                information.Append(Environment.NewLine);
            }
            
            File.AppendAllText(_settings.LogInformation.LogFile, information.ToString());
        }

        private List<List<string>> RetrieveInformation(MyWorksheet sheet)
        {
            if (sheet != null)
            {
                // create regex globs
                List<Regex> regexPatterns = _settings.LogInformation.Globs.Select(glob => new Regex("^" + glob.Replace("*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();

                if (sheet.Data == null || sheet.Data[1, 1].ToString() != "t")
                {
                    return null;
                }

                int rows = sheet.Data.GetLength(0);
                int columns = sheet.Data.GetLength(1);
                List<List<string>> values = new List<List<string>>(rows);

                for (int i = 0; i < rows; i++)
                {
                    values.Add(new List<string>());
                }

                for (int i = 1; i <= columns; i++)
                {
                    string headerValue = sheet.Data[1, i].ToString();
                    if (headerValue == "t" || regexPatterns.Any(glob => glob.IsMatch(headerValue)))
                    {
                        for (int j = 1; j <= rows; j++)
                        {
                            values[j - 1].Add(sheet.Data[j, i].ToString());
                        }
                    }
                }

                return values;
            }

            return null;
        }
    }
}
