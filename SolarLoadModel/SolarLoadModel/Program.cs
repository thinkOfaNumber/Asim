using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using SolarLoadModel.Actors;
using SolarLoadModel.Contracts;
using SolarLoadModel.Exceptions;
using SolarLoadModel.Utils;

namespace SolarLoadModel
{
    class Program
    {
        private static Simulator _simulator;
        private static bool _pause;
        private static DateTime _startTime;
        enum Arguments
        {
            Iterations,
            Input,
            Output,
            Directory,
            Nopause,
            StartTime
        }

        static void Main(string[] args)
        {
            _simulator = new Simulator();
            ulong iterations = 100000;
            _pause = true;
            // convert the enum to a list of strings to save retyping each option:
            var allowed = Enum.GetValues(typeof(Arguments)).Cast<Arguments>().Select(e => e.ToString()).ToList();

            string outputFile = null;

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
                        case Arguments.Iterations:
                            var s = stack.Pop();
                            if (!ulong.TryParse(s, out iterations))
                                Error("--iterations must be followed by a whole number, not '" + s + "'");
                            break;

                        case Arguments.Input:
                            _simulator.AddInput(stack.Pop());
                            break;

                        case Arguments.Output:  
                            outputFile = stack.Pop();
                            uint period = 1;
                            if (UInt32.TryParse(stack.Peek(), out period))
                                stack.Pop();

                            var vars = stack.Pop().Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries);
                            _simulator.AddOutput(outputFile, vars, period);
                            break;

                        case Arguments.Directory:
                            string path = stack.Pop();
                            try
                            {
                                Directory.SetCurrentDirectory(path);
                            }
                            catch(Exception e)
                            {
                                Error("Couldn't open directory '" + path + "'. " + e.Message);
                            }
                            break;

                        case Arguments.Nopause:
                            _pause = false;
                            break;

                        case Arguments.StartTime:
                            string st = stack.Pop();
                            try
                            {
                                _startTime = DateTime.Parse(st);
                            }
                            catch (Exception e)
                            {
                                Error("Couldn't understand start time '" + st + "'.");
                            }
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
            catch(Exception e)
            {
                Error("Error running simulation: " +  e.Message);
            }
            AnyKey();
        }

        static void Error(string s)
        {
            const string usage = @"
Usage:
SolarLoadModel.exe [--iterations <iterations>] [--input <filename> [...]]
        [--output [period] <varlist> [...]] [--path <pathName>] [--nopause]

Where:
    iterations: number of iterations to run
    period: seconds
    varlist: comma separated list of variable names (no spaces)

--nopause tells the application not to prompt to ""Press any key to continue""
    at the end.

All paths are system-quoted (\\ in Windows).  pathName is prefixed to both
    input and output file names.

Example:
SolarLoadModel.exe --iterations 100000 --path C:\\Users\\Joe\\Data
    --input config.csv
    --output output.csv Gen1KwhTotal,Gen2KwhTotal,Gen3KwhTotal,Gen4KwhTotal
";
            Console.WriteLine("Error: " + s);
            Console.WriteLine(usage);
            AnyKey();
            Environment.Exit(-1);
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
    }
}
