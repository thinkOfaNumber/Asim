using System.IO;
using System.Linq;
using System.Text;
using OfficeOpenXml;

namespace ExcelReader.Logic
{
    class ReadWorksheet
    {
        #region Variables
        private const char Delimiter = ',';
        private const char WrapperString = '"';
        private const int PropertyPosition = 1;
        private const int AttributePosition = 2;
        private const int PeriodPosition = 3;
        private const int VariablesStartPosition = 4;

        private string _defaultDirectory;
        private string _defaultFilePrefix;

        #endregion

        #region Constructor
        public ReadWorksheet()
        {
        }

        public ReadWorksheet(string defaultDirectory, string defaultFilePrefix)
        {
            _defaultDirectory = defaultDirectory;
            _defaultFilePrefix = defaultFilePrefix;
        }

        #endregion

        #region Process Config Sheet
        public ConfigSettings ProcessConfigSheet(ExcelWorksheets lstSheets)
        {
            ConfigSettings settings = new ConfigSettings();

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
                                    settings.Simulator = RetrieveCellValue(sheet.Cells[i,AttributePosition].Value);
                                    break;
                                case "directory":
                                    settings.Directory = RetrieveCellValue(sheet.Cells[i,AttributePosition].Value);
                                    // ensure ends with \\
                                    if (!string.IsNullOrEmpty(settings.Directory))
                                    {
                                        if (settings.Directory.StartsWith(WrapperString.ToString()))
                                        {
                                            settings.Directory = settings.Directory.TrimStart(new char[] { WrapperString });
                                        }

                                        if (settings.Directory.EndsWith(WrapperString.ToString()))
                                        {
                                            settings.Directory = settings.Directory.TrimEnd(new char[] { WrapperString });
                                        }

                                        if (!settings.Directory.EndsWith("\\"))
                                        {
                                            settings.Directory = settings.Directory + "\\";
                                        }

                                        // now add the " back on
                                        settings.Directory = WrapperString + settings.Directory + WrapperString;
                                    }
                                    break;
                                case "iterations":
                                    settings.Iterations = RetrieveCellValue(sheet.Cells[i,AttributePosition].Value);
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

                                        settings.InputFiles.Add(cellValue);
                                    }
                                    break;
                                case "output":
                                    OutputInformation info = RetrieveOutputInformation(sheet, i);
                                    if(info != null)
                                    {
                                        settings.OutputFiles.Add(info);
                                    }
                                    break;
                                case "runsimulator":
                                    cellValue = RetrieveCellValue(sheet.Cells[i,AttributePosition].Value).ToLower();
                                    if (cellValue == "false" || cellValue == "f" ||
                                        cellValue == "no" || cellValue == "n" ||
                                        cellValue == "0")
                                    {
                                        settings.RunSimulator = false;
                                    }
                                    else
                                    {
                                        settings.RunSimulator = true;
                                    }
                                    break;
                            }
                        }

                        settings.SplitFileDirectory = !string.IsNullOrEmpty(settings.Directory) ? settings.Directory : _defaultDirectory;
                        settings.SplitFilePrefix = _defaultFilePrefix;
                    }
                }
            }

            return settings;
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


        public string ProcessWorksheets(ExcelWorksheet sheet, string outputFileName)
        {
            // must be t
            var startCell = sheet.GetValue(1, 1);
            if (startCell != null && startCell.ToString() == "t")
            {
                StringBuilder sbRow = new StringBuilder();
                outputFileName = outputFileName + sheet.Name + ".csv";
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
