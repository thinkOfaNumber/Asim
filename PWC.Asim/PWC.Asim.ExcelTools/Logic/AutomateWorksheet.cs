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
using System.Text.RegularExpressions;
using PWC.Asim.ExcelTools.Interface;
using Excel = Microsoft.Office.Interop.Excel;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;

namespace PWC.Asim.ExcelTools.Logic
{
    public class AutomateWorksheet : IExcelReader
    {
        public Action<string> ShowSimOutput { get; set; }
        public Action<bool> OnExit { get; set; }

        private ConfigSettings _settings;
        private string _filename;
        Workbook _workBook;
        private bool _weOpened;
        private List<MyWorksheet> _workBookData;
        private static readonly object LockResults = new Object();

        // sim results sheet:
        private Worksheet _resultsSheet;
        private int _resultsCell = 1;
        Excel.Chart _resultChartPage;
        private Excel.Range _resultChartRange;

        public AutomateWorksheet(string inputFile, ConfigSettings settings)
        {
            _settings = settings;
            _filename = inputFile;
            ShowSimOutput = ShowSimResults;
            OnExit = CleanupMessages;

            _settings.ExcelFile = inputFile;
        }

        public void ProcessConfigSheet(bool attachToRunningProcess)
        {
            var fileInfo = new FileInfo(_filename);
            try
            {
                Directory.SetCurrentDirectory(fileInfo.DirectoryName);
                _weOpened = !IsFileOpen(fileInfo.FullName);
                if (_weOpened)
                    _workBook =
                        System.Runtime.InteropServices.Marshal.BindToMoniker(fileInfo.FullName) as Excel.Workbook;
                else
                {
                    var app = System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application") as Excel.Application;
                    _workBook = app.ActiveWorkbook;
                }
                GetWorkbookData();
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("couldn't {0} Excel workbook: {1}",
                    _weOpened ? "attach to running" : "open", e.Message));
            }

            try
            {
                ParseConfigData(_workBookData.First(s => s.Name.Equals("config")));
            }
            catch (Exception e)
            {
                throw new Exception("Couldn't find config tab.", e);
            }
            // set the current directory
            if (!string.IsNullOrEmpty(_settings.Directory))
            {
                try
                {
                    Directory.SetCurrentDirectory(_settings.Directory);
                }
                catch (Exception e)
                {
                    throw new Exception("Error setting directory: " + e.Message, e);
                }
            }

            new LogFile(_workBookData, _settings).Run();

            _settings.SplitFilePrefix = fileInfo.Name.Replace(fileInfo.Extension, "_");

            // prefix the community name.
            string date = _settings.NoDate ? "" : _settings.DateSimulatorRun.ToString(" yyyy-MM-dd-HH-mm-ss");
            string prefixFileName = (_settings.CommunityName + date).Trim();
            if (_settings.OutputFiles == null)
                _settings.OutputFiles = new List<OutputInformation>();
            if (_settings.TemplateFiles == null)
                _settings.TemplateFiles = new List<TemplateInformation>();

