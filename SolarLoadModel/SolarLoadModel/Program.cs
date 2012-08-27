using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using SolarLoadModel.Actors;
using SolarLoadModel.Contracts;
using SolarLoadModel.Utils;

namespace SolarLoadModel
{
    class Program
    {
        private static Simulator _simulator;
        enum Arguments
        {
            Iterations,
            Input,
            Output,
            Path
        }

        static void Main(string[] args)
        {
            _simulator = new Simulator();
            ulong iterations = 100000;
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
                            iterations = Convert.ToUInt64(stack.Pop());
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
                        case Arguments.Path:
                            string path = stack.Pop();
                            if (!path.EndsWith("\\"))
                                path = path + "\\";
                            _simulator.Path = path;
                            break;
                    }
                }
                else
                {
                    Error("Unknown argument: " + arg);
                }
            }

            _simulator.Iterations = iterations;
            _simulator.Simulate();
        }

        static void Error(string s)
        {
            const string usage = @"
Usage:
SolarLoadModel.exe [--iterations <iterations>] [--input <filename> [...]]
        [--output [period] <varlist> [...]] [--path <pathName>]

Where:
    iterations: number of iterations to run
    period: seconds
    varlist: comma separated list of variable names (no spaces)

All paths are system-quoted (\\ in Windows).  pathName is prefixed to both
    input and output file names.

Example:
SolarLoadModel.exe --iterations 100000 --path C:\\Users\\Joe\\Data
    --input config.csv
    --output output.csv Gen1KwhTotal,Gen2KwhTotal,Gen3KwhTotal,Gen4KwhTotal
";
            Console.WriteLine(s);
            Console.WriteLine(usage);
            Environment.Exit(-1);
        }
    }
}
