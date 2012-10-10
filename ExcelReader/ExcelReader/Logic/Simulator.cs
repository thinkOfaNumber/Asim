using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace ExcelReader.Logic
{
    class Simulator
    {
        public void Run(ConfigSettings settings)
        {
            if (settings != null && settings.RunSimulator)
            {
                if (!string.IsNullOrEmpty(settings.Simulator))
                {
                    FileInfo simulatorLocation = new FileInfo(settings.Simulator);
                    if (simulatorLocation.Exists)
                    {
                        Process simulator = new Process();
                        ProcessStartInfo psi = new ProcessStartInfo(settings.Simulator);
                        psi.Arguments = GenerateArguments(settings);

                        psi.CreateNoWindow = true;
                        psi.UseShellExecute = false;
                        psi.RedirectStandardOutput = true;
                        psi.RedirectStandardError = true;

                        simulator.StartInfo = psi;
                        simulator.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                        simulator.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);
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
