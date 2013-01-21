// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of Excel Reader - An Excel Manipulation Program
//
// Excel Reader is free software: you can redistribute it and/or modify
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
        private static bool _attach;

        enum Arguments
        {
            Input,
            Attach
        }

        public static void Main(string[] args)
        {
            ShowCopyrightNotice();
            IExcelReader reader = null;
            GetOpts(args);
            if (string.IsNullOrEmpty(_inputFile))
            {
                Error("No input file provided.");
            }
            else
            {
                try
                {
                    reader = new AutomateWorksheet(_inputFile, Settings);
                    reader.ProcessConfigSheet(_attach);
                    reader.ProcessAllWorksheets();
                }
                catch (Exception e)
                {
                    Error("Error running reader: " + e.Message);
                }

                // call the external system
                bool success = false;
                try
                {
                    var sim = new Simulator(Settings);
                    success = sim.Run(reader.ShowSimOutput);
                    reader.Finalise();
                }
                catch (Exception e)
                {
                    Error("Error running simulation: " + e.Message);
                }

                if (success)
                {
                    try
                    {
                        reader.ShowAnalyst();
                    }
                    catch (Exception e)
                    {
                        Error("Error running analyst: " + e.Message);
                    }
                }
            }
        }

        private static void ShowCopyrightNotice()
        {
            Console.WriteLine("Excel Reader  Copyright (C) 2012, 2013  Power Water Corporation.");
            Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY;");
            Console.WriteLine("This is free software, and you are welcome to redistribute it");
            Console.WriteLine("under certain conditions; see the file COPYING for details.");
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
