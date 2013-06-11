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
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace PWC.Asim.ExcelTools.Logic
{
    class Simulator
    {
        private ConfigSettings _settings;

        public Simulator(ConfigSettings settings)
        {
            _settings = settings;
        }

        public bool Run(Action<string> onOutputData, Action<bool> onExit)
        {
            bool success = false;
            if (_settings != null && _settings.RunSimulator)
            {
                FileInfo simulatorLocation = new FileInfo(_settings.Simulator);
                if (simulatorLocation.Exists)
                {
                    var cliArgs = GenerateArguments(_settings);
                    Console.WriteLine();
                    Console.WriteLine("Spawning simulator with options:");
                    Console.WriteLine(cliArgs);
                    Console.WriteLine("Please wait...");
                    Process simulator = new Process();
                    ProcessStartInfo psi = new ProcessStartInfo(_settings.Simulator);
                    psi.Arguments = cliArgs;

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
                    success = simulator.ExitCode == 0;
                    onExit(success);
                    simulator.Close();
                    simulator.Dispose();
                    Console.WriteLine("simulator finished.");
                }
                else
                {
                    Console.WriteLine("The simulator doesn't exist at the following location: " +
                                        simulatorLocation.FullName);
                }
            }
            return success;
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
                args.Append(settings.StartDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                args.Append("\"");
            }

            settings.InputFiles.ForEach(file =>
            {
                if (!string.IsNullOrEmpty(file.Filename))
                {
                    args.Append(" --input ");
                    args.Append(file.Filename);
                    if (file.Recycle)
                        args.Append(" recycle ");
                }
            });

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
                            args.Append(string.Join(" ", oi.Variables));
                        }
                    }
                });
            }

            if (!string.IsNullOrEmpty(settings.Directory))
            {
                args.Append(" --directory ");
                args.Append(settings.Directory);
            }

            if (!string.IsNullOrWhiteSpace(settings.WatchFile) && settings.WatchGlobs.Any())
            {
                args.Append(" --watch ");
                args.Append(settings.WatchFile);
                args.Append(" ");
                args.Append(string.Join(",", settings.WatchGlobs));
            }

            settings.ExtraArgList.ForEach(a => {
                args.Append(" --" + a[0]);
                a.RemoveAt(0);
                a.ForEach(i => args.Append(" " + i));
            });
            
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
