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

        public bool RunBatchCommand(List<string> cmd, Action<string> onOutputData, string excelfile)
        {
            bool success = true;
            if (cmd.Any())
            {
                FileInfo location = new FileInfo(cmd[0]);
                StringBuilder args = new StringBuilder(Helper.Quote);
                cmd.Where((s, i) => i > 0).ToList().ForEach(c =>
                    {
                        args.Append(c);
                        args.Append(Helper.Quote + " ");
                    });
                var environment = new Dictionary<string, string>
                    {
                        {"ASIM_INPUTFILES", string.Join(Helper.Sdelim, _settings.InputFiles.Select(f => f.Filename))},
                        {"ASIM_OUTPUTFILES", string.Join(Helper.Sdelim, _settings.OutputFiles.Select(f => f.Filename))},
                        {"ASIM_COMMUNITYNAME", _settings.CommunityName},
                        {"ASIM_ITERATIONS", _settings.Iterations},
                        {
                            "ASIM_STARTTIME",
                            _settings.StartDate.HasValue
                                ? _settings.StartDate.Value.ToString("yyyy-MM-dd HH:mm:ss")
                                : ""
                        },
                        {"ASIM_DIRECTORY", _settings.Directory},
                        {"ASIM_EXCELFILE", excelfile}
                    };
                success = Run(onOutputData, null, location, string.Join(Helper.Sdelim, args.ToString()), environment);
            }
            return success;
        }

        public bool Run(Action<string> onOutputData, Action<bool> onExit)
        {
            bool success = true;
            if (_settings != null && _settings.RunSimulator)
            {
                FileInfo simulatorLocation = new FileInfo(_settings.Simulator);
                success = Run(onOutputData, onExit, simulatorLocation, GenerateArguments(_settings));
            }
            return success;
        }

        private bool Run(Action<string> onOutputData, Action<bool> onExit, FileInfo execute, string cliArgs, Dictionary<string,string> env = null)
        {
            bool success = false;

                if (execute.Exists)
                {
                    Console.WriteLine();
                    Console.WriteLine("Running '" + execute + "' with options:");
                    Console.WriteLine(cliArgs);
                    Console.WriteLine("Please wait...");
                    Process runner = new Process();
                    ProcessStartInfo psi = new ProcessStartInfo(execute.FullName);
                    psi.Arguments = cliArgs;

                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    if (env != null)
                    {
                        foreach (KeyValuePair<string, string> e in env)
                        {
                            psi.EnvironmentVariables.Add(e.Key, e.Value);
                        }
                    }

                    runner.StartInfo = psi;
                    if (onOutputData != null)
                    {
                        runner.OutputDataReceived += (sender, args) => onOutputData(args.Data);
                        runner.ErrorDataReceived += (sender, args) => onOutputData(args.Data);
                    }
                    runner.Start();
                    runner.BeginOutputReadLine();
                    runner.BeginErrorReadLine();
                    runner.WaitForExit();
                    success = runner.ExitCode == 0;
                    if (onExit != null)
                        onExit(success);
                    runner.Close();
                    runner.Dispose();
                    Console.WriteLine("Finished.");
                }
                else
                {
                    Console.WriteLine("The executable doesn't exist at the following location: " +
                                        execute.FullName);
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
                    args.Append(Helper.Quote + file.Filename + Helper.Quote);
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
                        args.Append(Helper.Quote + oi.Filename + Helper.Quote);
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

            //if (!string.IsNullOrEmpty(settings.Directory))
            //{
            //    args.Append(" --directory ");
            //    args.Append(Helper.Quote + settings.Directory + Helper.Quote);
            //}

            if (!string.IsNullOrWhiteSpace(settings.WatchFile) && settings.WatchGlobs.Any())
            {
                args.Append(" --watch ");
                args.Append(Helper.Quote + settings.WatchFile + Helper.Quote);
                args.Append(" ");
                args.Append(string.Join(Helper.Sdelim, settings.WatchGlobs));
            }

            settings.ExtraArgList.ForEach(a => {
                args.Append(" --" + a[0]);
                a.RemoveAt(0);
                a.ForEach(i => args.Append(" " + i));
            });

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
