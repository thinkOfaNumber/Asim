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
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace ExcelReader.Logic
{
    class Simulator
    {
        private ConfigSettings _settings;

        public Simulator(ConfigSettings settings)
        {
            _settings = settings;
        }

        public void Run(Action<string> onOutputData)
        {
            if (_settings != null && _settings.RunSimulator)
            {
                if (!string.IsNullOrEmpty(_settings.Simulator))
                {
                    FileInfo simulatorLocation = new FileInfo(_settings.Simulator);
                    if (simulatorLocation.Exists)
                    {
                        Process simulator = new Process();
                        ProcessStartInfo psi = new ProcessStartInfo(_settings.Simulator);
                        psi.Arguments = GenerateArguments(_settings);

                        psi.CreateNoWindow = true;
                        psi.UseShellExecute = false;
                        psi.RedirectStandardOutput = true;
                        psi.RedirectStandardError = true;

                        simulator.StartInfo = psi;
                        simulator.OutputDataReceived += (sender, args) => onOutputData(args.Data);
                        simulator.ErrorDataReceived += (sender, args) => onOutputData(args.Data);
                        simulator.Start();
                        simulator.BeginOutputReadLine();
                        simulator.BeginErrorReadLine();
                        simulator.WaitForExit();
                        simulator.Close();
                        simulator.Dispose();
                    }
                    else
                    {
                        Console.WriteLine("The simulator doesn't exist at the following location: " +
                                          simulatorLocation.FullName);
                    }
                }
                else
                {
                    Console.WriteLine("No simulator location was provided.");
                }
            }
        }

        private string GenerateArguments(ConfigSettings settings)
        {
            StringBuilder args = new StringBuilder();

            args.Append(" --nopause ");

            if (!string.IsNullOrEmpty(settings.Iterations))
            {
                args.Append(" --iterations ");
                args.Append(settings.Iterations);
            }

            if (settings.StartDate.HasValue)
            {
                args.Append(" --StartTime \"");
                args.Append(settings.StartDate.Value.ToString("yyyy-MM-dd hh:mm:ss"));
                args.Append("\"");
            }

            if (settings.InputFiles.Any())
            {
                settings.InputFiles.ForEach(file =>
                {
                    if (!string.IsNullOrEmpty(file))
                    {
                        args.Append(" --input ");
                        args.Append(file);
                    }
                });
            }

            if (settings.OutputFiles.Any())
            {
                settings.OutputFiles.ForEach(oi =>
                {
                    if (oi != null && !string.IsNullOrEmpty(oi.Filename))
                    {
                        args.Append(" --output ");
                        args.Append(oi.Filename);
                        if(!string.IsNullOrEmpty(oi.Period))
                        {
                            args.Append(" ");
                            args.Append(DetermineOutputPeriod(oi.Period));
                        }
                        if (oi.Variables.Any())
                        {
                            args.Append(" ");
                            args.Append(string.Join(",", oi.Variables));
                        }
                    }
                });
            }

            if (!string.IsNullOrEmpty(settings.Directory))
            {
                args.Append(" --directory ");
                args.Append(settings.Directory);
            }

            
            //Console.WriteLine(args);
            return args.ToString();
        }

        private string DetermineOutputPeriod(string period)
        {
            string rVal = string.Empty;
            long outputPeriod;

            // determine whether we are already dealing with a number.
            if (!long.TryParse(period, out outputPeriod))
            {
                switch (period)
                {
                    case "day":
                    case "d":
                        rVal = new TimeSpan(1, 0, 0, 0).TotalSeconds.ToString();
                        break;
                    case "week":
                    case "w":
                        rVal = new TimeSpan(7, 0, 0, 0).TotalSeconds.ToString();
                        break;
                    case "month":
                    case "m":
                        rVal = new TimeSpan(31, 0, 0, 0).TotalSeconds.ToString();
                        break;
                    case "year":
                    case "y":
                        rVal = new TimeSpan(365, 0, 0, 0).TotalSeconds.ToString();
                        break;
                }
            }
            else
            {
                rVal = period;
            }

            return rVal;
        }
    }
}
