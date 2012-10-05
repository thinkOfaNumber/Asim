using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
    }
}
