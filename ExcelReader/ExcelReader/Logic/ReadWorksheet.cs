using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace ExcelReader.Logic
{
    class ReadWorksheet : IExcelReader
    {
        #region Variables
        private const char Delimiter = ',';
        private const char WrapperString = '"';
        private const int PropertyPosition = 1;
        private const int AttributePosition = 2;
        private const int PeriodPosition = 3;
        private const int VariablesStartPosition = 4;

        private string _filename;
        private ConfigSettings _settings;
        private ExcelWorkbook _book;

        #endregion

        #region Constructor

        public ReadWorksheet(string filename, ConfigSettings settings)
        {
            _filename = filename;
            _settings = settings;
        }

        #endregion

        #region Process Config Sheet

        public void ProcessConfigSheet(bool attachToRunningProcess)
        {
            if (attachToRunningProcess)
            {
                throw new Exception("not implemented");
            }

            FileInfo config = new FileInfo(_filename);
            if (config.Exists)
            {
                Directory.SetCurrentDirectory(config.DirectoryName);
                using (FileStream theFile = new FileStream(config.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (ExcelPackage package = new ExcelPackage(theFile))
                    {
                        _book = package.Workbook;
                        if (_book != null)
                        {
                            if (_book.Worksheets.Any())
                            {
                                // find the config worksheet
                                try
                                {
                                    _settings = ProcessSheet(_book.Worksheets);
                                }
                                catch (Exception e)
                                {
                                    throw new Exception("Error reading config worksheets: " + e.Message, e);
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
                                _settings.SplitFilePrefix = config.Name.Replace(config.Extension, "_");
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("The file '" + config.FullName + "' does not exist.");
            }
        }

        private ConfigSettings ProcessSheet(ExcelWorksheets lstSheets)
        {
            if (lstSheets.Any())
            {
                foreach (var sheet in lstSheets)
                {
                    // check whether the first cell is config
                    if (sheet.Cells[1,1].Value.ToString() == "config")
                    {
                        int endRow = sheet.Dimension.End.Row;
                        string cellValue;
                        for (int i = 1; i <= endRow; i++)
                        {
                            switch (RetrieveCellValue(sheet.Cells[i, PropertyPosition].Value).Trim().ToLower())
                            {
                                case "simulator":
                                    _settings.Simulator = RetrieveCellValue(sheet.Cells[i,AttributePosition].Value);
                                    break;
                                case "directory":
                                    _settings.Directory = RetrieveCellValue(sheet.Cells[i,AttributePosition].Value);
                                    // ensure ends with \\
                                    if (!string.IsNullOrEmpty(_settings.Directory))
                                    {
                                        if (_settings.Directory.StartsWith(WrapperString.ToString()))
                                        {
                                            _settings.Directory = _settings.Directory.TrimStart(new char[] { WrapperString });
                                        }

                                        if (_settings.Directory.EndsWith(WrapperString.ToString()))
                                        {
                                            _settings.Directory = _settings.Directory.TrimEnd(new char[] { WrapperString });
                                        }

                                        if (_settings.Directory.EndsWith("\\"))
                                        {
                                            _settings.Directory = _settings.Directory.TrimEnd(new char[] { '\\' });
                                        }
                                    }
                                    break;
                                case "iterations":
                                    _settings.Iterations = RetrieveCellValue(sheet.Cells[i,AttributePosition].Value);
                                    break;
                                case "input":
                                    cellValue = RetrieveCellValue(sheet.Cells[i,AttributePosition].Value);
                                    if(!string.IsNullOrEmpty(cellValue))
                                    {
                                        if(!cellValue.StartsWith(WrapperString.ToString()))
                                        {
                                            cellValue = WrapperString + cellValue;
                                        }

                                        if (!cellValue.EndsWith(WrapperString.ToString()))
                                        {
                                            cellValue = cellValue + WrapperString;
                                        }

                                        _settings.InputFiles.Add(cellValue);
                                    }
                                    break;
                                case "output":
                                    OutputInformation info = RetrieveOutputInformation(sheet, i);
                                    if(info != null)
                                    {
                                        _settings.OutputFiles.Add(info);
                                    }
                                    break;
                                case "runsimulator":
                                    cellValue = RetrieveCellValue(sheet.Cells[i,AttributePosition].Value).ToLower();
                                    _settings.RunSimulator = !Helper.IsFalse(cellValue);
                                    break;
                            }
                        }
                    }
                }
            }

            return _settings;
        }

        private string RetrieveCellValue(object cellValue)
        {
            string rVal = string.Empty;
            if(cellValue != null)
            {
                rVal = cellValue.ToString().Trim();
            }
            return rVal;
        }

        private OutputInformation RetrieveOutputInformation(ExcelWorksheet sheet, int currentRow)
        {
            OutputInformation rVal = null;
            if(sheet != null)
            {
                string filename = RetrieveCellValue(sheet.Cells[currentRow,AttributePosition].Value);
                int endColumn = sheet.Dimension.End.Column;
                
                if(!string.IsNullOrEmpty(filename))
                {
                    rVal = new OutputInformation();
                    // get the filename
                    if (!filename.StartsWith(WrapperString.ToString()))
                    {
                        filename = WrapperString + filename;
                    }

                    if (!filename.EndsWith(WrapperString.ToString()))
                    {
                        filename = filename + WrapperString;
                    }
                    rVal.Filename = filename;
                    // get the period
                    rVal.Period = RetrieveCellValue(sheet.Cells[currentRow,PeriodPosition].Value);
                    // get the variables
                    string variable;
                    for (int i = VariablesStartPosition; i <= endColumn; i++)
                    {
                        variable = RetrieveCellValue(sheet.Cells[currentRow,i].Value);
                        if(!string.IsNullOrEmpty(variable))
                        {
                            rVal.Variables.Add(variable);
                        }
                    }
                }
            }
            return rVal;
        }
        #endregion

        public void ProcessAllWorksheets()
        {
            if (_book == null)
            {
                return;
            }
            // process the other worksheets
            try
            {
                Parallel.ForEach(_book.Worksheets, sheet =>
                {
                    string inputFileName = ProcessWorksheet(sheet, _settings.SplitFilePrefix);
                    if (!string.IsNullOrEmpty(inputFileName))
                    {
                        lock (_settings)
                        {
                            _settings.InputFiles.Add(WrapperString + inputFileName + WrapperString);
                        }
                    }
                });
            }
            catch (Exception e)
            {
                throw new Exception("Error splitting worksheets: " + e.Message, e);
            }

        }

        private string ProcessWorksheet(ExcelWorksheet sheet, string prefix)
        {
            // must be t
            var startCell = sheet.GetValue(1, 1);
            if (startCell != null && startCell.ToString() == "t")
            {
                StringBuilder sbRow = new StringBuilder();
                string outputFileName = prefix + sheet.Name + ".csv";
                FileStream fs = new FileStream(outputFileName, FileMode.Create);
                using (StreamWriter streamWriter = new StreamWriter(fs))
                {
                    int endColumn = sheet.Dimension.End.Column;
                    int startColumn = sheet.Dimension.Start.Column;
                    int endRow = sheet.Dimension.End.Row;
                    int startRow = sheet.Dimension.Start.Row;

                    for (int row = startRow; row <= endRow; row++)
                    {
                        sbRow.Clear();
                        for (int column = startColumn; column <= endColumn; column++)
                        {
                            var cell = sheet.Cells[row, column].Value;
                            //sbRow.Append(WrapperString);
                            if (cell != null)
                            {
                                sbRow.Append(cell);
                            }
                            //sbRow.Append(WrapperString);
                            if (column != endColumn)
                            {
                                sbRow.Append(Delimiter);
                            }
                        }
                        streamWriter.WriteLine(sbRow);

                    }
                    // write the information out
                    streamWriter.Flush();
                }

                return outputFileName;
            }

            return null;
        }
    }
}
