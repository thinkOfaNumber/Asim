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
            if (settings != null && settings.RunSimulator && !string.IsNullOrEmpty(settings.Simulator))
            {
                FileInfo simulatorLocation = new FileInfo(settings.Simulator);
                if (simulatorLocation.Exists)
                {
                    ProcessStartInfo psi = new ProcessStartInfo(settings.Simulator);
                    psi.Arguments = GenerateArguments(settings);
                    Process.Start(psi);
                }
            }
        }

        private string GenerateArguments(ConfigSettings settings)
        {
            StringBuilder args = new StringBuilder();
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
                        args.Append('"');
                        args.Append(file);
                        args.Append('"');
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
                        args.Append('"');
                        args.Append(oi.Filename);
                        args.Append('"');
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
