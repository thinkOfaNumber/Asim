// Copyright (C) 2012, 2013  Power Water Corporation
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ConsoleTests
{
    public enum Arguments
    {
        Iterations = 0,
        Input,
        Output,
        Directory,
        Nopause,
        StartTime
    }

    public class TestBase
    {
        public StringBuilder ConsoleOutput { get; set; }
        private readonly Queue<string> _tempFiles = new Queue<string>();

        public string GetTempFilename
        {
            get
            {
                string f = Path.GetRandomFileName();
                f = Path.GetTempPath() + f.Replace(".", "") + "slms.tmp";
                _tempFiles.Enqueue(f);
                // Console.WriteLine();
                _normalOutput.WriteLine("Serving file: " + f);
                return f;
            }
        }

        protected TextWriter _normalOutput = System.Console.Out;
        protected StringWriter _testingConsole;
        private const string ConsoleDir = @"..\..\..\PWC.Asim.ConsoleApp\bin\Debug\";
        private const string ConsoleApp = "Asim.exe";
        private const string ExcelApp = @"..\..\..\PWC.Asim.ExcelTools\bin\Debug\AsimExcelTools.exe";
        protected const string TestDir = @"..\..\tmp";


        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            // Set current folder to testing folder
            string assemblyCodeBase = System.Reflection.Assembly. GetExecutingAssembly().CodeBase;

            // remove URL-prefix if it exists
            if (assemblyCodeBase.StartsWith("file:///"))
                assemblyCodeBase = assemblyCodeBase.Substring(8);

            // Get directory name
            string dirName = Path.GetDirectoryName(assemblyCodeBase);

            // remove URL-prefix if it exists
            if (dirName.StartsWith("file:\\"))
                dirName = dirName.Substring(6);

            // set current folder
            Environment.CurrentDirectory = dirName;
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            while (_tempFiles.Any())
            {
                string f = _tempFiles.Dequeue();
                try
                {
                    File.Delete(f);
                }
                catch(Exception e)
                {
                    _tempFiles.Enqueue(f);
                }
            }
        }

        /// <summary>
        /// writes string to normal non-redirected console for unit test output
        /// </summary>
        /// <param name="s"></param>
        public void WriteLine(string s)
        {
            _normalOutput.WriteLine(s);
        }

        /// <summary>
        /// Starts the console application.
        /// </summary>
        /// <param name="arguments">The arguments for console application. 
        /// Specify empty string to run with no arguments</param>
        /// <returns>exit code of console app</returns>
        public int StartConsoleApplication(string arguments)
        {
            // Initialize process here
            Process proc = new Process();
            proc.StartInfo.FileName = ConsoleDir + ConsoleApp;
            // add arguments as whole string
            proc.StartInfo.Arguments = "--nopause " + arguments;

            // use it to start from testing environment
            proc.StartInfo.UseShellExecute = false;

            // redirect outputs to have it in testing console
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;

            // set working directory
            proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

            // start and wait for exit
            proc.Start();
            proc.WaitForExit();

            // get output to testing console.
            System.Console.WriteLine(proc.StandardOutput.ReadToEnd());
            System.Console.Write(proc.StandardError.ReadToEnd());

            // return exit code
            return proc.ExitCode;
        }

        /// <summary>
        /// Starts the Excel integration application
        /// </summary>
        /// <param name="arguments">The arguments for the Excel application. 
        /// Specify empty string to run with no arguments</param>
        /// <returns>exit code of console app</returns>
        public int StartExcelApplication(string arguments)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = ExcelApp;
            // add arguments as whole string
            proc.StartInfo.Arguments = "--NoDate " + arguments;

            // use it to start from testing environment
            proc.StartInfo.UseShellExecute = false;

            // redirect outputs to have it in testing console
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;

            // set working directory
            // proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

            // start and wait for exit
            proc.Start();
            proc.WaitForExit();

            // get output to testing console.
            System.Console.WriteLine(proc.StandardOutput.ReadToEnd());
            System.Console.Write(proc.StandardError.ReadToEnd());

            // return exit code
            return proc.ExitCode;
        }

        /// <summary>
        /// Converts an enum argument into a string to be used on the command line
        /// </summary>
        /// <param name="arg">Argument</param>
        /// <returns>string value to pass to console app</returns>
        public string ConsoleArguments(Arguments arg)
        {
            return "--" + arg;
        }

        public StringBuilder BuildCsvFor<T>(string varName, T[] values, int period = 1)
        {
            var sb = new StringBuilder(values.Length * 10);
            sb.Append("t,");
            sb.Append(varName);
            sb.Append("\n");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(i * period);
                sb.Append(",");
                if (values[i] is double)
                {
                    sb.Append(Convert.ToDouble(values[i]).ToString("0.0000"));
                }
                else
                {
                    sb.Append(values[i].ToString());
                }
                sb.Append("\n");
            }
            return sb;
        }

        public StringBuilder BuildCsvFor<T>(List<string> varName, T[][] values, int period = 1)
        {
            if (!values.Any() || !varName.Any())
            {
                throw new ArgumentException("Unexpected number of cells.");
            }

            var sb = new StringBuilder();
            sb.Append("t,");
            sb.Append(string.Join(",", varName));
            sb.Append("\n");
            int numRows = values[0].Length;
            int numCols = varName.Count;
            for (int row = 0; row < numRows; row++)
            {
                for (int col = 0; col < numCols; col++)
                {
                    if (col == 0)
                    {
                        sb.Append(row * period);
                    }
                    sb.Append(",");
                    if (values[col][row] is double)
                    {
                        sb.Append(Convert.ToDouble(values[col][row]).ToString("0.0000"));
                    }
                    else
                    {
                        sb.Append(values[col][row].ToString());
                    }
                }
                sb.Append("\n");
            }
            return sb;
        }

        public List<string[]> CsvFileToArray(string filename)
        {
            var file = new StreamReader(filename);
            var values = new List<string[]>();

            string line;
            while ((line = file.ReadLine()) != null)
            {
                values.Add(line.Split(','));
            }
            return values;
        }

        public bool DoublesAreEqual(double a, double b)
        {
            double diff = Math.Abs(a - b);
            if (diff < 0.0001)
            {
                return true;
            }
            else
            {
                Console.WriteLine("Double precision off by: " + diff);
                return false;
            }
        }

        /// <summary>
        /// Fills an array of doubles with random data
        /// </summary>
        /// <param name="array">Array to be filled.  Existing values will be clobbered.</param>
        /// <param name="low">lower bound of all random numbers</param>
        /// <param name="high">upper bound of all random numbers</param>
        public void FillWithRandomData<T>(ref T[] array, int low = 0, int high = 1)
            where T : struct, IComparable, IFormattable, IConvertible, IComparable<T>, IEquatable<T>
        {
            var r = new Random();
            array = new T[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = (T)Convert.ChangeType(r.NextDouble() * (high - low) + low, typeof(T)); //r.NextDouble() * (high - low) + low;
            }
        }

        /// <summary>
        /// Inserts the given constant fuel consumption into the value dictionary
        /// </summary>
        /// <param name="values">dictionary of values for testing</param>
        /// <param name="fuelConst">fuel consumption L/kWh</param>
        /// <param name="nGens">number of Generators to insert values for</param>
        public void InsertFuelConsumption(SortedDictionary<string, double[]> values, double fuelConst, int nGens = 1)
        {
            for (int i = 1; i < nGens + 1; i++)
            {
                values["Gen" + i + "FuelCons1P"] = new double[] { 0 };
                values["Gen" + i + "FuelCons1L"] = new double[] { fuelConst };
                values["Gen" + i + "FuelCons2P"] = new double[] { 1 };
                values["Gen" + i + "FuelCons2L"] = new double[] { fuelConst };
                values["Gen" + i + "FuelCons3P"] = new double[] { 0 };
                values["Gen" + i + "FuelCons3L"] = new double[] { 0 };
                values["Gen" + i + "FuelCons4P"] = new double[] { 0 };
                values["Gen" + i + "FuelCons4L"] = new double[] { 0 };
                values["Gen" + i + "FuelCons5P"] = new double[] { 0 };
                values["Gen" + i + "FuelCons5L"] = new double[] { 0 };
            }
        }
    }
}