            _settings.OutputFiles.Where(f => !_settings.TemplateFiles.Any(t => t.OutputName.Equals(f.Filename))).ToList()
                .ForEach(o => o.Filename = string.Join(" ", new List<string>() { prefixFileName, o.Period, o.Filename }.Where(s => !string.IsNullOrWhiteSpace(s))));
            _settings.Reports.ToList()
                .ForEach(r => r.OutputName = string.Join(" ", new List<string>() { prefixFileName, r.OutputName }.Where(s => !string.IsNullOrWhiteSpace(s))));
            if (!string.IsNullOrWhiteSpace(_settings.WatchFile))
            {
                _settings.WatchFile = string.Join(" ", new List<string>() { prefixFileName, _settings.WatchFile }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }
        
        private void GetWorkbookData()
        {
            _workBookData = new List<MyWorksheet>();
            int numSheets = _workBook.Sheets.Count;
            // sheet index starts at 1
            for (int i = 1; i < numSheets + 1; i++)
            {
                var sheet = (Worksheet)_workBook.Sheets[i];
                var ws = new MyWorksheet()
                {
                    Name = sheet.Name
                };
                Range excelRange = sheet.UsedRange;
                var data = excelRange.Value[XlRangeValueDataType.xlRangeValueDefault];
                if (data is string)
                {
                    // one-cell worksheets evaluate to strings, and won't have any time-value data in them
                    ws.Data = null;
                }
                else
                {
                    ws.Data = (object[,])data;
                }
                _workBookData.Add(ws);
            }
        }

        private void ParseConfigData(MyWorksheet worksheet)
        {
            var data = worksheet.Data;
            if (!data[1, 1].Equals("config"))
                return;
            for (int i = 1; i <= data.GetLength(0); i++)
            {
                var val = data[i, 1];
                if (val == null)
                {
                    continue;
                }
                string s = val.ToString().ToLower().TrimStart(new[] { ' ' }).TrimEnd(new[] { ' ' });
                switch (s)
                {
                    case "config":
                    case "flattenapplication":
                        break;

                    case "simulator":
                        _settings.Simulator = data[i,2].ToString();
                        break;

                    case "directory":
                        _settings.Directory = data[i, 2].ToString();
                        // ensure ends with \\
                        if (!string.IsNullOrEmpty(_settings.Directory))
                        {
                            _settings.Directory = _settings.Directory.TrimStart(new[] { Helper.Quote });
                            _settings.Directory = _settings.Directory.TrimEnd(new[] { Helper.Quote, '\\' });
                        }
                        break;

                    case "iterations":
                        ulong iterations;
                        if (ulong.TryParse(data[i,2].ToString(), out iterations))
                        {
                            _settings.Iterations = iterations.ToString();
                        }
                        break;

                    case "input":
                        var file = data[i, 2].ToString().TrimStart(new[] { Helper.Quote }).TrimEnd(new[] { Helper.Quote });
                        var input = new InputInformation
                                        {
                                            Filename = file,
                                            Recycle = data[i, 3] != null && data[i, 3].ToString().ToLower().Equals("recycle")
                                        };
                        _settings.InputFiles.Add(input);
                        break;

                    case "output":
                        var o = new OutputInformation()
                        {
                            Filename = data[i, 2].ToString(),
                            Period = data[i, 3].ToString(),

                        };
                        for (int j = 4; j <= data.GetLength(1); j++)
                        {
                            var cell = data[i, j];
                            if (cell != null)
                                o.Variables.Add(cell.ToString());
                        }
                        _settings.OutputFiles.Add(o);
                        break;

                    case "runsimulator":
                        var cellValue = data[i, 2].ToString();
                        _settings.RunSimulator = !Helper.IsFalse(cellValue);
                        break;

                    case "community name":
                        _settings.CommunityName = data[i, 2].ToString();
                        break;

                    case "template":
                        var t = new TemplateInformation()
                        {
                            TemplateName = data[i, 2].ToString(),
                            OutputName = data[i, 3].ToString()
                        };

                        _settings.TemplateFiles.Add(t);
                        break;

                    case "log file":
                        var lf = new LogFileInformation()
                        {
                            LogFile = data[i, 2].ToString()
                        };

                        for (int j = 3; j <= data.GetLength(1); j++)
                        {
                            var cell = data[i, j];
                            if (cell != null)
                                lf.Globs.Add(cell.ToString());
                        }
                        _settings.LogInformation = lf;
                        break;

                    case "start time":
                        DateTime startDate;
                        if (DateTime.TryParse(data[i, 2].ToString(), out startDate))
                        {
                            _settings.StartDate = startDate;
                        }
                        else
                        {
                            throw new Exception("Couldn't understand the date '" + data[i, 2] + "'.");
                        }
                        break;

                    case "watch":
                        _settings.WatchFile = data[i, 2].ToString();
                        _settings.WatchGlobs = new List<string>();
                        for (int j = 3; j <= data.GetLength(1); j++)
                        {
                            var cell = data[i, j];
                            if (cell != null)
                                _settings.WatchGlobs.Add(cell.ToString());
                        }
                        break;

                    case "parameter":
                        var param = new List<string> {data[i, 2].ToString()};
                        for (int j = 3; j <= data.GetLength(1); j++)
                        {
                            var cell = data[i, j];
                            if (cell != null)
                                param.Add(cell.ToString());
                        }
                        _settings.ExtraArgList.Add(param);
                        break;

                    case "batch command":
                        var batch = new List<string> { data[i, 2].ToString() };
                        for (int j = 3; j <= data.GetLength(1); j++)
                        {
                            var cell = data[i, j];
                            if (cell != null)
                                batch.Add(cell.ToString());
                        }
                        _settings.BatchCommands.Add(batch);
                        break;

                    case "report":
                        _settings.Reports.Add(new TemplateInformation()
                            {
                                TemplateName = data[i, 2].ToString(),
                                OutputName = data[i, 3].ToString()
                            });
                        break;

                    case "eval":
                        for (int j = 2; j <= data.GetLength(1); j++)
                        {
                            var cell = data[i, j];
                            if (cell != null)
                                _settings.EvalFiles.Add(cell.ToString());
                        }
                        break;

                    default:
                        Console.WriteLine("unknown option: '" + s + "'");
                        break;
                }
            }
        }

        public void ProcessAllWorksheets()
        {
            if (_workBookData == null)
                return;

            var sheets = _workBookData.Where(s => !s.Name.Equals("config"));
            
            try
            {
                Parallel.ForEach(sheets, sheet =>
                {
                    string filename = SheetToCsv(sheet);
                    if (!string.IsNullOrEmpty(filename))
                    {
                        lock (_settings)
                        {
                            _settings.InputFiles.Add(
                                new InputInformation() { Filename = filename, Recycle = false }
                            );
                        }
                    }
                });
            }
            catch(AggregateException ae)
            {
                string errorMsg = "";
                foreach (var e in ae.InnerExceptions)
                {
                    errorMsg += e.Message + "\n";
                }
                throw new Exception(errorMsg, ae);
            }
        }

        public void ShowSimResults(string message)
        {
            lock (LockResults)
            {
                if (_workBook == null || _weOpened || message == null)
                    return;

                try
                {
                    if (_resultsSheet == null)
                    {
                        _resultsSheet = _workBook.Sheets.Cast<Worksheet>()
                            .FirstOrDefault(sheet => sheet.Name.Equals("SimResults"));
                        if (_resultsSheet == null)
                        {
                            _resultsSheet = _workBook.Sheets.Add();
                            _resultsSheet.Name = "SimResults";
                        }
                        _resultsSheet.Cells.Clear();
                        //_resultsSheet.get_Range("A1").Select(); // fails?
                        _resultsSheet.Cells[_resultsCell++, 1] = "This sheet is automatically filled.  Any edits will be lost each time you run the Simulator";
                        _resultsSheet.Cells[_resultsCell++, 1] = "Run started on " + DateTime.Now;

                        var resultCharts = (ChartObjects)_resultsSheet.ChartObjects();
                        foreach (ChartObject ch in resultCharts)
                        {
                            ch.Delete();
                        }
                        ChartObject resultChart = resultCharts.Add(150, 100, 300, 100);
                        _resultChartPage = resultChart.Chart;

                        if (_weOpened)
                            _workBook.Application.Visible = true;
                    }

                    if (message.Contains("%"))
                    {
                        _resultsSheet.Cells[_resultsCell, 1] = "Percent Complete";
                        _resultsSheet.Cells[_resultsCell, 2] = message;
                        if (_resultChartRange == null)
                        {
                            _resultChartRange = _resultsSheet.get_Range("A" + _resultsCell, "B" + _resultsCell);
                            _resultChartPage.SetSourceData(_resultChartRange);
                            _resultChartPage.ChartType = Excel.XlChartType.xlBarStacked;
                            _resultChartPage.HasLegend = false;

                            Axis axis = _resultChartPage.Axes(
                                XlAxisType.xlValue,
                                XlAxisGroup.xlPrimary);
                            axis.MaximumScaleIsAuto = false;
                            axis.MaximumScale = 1; // 1 ~ 100%
                            axis.MinimumScaleIsAuto = false;
                            axis.MinimumScale = 0;
                            axis.HasTitle = false;
                        }
                        _resultChartPage.Refresh();
                        if (message.StartsWith("100"))
                        {
                            _resultsCell++;
                            _workBook.Application.StatusBar = "";
                        }
                        else
                        {
                            _workBook.Application.StatusBar = "Running Simulation: " + message;
                        }
                    }
                    else
                    {
                        _resultsSheet.Cells[++_resultsCell, 1] = message;
                    }

                    // accidentally discovered how to make a cell corner note:
                    // bool result = excelApp.Dialogs[Excel.XlBuiltInDialog.xlDialogNote].Show();
                }
                catch
                {

                }
            }
        }

        public void CleanupMessages(bool success)
        {
            try
            {
                if (_workBook == null)
                    return;

                if (success)
                    _workBook.Application.StatusBar = "";
                else
                {
                    _workBook.Application.StatusBar = "Simulation completed with errors.";
                    if (_resultsSheet != null)
                        _resultsSheet.Activate();
                }
            }
            catch (Exception)
            {
            }
        }

        private string SheetToCsv(MyWorksheet sheet)
        {
            var data = sheet.Data;
            
            if (data == null || !data[1,1].Equals("t"))
                return null;

            StringBuilder row = new StringBuilder();
            string outputFileName = _settings.SplitFilePrefix + sheet.Name + ".csv";
            FileStream fs = new FileStream(outputFileName, FileMode.Create);
            using (StreamWriter streamWriter = new StreamWriter(fs))
            {
                for (int i = 1; i <= data.GetLength(0); i++)
                {
                    row.Clear();
                    row.Append(data[i,1]);
                    for (int j = 2; j <= data.GetLength(1); j++)
                    {
                        row.Append(Helper.Delim);
                        row.Append(data[i, j]);
                    }
                    streamWriter.WriteLine(row);
                }
                streamWriter.Flush();
            }
            return outputFileName;
        }

        public void ShowAnalyst()
        {
            Console.WriteLine("Starting Analyser.  Please wait...");
            foreach (var t in _settings.TemplateFiles)
            {
                ProcessTemplate(t.TemplateName, t.OutputName);
            }
            Console.WriteLine("Analyser finished.");
        }

        private void ProcessTemplate(string template, string output)
        {
            if (string.IsNullOrEmpty(template) || string.IsNullOrEmpty(output))
            {
                return;
            }

            FileInfo templateInfo = new FileInfo(template);
            FileInfo outputInfo = new FileInfo(output);

            if (!templateInfo.Exists || !outputInfo.Exists)
            {
                return;
            }

            Excel.Application excelApp = null;
            Workbook outputBook = null;

            try
            {
                excelApp = new Excel.Application();
                Workbook templateBook = excelApp.Workbooks.Open(templateInfo.FullName);
                outputBook = excelApp.Workbooks.Open(outputInfo.FullName);
                Worksheet autofill = null;

                // have we got data to copy?
                if (outputBook.Sheets.Count == 0)
                {
                    return;
                }

                // check whether there is an autofill sheet
                for (int i = 1; i <= templateBook.Sheets.Count; i++)
                {
                    autofill = (Worksheet)templateBook.Sheets[i];
                    if (autofill.Name == "autofill")
                    {
                        break;
                    }
                }

                if (autofill == null)
                {
                    autofill = templateBook.Sheets.Add();
                    autofill.Name = "autofill";
                }

                // copy the information from output to template
                var outputSheet = (Worksheet)outputBook.Sheets[1];
                Range excelRange = outputSheet.UsedRange;
                object[,] valueArray = (object[,])excelRange.Value[XlRangeValueDataType.xlRangeValueDefault];
                var address = outputSheet.UsedRange.Cells.Address;
                autofill.Cells.Clear();
                autofill.Range[address].Value = valueArray;

                long rowCount = autofill.UsedRange.Rows.Count;

                foreach (Worksheet sheet in templateBook.Sheets)
                {
                    if (sheet.Name != "autofill" && sheet.Name != "Helper")
                    {
                        // update the data sources for each chart
                        ChartObjects charts = (ChartObjects)sheet.ChartObjects();
                        if (charts.Count > 0)
                        {
                            foreach (ChartObject chart in charts)
                            {
                                int chartType = chart.Chart.Type;
                                var seriesCollection = (Excel.SeriesCollection)chart.Chart.SeriesCollection();
                                foreach (Series series in seriesCollection)
                                {
                                    series.Formula = UpdateFormulaRange(series.Formula, rowCount);
                                }
                            }
                        }
                        
                        // update formula cells
                        foreach (Range range in sheet.UsedRange.Cells)
                        {
                            range.Formula = UpdateFormulaRange(range.Formula, rowCount);
                        }
                    }
                }

                templateBook.SaveAs(templateInfo.DirectoryName + "\\" + 
                                    (!string.IsNullOrEmpty(_settings.CommunityName) ? _settings.CommunityName + " " : "") + 
                                    _settings.DateSimulatorRun.ToString("yyyy-MM-dd-HH-mm-ss ") + templateInfo.Name);
                excelApp.Visible = true;
            }
            catch (Exception e)
            {
                if (excelApp != null)
                {
                    foreach (Workbook book in excelApp.Workbooks)
                    {
                        book.Close(SaveChanges: false);
                    }
                    excelApp.Quit();
                }

                if (e.Message.Contains("0x800A03EC"))
                {
                    throw new Exception("Too many rows have been generated, please reduce the number of rows in the output.", e);
                }

                throw new Exception("Error opening workbook: " + e.Message, e);
            }
            finally
            {
                if (outputBook != null)
                {
                    outputBook.Close();
                }
            }
            // leave Excel open and visible
        }

        private string UpdateFormulaRange(string formula, long rowNumber)
        {
            if (!string.IsNullOrEmpty(formula) && rowNumber > 0)
            {
                // is it a formula?
                if (formula[0] == '=')
                {
                    string regex = @"autofill![\$]{0,1}[A-Z]+[\$]{0,1}[0-9]+:[\$]{0,1}[A-Z]+[\$]{0,1}[0-9]+";

                    MatchCollection collection = Regex.Matches(formula, regex);
                    if (collection.Count > 0)
                    {
                        StringBuilder buildFormula = new StringBuilder();
                        string[] splitString = Regex.Split(formula, regex, RegexOptions.None);

                        buildFormula.Append(splitString[0]);

                        for (int i = 0; i < collection.Count; i++)
                        {
                            Match match = collection[i];
                            // get the column values
                            MatchCollection columns = Regex.Matches(match.Value, @"\d+");
                            if (columns[0].Value != columns[1].Value)
                            {
                                int dollarIndex = match.Value.LastIndexOf('$');
                                buildFormula.Append(match.Value.Substring(0, dollarIndex + 1) + rowNumber);

                            }
                            else
                            {
                                buildFormula.Append(match.Value);
                            }
                            buildFormula.Append(splitString[i+1]);
                        }

                        return buildFormula.ToString();
                    }
                }
            }

            return formula;
        }

        public void Finalise()
        {
            var excelApp = _workBook.Application;
            if (_weOpened && _workBook != null)
            {
                _workBook.Close(false, _filename, null);
            }
            if (_weOpened && excelApp != null)
            {
                excelApp.Quit();
            }
            _workBook = null;
        }

        private static bool IsFileOpen(string path)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch (IOException ex)
            {
                return true;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
        }
    }
}
