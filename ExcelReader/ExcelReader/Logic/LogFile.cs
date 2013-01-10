// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of Excel Reader - An Excel Manipulation Program
//
// Excel Reader is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Foobar is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
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
            StringBuilder information = new StringBuilder();

            if (!string.IsNullOrEmpty(_settings.CommunityName))
            {
                information.Append(_settings.CommunityName);
                information.Append(" ");
            }
            information.Append(_settings.DateSimulatorRun.ToString("yyyy-MM-dd HH:mm:ss"));
            information.Append(Environment.NewLine);
            foreach (List<string> list in sheetInformation)
            {
                information.Append(string.Join(",", list));
                information.Append(Environment.NewLine);
            }
            information.Append(Environment.NewLine);

            File.AppendAllText(_settings.LogInformation.LogFile, information.ToString());
        }

        private List<List<string>> RetrieveInformation(MyWorksheet sheet)
        {
            if (sheet != null)
            {
                // create regex globs
                List<Regex> regexPatterns = _settings.LogInformation.Globs.Select(glob => new Regex("^" + glob.Replace("*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                bool globMatches = false; // this is to make sure that there were at least some headers that matched.

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
                    bool isGlobMatch = false;
                    if (regexPatterns.Any(glob => glob.IsMatch(headerValue)))
                    {
                        isGlobMatch = true;
                        globMatches = true;
                    }

                    if (headerValue == "t" || isGlobMatch)
                    {
                        for (int j = 1; j <= rows; j++)
                        {
                            values[j - 1].Add(sheet.Data[j, i].ToString());
                        }
                    }
                }

                if (globMatches)
                {
                    return values;
                }
            }

            return null;
        }
    }
}
