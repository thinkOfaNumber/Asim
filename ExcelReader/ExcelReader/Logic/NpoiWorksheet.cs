using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.SS.UserModel;

namespace ExcelReader.Logic
{
    public class NpoiWorksheet : IExcelReader
    {
        #region Variables
        private const char Delimiter = ',';
        private const char WrapperString = '"';
        private const int PropertyPosition = 0;
        private const int AttributePosition = 1;
        private const int PeriodPosition = 2;
        private const int VariablesStartPosition = 3;

        private string _filename;
        private ConfigSettings _settings;
        private IWorkbook _factory;
        private List<ISheet> _sheets;
        #endregion

        #region Constructor

        public NpoiWorksheet(string filename, ConfigSettings settings)
        {
            _filename = filename;
            _settings = settings;
            _sheets = new List<ISheet>();
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
                    _factory = WorkbookFactory.Create(theFile);
                    if (_factory != null && _factory.NumberOfSheets > 0)
                    {
                        // find the config worksheet
                        try
                        {
                            _settings = ProcessSheet();
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

        private ConfigSettings ProcessSheet()
        {
            if (_factory != null)
            {
                int sheetCount = _factory.NumberOfSheets;
                ISheet sheet;
                IRow row;
                string cellValue;
                for (int i = 0; i < sheetCount; i++)
                {
                    sheet = _factory.GetSheetAt(i);
                    if (sheet != null)
                    {
                        _sheets.Add(sheet);
                        int endRow = sheet.LastRowNum;
                        bool proceedToRead = false;
                        for (int j = 0; j <= endRow; j++)
                        {
                            row = sheet.GetRow(j);
                            // makes sure that the first cell is 0,0.
                            if (row != null && row.Cells.Any() && row.FirstCellNum == 0 && row.GetCell(0).StringCellValue.Trim() == "config")
                            {
                                proceedToRead = true;
                            }
                            // this is the config file
                            else if (row != null && proceedToRead)
                            {
                                switch (RetrieveCellValue(row.Cells[PropertyPosition]).ToLower())
                                {
                                    case "simulator":
                                        _settings.Simulator = RetrieveCellValue(row.Cells[AttributePosition]);
                                        break;
                                    case "directory":
                                        _settings.Directory = RetrieveCellValue(row.Cells[AttributePosition]);
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
                                        _settings.Iterations = RetrieveCellValue(row.Cells[AttributePosition]);
                                        break;
                                    case "input":
                                        cellValue = RetrieveCellValue(row.Cells[AttributePosition]);
                                        if (!string.IsNullOrEmpty(cellValue))
                                        {
                                            if (!cellValue.StartsWith(WrapperString.ToString()))
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
                                        OutputInformation info = RetrieveOutputInformation(row);
                                        if (info != null)
                                        {
                                            _settings.OutputFiles.Add(info);
                                        }
                                        break;
                                    case "runsimulator":
                                        cellValue = RetrieveCellValue(row.Cells[AttributePosition]).ToLower();
                                        _settings.RunSimulator = !Helper.IsFalse(cellValue);
                                        break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return _settings;
        }

        private string RetrieveCellValue(ICell cellValue)
        {
            string rVal = string.Empty;
            if (cellValue != null)
            {
                switch (cellValue.CellType)
                {
                    case CellType.Unknown:
                        break;
                    case CellType.NUMERIC:
                        rVal = cellValue.NumericCellValue.ToString();
                        break;
                    case CellType.STRING:
                        rVal = cellValue.StringCellValue.Trim();
                        break;
                    case CellType.FORMULA:
                        switch (cellValue.CachedFormulaResultType)
                        {
                            case CellType.NUMERIC:
                                rVal = cellValue.NumericCellValue.ToString();
                                break;
                            case CellType.STRING:
                                rVal = cellValue.StringCellValue.Trim();
                                break;
                        }
                        break;
                    case CellType.BLANK:
                        break;
                    case CellType.BOOLEAN:
                        break;
                    case CellType.ERROR:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return rVal;
        }


        private OutputInformation RetrieveOutputInformation(IRow row)
        {
            OutputInformation rVal = null;
            if (row != null)
            {
                string filename = RetrieveCellValue(row.Cells[AttributePosition]);
                int endColumn = row.LastCellNum;

                if (!string.IsNullOrEmpty(filename))
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
                    rVal.Period = RetrieveCellValue(row.Cells[PeriodPosition]);
                    // get the variables
                    string variable;
                    for (int i = VariablesStartPosition; i < endColumn; i++)
                    {
                        variable = RetrieveCellValue(row.Cells[i]);
                        if (!string.IsNullOrEmpty(variable))
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
            if (_factory == null && (_sheets == null || !_sheets.Any()))
            {
                return;
            }

            try
            {
                Parallel.ForEach(_sheets, sheet =>
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

        private string ProcessWorksheet(ISheet sheet, string prefix)
        {
            string fileName = null;
            if (sheet != null)
            {
                IRow row;
                int endRow = sheet.LastRowNum;
                bool proceedToRead = false;

                StringBuilder sbRow = new StringBuilder();
                string outputFileName = prefix + sheet.SheetName + ".csv";
                FileStream fs = new FileStream(outputFileName, FileMode.Create);
                using (StreamWriter streamWriter = new StreamWriter(fs))
                {
                    for (int j = 0; j <= endRow; j++)
                    {
                        row = sheet.GetRow(j);
                        sbRow.Clear();
                        // makes sure that the first cell is 0,0.
                        if (row != null && row.Cells.Any() &&
                            (proceedToRead ||
                             (row.FirstCellNum == 0 && row.GetCell(0).StringCellValue == "t")))
                        {
                            proceedToRead = true;
                            int cellCount = row.Cells.Count;
                            if (cellCount > 0)
                            {
                                sbRow.Append(RetrieveCellValue(row.Cells[0]));
                                for (int i = 1; i < cellCount; i++)
                                {
                                    sbRow.Append(Delimiter);
                                    sbRow.Append(RetrieveCellValue(row.Cells[i]));
                                }
                                streamWriter.WriteLine(sbRow);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (proceedToRead)
                    {
                        streamWriter.Flush();
                        fileName = outputFileName;
                    }
                }
            }

            return fileName;
        }
    }
}
