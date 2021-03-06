﻿// Copyright (C) 2012, 2013  Power Water Corporation
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
using System.IO;
using System.Linq;
using PWC.Asim.Core.Exceptions;
using PWC.Asim.Core.Utils;

namespace PWC.Asim.ConsoleApp
{
    class Program
    {
        private static Simulator _simulator;
        private static bool _pause;
        private static DateTime? _startTime = null;
        enum Arguments
        {
            Iterations,
            Input,
            Output,
            Directory,
            NoPause,
            StartTime,
            Watch,
            Algorithm,
            GeneratorStats,
            Report,
            Eval,
            Debug
        }

        static void Main(string[] args)
        {
            ShowCopyrightNotice();
            Console.WriteLine(Version());
            var log = new Logger();
            _simulator = new Simulator(log);
            ulong iterations = 100000;
            _pause = true;
            // convert the enum to a list of strings to save retyping each option:
            var allowed = Enum.GetValues(typeof(Arguments)).Cast<Arguments>().Select(e => e.ToString()).ToList();

            string outputFile = null;

            var arglist = new SafeQueue<string>(args);
            while (arglist.Count > 0)
            {
                var arg = arglist.Dequeue();
                // Console.WriteLine("parsing: '" + arg + "'");
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
                        case Arguments.Iterations:
                            var s = arglist.Dequeue();
                            if (!ulong.TryParse(s, out iterations))
                                Error("--iterations must be followed by a whole number, not '" + s + "'");
                            break;

                        case Arguments.Input:
                            bool recycle = false;
                            string filename = arglist.Dequeue();
                            if (arglist.Peek() != null && arglist.Peek().ToLower().Equals("recycle"))
                            {
                                arglist.Dequeue();
                                recycle = true;
                            }
                            _simulator.AddInput(filename, recycle);
                            break;

                        case Arguments.Output:
                            outputFile = arglist.Dequeue();
                            uint period;
                            if (UInt32.TryParse(arglist.Peek(), out period))
                                arglist.Dequeue();
                            else
                                period = 1;

                            var vars = new List<string>();
                            var nextArg = arglist.Peek();
                            while (!string.IsNullOrEmpty(nextArg) && !nextArg.StartsWith("--"))
                            {
                                vars.Add(arglist.Dequeue());
                                nextArg = arglist.Peek();
                            }
                            _simulator.AddOutput(outputFile, vars.ToArray(), period);
                            break;

                        case Arguments.Directory:
                            string path = arglist.Dequeue();
                            try
                            {
                                Directory.SetCurrentDirectory(path);
                            }
                            catch (Exception e)
                            {
                                Error("Couldn't open directory '" + path + "'. " + e.Message);
                            }
                            break;

                        case Arguments.NoPause:
                            _pause = false;
                            break;

                        case Arguments.StartTime:
                            DateTime time;
                            string st = arglist.Dequeue();
                            if (DateTime.TryParse(st, out time))
                            {
                                _startTime = time;
                            }
                            else
                            {
                                Error("Couldn't understand start time '" + st + "'.");
                            }
                            break;

                        case Arguments.Watch:
                            string watchfile = arglist.Dequeue();
                            var watchvars = arglist.Dequeue().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            _simulator.Watchfile = watchfile;
                            _simulator.Watchvars = watchvars;
                            break;

                        case Arguments.Algorithm:
                            var controllerName = arglist.Dequeue();
                            var dllPath = arglist.Dequeue();
                            _simulator.Controllers[controllerName] = dllPath;
                            break;

                        case Arguments.GeneratorStats:
                            _simulator.GuessGeneratorState = true;
                            break;

                        case Arguments.Report:
                            _simulator.AddReport(arglist.Dequeue(), arglist.Dequeue());
                            break;

                        case Arguments.Eval:
                            _simulator.AddEval(arglist.Dequeue());
                            break;

                        case Arguments.Debug:
                            log.Debug = true;
                            log.WriteLine("Turning on debug logging");
                            log.WriteLine("program args are:");
                            log.WriteLine(string.Join(" ", args));
                            break;
                    }
                }
                else
                {
                    Error("Unknown argument: " + arg);
                }
            }

            _simulator.Iterations = iterations;
            _simulator.StartTime = _startTime;
            try
            {
                _simulator.Simulate();
            }
            catch (SimulationException e)
            {
                Error(e.Message);
            }
            catch (Exception e)
            {
                Error("Error running simulation: " + e.Message);
            }
            AnyKey();
            Environment.Exit(Convert.ToInt32(ExitCode.Success));
        }

        private static void ShowCopyrightNotice()
        {
            Console.WriteLine("Asim  Copyright (C) 2012, 2013  Power Water Corporation.");
            Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY;");
            Console.WriteLine("This is free software, and you are welcome to redistribute it");
            Console.WriteLine("under certain conditions; see the file COPYING for details.");
        }

        static void Error(string s)
        {
            const string usage = @"
Usage:
Asim.exe [--iterations <iterations>] [--input <filename> [...]]
        [--output [period] <varlist> [...]] [--directory <pathName>]
        [--starttime <time>] [--watch <file> <varlist>] [--nopause]
        [--algorithm <controllerName> <dllPath>] [--GeneratorStats]

Where:
    iterations: number of iterations to run
    period: seconds
    varlist: comma separated list of variable names (no spaces)
    time: ISO8601 or other string time

--nopause tells the application not to prompt to ""Press any key to continue""
    at the end.

All paths are system-quoted (\\ in Windows).  pathName is prefixed to both
    input and output file names.

Example:
Asim.exe --iterations 100000 --path C:\\Users\\Joe\\Data
    --input config.csv
    --output output.csv Gen1KwhTotal,Gen2KwhTotal,Gen3KwhTotal,Gen4KwhTotal
";
            Console.WriteLine("Error: " + s);
            Console.WriteLine(usage);
            AnyKey();
            Environment.Exit(Convert.ToInt32(ExitCode.Failure));
        }

        static void AnyKey()
        {
            if (!_pause)
                return;

            Console.WriteLine("Press any key to continue.");
            try
            {
                Console.ReadKey();
            }
            catch (Exception e)
            {
            }
        }

        private static string Version()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return string.Format("version {0}.{1}.{2}", version.Major, version.Minor, version.Revision);
        }
    }
}
