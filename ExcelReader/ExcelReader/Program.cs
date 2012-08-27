using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelReader.Logic;
using OfficeOpenXml;
using System.IO;

namespace ExcelReader
{
    class Program
    {
        private static ConfigSettings _settings;
        private static string _inputFile;

        public static void Main(string[] args)
        {
            GetOpts(args);
            FileInfo config = new FileInfo(_inputFile);
            if (config.Exists)
            {
                using (FileStream theFile = new FileStream(config.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (ExcelPackage package = new ExcelPackage(theFile))
                    {
                        var book = package.Workbook;
                        if (book != null)
                        {
                            if (book.Worksheets.Any())
                            {
                                string defaultDirectory = config.Directory + "\\";
                                string defaultFilePrefix = config.Name.Replace(config.Extension, string.Empty) + "_";

                                ReadWorksheet worksheetReader = new ReadWorksheet(defaultDirectory, defaultFilePrefix);

                                // find the config worksheet
                                _settings = worksheetReader.ProcessConfigSheet(book.Worksheets);

                                // update input files with directory location
                                if (_settings.InputFiles.Any() && !string.IsNullOrEmpty(_settings.Directory))
                                {
                                    int inputFileCount = _settings.InputFiles.Count();
                                    for (int i = 0; i < inputFileCount; i++)
                                    {
                                        _settings.InputFiles[i] = _settings.Directory + _settings.InputFiles[i];
                                    }
                                }

                                if (_settings.OutputFiles.Any() && !string.IsNullOrEmpty(_settings.Directory))
                                {
                                    int outputFileCount = _settings.OutputFiles.Count();
                                    for (int i = 0; i < outputFileCount; i++)
                                    {
                                        _settings.OutputFiles[i].Filename = _settings.Directory + _settings.OutputFiles[i].Filename;
                                    }
                                }

                                // process the other worksheets
                                Parallel.ForEach(book.Worksheets, sheet =>
                                {
                                    string inputFileName = worksheetReader.ProcessWorksheets(sheet, _settings.SplitOutputFile);
                                    if (!string.IsNullOrEmpty(inputFileName))
                                    {
                                        lock (_settings)
                                        {
                                            _settings.InputFiles.Add(inputFileName);
                                        }
                                    }
                                });

                                // call the external system
                                new Simulator().Run(_settings);
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("The file '" + config.FullName + "' does not exist.");
            }
            Console.WriteLine("Process complete. Press any key to continue.");
            Console.ReadKey(true);
        }

        static void GetOpts(string[] args)
        {
            var stack = new Stack<string>(args.Reverse());
            while (stack.Count > 0)
            {
                var arg = stack.Pop();
                if (arg.Equals("--input"))
                {
                    _inputFile = stack.Pop();
                }
            }
            if (stack.Any())
            {
                throw new ArgumentException("Unknown command line arguments: '" + string.Join(", ", stack) + "'");
            }
        }
    }
}
