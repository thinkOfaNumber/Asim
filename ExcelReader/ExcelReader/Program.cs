using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelReader.Interface;
using ExcelReader.Logic;
using System.IO;

namespace ExcelReader
{
    class Program
    {
        private static readonly ConfigSettings Settings = new ConfigSettings();
        private static string _inputFile;
        private const char WrapperString = '"';
        private static bool _attach;

        enum Arguments
        {
            Input,
            Attach
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
                    IExcelReader reader = new AutomateWorksheet(_inputFile, Settings);
                    reader.ProcessConfigSheet(_attach);
                    reader.ProcessAllWorksheets();
                }
                catch (Exception e)
                {
                    Error("Error running reader: " + e.Message);
                }

                // add the quotes to the directory
                if (!string.IsNullOrEmpty(Settings.Directory))
                {
                    Settings.Directory = WrapperString + Settings.Directory + WrapperString;
                }

                // prefix the community name.
                if (!string.IsNullOrEmpty(Settings.CommunityName) && 
                    Settings.OutputFiles != null && 
                    Settings.OutputFiles.Any())
                {
                    Settings.OutputFiles.ForEach(o => o.Filename = Settings.CommunityName + "_" + o.Filename);
                }

                // call the external system
                try
                {
                    new Simulator().Run(Settings);
                }
                catch (Exception e)
                {
                    Error("Error running simulation: " + e.Message);
                }

                try
                {
                    IExcelReader reader = new AutomateWorksheet(_inputFile, Settings);
                    Settings.TemplateFiles.ForEach(tf => reader.GenerateGraphs(tf.TemplateName, tf.OutputName));
                }
                catch (Exception e)
                {
                    Error("Error: " + e.Message);
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

                        case Arguments.Attach:
                            _attach = true;
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
