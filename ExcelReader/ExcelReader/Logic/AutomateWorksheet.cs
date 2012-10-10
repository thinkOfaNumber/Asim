using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ExcelReader.Interface;
using Excel = Microsoft.Office.Interop.Excel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;

namespace ExcelReader.Logic
{
    public class AutomateWorksheet : IExcelReader
    {
        private ConfigSettings _settings;
        private string _filename;
        Workbook _workBook;
        private const char q = '"';
        private const char delim = ',';
        private bool _attach;

        public AutomateWorksheet(string inputFile, ConfigSettings settings)
        {
            _settings = settings;
            _filename = inputFile;
        }


        public void ProcessConfigSheet(bool attachToRunningProcess)
        {
            _attach = attachToRunningProcess;
            var fileInfo = new FileInfo(_filename);
            try
            {
                Directory.SetCurrentDirectory(fileInfo.DirectoryName);
                if (_attach)
                {
                    //_workBook = System.Runtime.InteropServices.Marshal.BindToMoniker(_filename) as Excel.Workbook;
                    _workBook = System.Runtime.InteropServices.Marshal.BindToMoniker(fileInfo.FullName) as Excel.Workbook;
                }
                else
                {
                    var excelApp = new Excel.Application();
                    //_workBook = excelApp.Workbooks.Open(_filename);
                    _workBook = excelApp.Workbooks.Open(fileInfo.FullName);
                }
                _workBook.Application.StatusBar = "Running Simulation";
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("couldn't {0} Excel workbook: {1}",
                    _attach ? "attach to running" : "open",
                    e.Message));
            }

            try
            {
                object[,] config = GetConfigSheetData();
                if (config != null)
                {
                    ParseConfigData(config);
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
                _settings.SplitFilePrefix = fileInfo.Name.Replace(fileInfo.Extension, "_");
            }
            catch (Exception e)
            {
                if (!_attach)
                {
                    _workBook.Close(false, _filename, null);
                }
                //Marshal.ReleaseComObject(_workBook);
                throw new Exception("Error opening workbook: " + e.Message, e);
            }
        }

        private object[,] GetConfigSheetData()
        {
            int numSheets = _workBook.Sheets.Count;
            // sheet index starts at 1
            for (int i = 1; i < numSheets + 1; i++)
            {
                var sheet = (Worksheet)_workBook.Sheets[i];
                if (sheet.Name == "config")
                {
                    Range excelRange = sheet.UsedRange;
                    object[,] valueArray = (object[,])excelRange.Value[XlRangeValueDataType.xlRangeValueDefault];
                    return valueArray;
                }
            }
            throw new Exception("Couldn't find config tab.");
            return null;
        }

        private void ParseConfigData(object[,] data)
        {
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
                    case "simulator":
                        _settings.Simulator = data[i,2].ToString();
                        break;

                    case "directory":
                        _settings.Directory = data[i, 2].ToString();
                        // ensure ends with \\
                        if (!string.IsNullOrEmpty(_settings.Directory))
                        {
                            _settings.Directory = _settings.Directory.TrimStart(new [] { q });
                            _settings.Directory = _settings.Directory.TrimEnd(new [] { q, '\\' });
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
                        var file = data[i, 2].ToString().TrimStart(new[] { q }).TrimEnd(new[] { q });
                        file = q + file + q;
                        _settings.InputFiles.Add(file);
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

                    default:
                        Console.WriteLine("unknown option: '" + s + "'");
                        break;
                }
            }
        }

        public void ProcessAllWorksheets()
        {
            if (_workBook == null)
                return;

            int numSheets = _workBook.Sheets.Count;
            var sheets = new List<Worksheet>();
            // sheet index starts at 1
            for (int i = 1; i < numSheets + 1; i++)
            {
                var sheet = (Worksheet)_workBook.Sheets[i];
                if (sheet.Name != "config")
                {
                    sheets.Add(sheet);
                }
            }
            
            try
            {
                Parallel.ForEach(sheets, sheet =>
                {
                    string filename = SheetToCsv(sheet);
                    if (!string.IsNullOrEmpty(filename))
                    {
                        lock (_settings)
                        {
                            _settings.InputFiles.Add(q + filename + q);
                        }
                    }
                });
            }
            catch (Exception e)
            {
                throw new Exception("Error splitting worksheets: " + e.Message, e);
            }
            finally
            {
                if (!_attach)
                {
                    _workBook.Close(false, _filename, null);
                }
                //Marshal.ReleaseComObject(_workBook);
            }
        }

        private string SheetToCsv(Worksheet sheet)
        {
            Range excelRange = sheet.UsedRange;
            object[,] data = (object[,])excelRange.Value[XlRangeValueDataType.xlRangeValueDefault];
            
            
            if (!data[1,1].Equals("t"))
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
                        row.Append(delim);
                        row.Append(data[i, j]);
                    }
                    streamWriter.WriteLine(row);
                }
                streamWriter.Flush();
            }
            return outputFileName;
        }

        public void GenerateGraphs(string template, string output)
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

            try
            {
                excelApp = new Excel.Application();
                Workbook templateBook = excelApp.Workbooks.Open(templateInfo.FullName);
                Workbook outputBook = excelApp.Workbooks.Open(outputInfo.FullName);
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
                    if (sheet.Name != "autofill")
                    {
                        // update the data sources for each chart
                        ChartObjects charts = (ChartObjects)sheet.ChartObjects();
                        if (charts.Count > 0)
                        {
                            foreach (ChartObject chart in charts)
                            {
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

                templateBook.SaveAs(templateInfo.DirectoryName + "\\" + DateTime.Now.ToString("yyyyMMdd_HHmm_") + templateInfo.Name);
            }
            catch (Exception e)
            {
                throw new Exception("Error opening workbook: " + e.Message, e);
            }
            finally
            {
                if (excelApp != null)
                {
                    foreach (Workbook book in excelApp.Workbooks)
                    {
                        book.Close(SaveChanges: false);
                    }
                    excelApp.Quit();
                }
            }
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

    }
}
