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
        private const char WrapperString = '"';

        enum Arguments
        {
            Input
        }

        public static void Main(string[] args)
        {
            GetOpts(args);
            if (string.IsNullOrEmpty(_inputFile))
            {
                Error("No input file provided.");
            }
            else
            {
                try
                {
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
                                        try
                                        {
                                            _settings = worksheetReader.ProcessConfigSheet(book.Worksheets);
                                        }
                                        catch (Exception e)
                                        {
                                            Error("Error reading config worksheets: " + e.Message);
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
                                                Error("Error setting directory: " + e.Message);
                                            }
                                        }
                                        else
                                        {
                                            try
                                            {
                                                Directory.SetCurrentDirectory(_settings.SplitFileDirectory);
                                            }
                                            catch (Exception e)
                                            {
                                                Error("Error setting directory: " + e.Message);
                                            }
                                        }


                                        // process the other worksheets
                                        string currentDirectory = Directory.GetCurrentDirectory();
                                        try
                                        {
                                            Parallel.ForEach(book.Worksheets, sheet =>
                                            {
                                                string inputFileName = worksheetReader.ProcessWorksheets(sheet, currentDirectory, _settings.SplitFilePrefix);
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
                                            Error("Error splitting worksheets: " + e.Message);
                                        }

                                        // add the quotes to the directory
                                        if (!string.IsNullOrEmpty(_settings.Directory))
                                        {
                                            _settings.Directory = WrapperString + _settings.Directory + WrapperString;
                                        }

                                        // call the external system
                                        try
                                        {
                                            new Simulator().Run(_settings);
                                        }
                                        catch (Exception e)
                                        {
                                            Error("Error running simulation: " + e.Message);
                                        }

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
                catch (Exception e)
                {
                    Error("Error running reader: " + e.Message);
                }
            }
            AnyKey();
        }

        static void GetOpts(string[] args)
        {
            var allowed = Enum.GetValues(typeof(Arguments)).Cast<Arguments>().Select(e => e.ToString()).ToList();

            var stack = new Stack<string>(args.Reverse());
            while (stack.Count > 0)
            {
                var arg = stack.Pop();
                if (!arg.StartsWith("--"))
                {
                    Error("Unknown argument: " + arg);
                }
                arg = arg.Substring(2);
                if (allowed.Count(s => s.ToLower().StartsWith(arg.ToLower())) > 1)
                {
                    Error("Ambiguous argument: " + arg);
                }

                Arguments thisArg;
                if (Enum.TryParse(arg, true, out thisArg))
                {
                    switch (thisArg)
                    {
                        case Arguments.Input:
                            _inputFile = stack.Pop();
                            break;
                    }
                }
                else
                {
                    Error("Unknown argument: " + arg);
                }
            }
        }

        static void Error(string s)
        {
            StringBuilder usage = new StringBuilder();
            usage.Append(Environment.NewLine);
            usage.Append("Useage:");
            usage.Append(Environment.NewLine);
            usage.Append("ExcelReader.exe [--input <filename>]");
            usage.Append(Environment.NewLine);
            usage.Append(Environment.NewLine);
            usage.Append("Example:");
            usage.Append(Environment.NewLine);
            usage.Append("ExcelReader.exe --input \"C:\\Users\\Joe\\Data\\Example.xlsm\"");
            usage.Append(Environment.NewLine);

            Console.WriteLine("Error: " + s);
            Console.WriteLine(usage);
            AnyKey();
            Environment.Exit(-1);
        }

        static void AnyKey()
        {
            Console.WriteLine("Press any key to continue.");
            try
            {
                Console.ReadKey();
            }
            catch (Exception e)
            {
            }
        }

    }
}
