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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using PWC.Asim.Core.Actors;
using PWC.Asim.Core.Contracts;
using PWC.Asim.Core.Exceptions;

namespace PWC.Asim.Core.Utils
{
    public class Simulator
    {
        public bool WaitForKeyPress { get; set; }

        private ILogger _logger;

        public Simulator(ILogger logger = null)
        {
            _logger = logger;
            if (_logger == null)
                _logger = new NoLogging();
        }

        private class OutputOption
        {
            public string Filename;
            public string[] Vars;
            public uint Period;
        }

        private class InputOption
        {
            public string Filename;
            public bool Recycle;
        }

        private class ReportFile
        {
            public string Template;
            public string Output;
        }

        public ulong Iteration { get; private set; }
        private Timer _timer;
        private List<InputOption> _inputActors = new List<InputOption>();
        private List<OutputOption> _outputActors = new List<OutputOption>();
        private List<ReportFile> _reportFiles = new List<ReportFile>();
        private StreamWriter _watchWriter;
        private readonly Dictionary<string, string> _controllers = new Dictionary<string, string>();
        private readonly SharedContainer _sharedVars = SharedContainer.Instance;

        private object _solarController;

        #region Options

        public ulong Iterations { get; set; }
        public DateTime? StartTime { get; set; }
        public string Watchfile { get; set; }
        public string[] Watchvars { get; set; }
        public bool GuessGeneratorState { get; set; }

        public Dictionary<string, string> Controllers
        {
            get { return _controllers; }
        }

        public void AddInput(string filename, bool recycle = false)
        {
            _inputActors.Add(new InputOption() { Filename = filename, Recycle = recycle });
        }

        public void AddOutput(string filename, string[] variables = null, uint period = 1)
        {
            _outputActors.Add(new OutputOption() { Filename = filename, Vars = variables, Period = period });
        }

        public void AddReport(string template, string output)
        {
            _reportFiles.Add(new ReportFile() {Template = template, Output = output});
        }

        #endregion Options

        public void Simulate()
        {
            var actors = new List<IActor>();

            _inputActors.ForEach(s => actors.Add(new NextData(s.Filename, StartTime, s.Recycle)));
            // add extra simulation actors here.  Order is important:
            actors.Add(new Load());
            actors.Add(new Station());
            actors.Add(new SheddableLoadMgr());
            actors.Add(new GenMgr(GuessGeneratorState ? GenMgrType.Calculate : GenMgrType.Simulate));
            actors.Add(new Solar(LoadSolarDelegate()));
            _outputActors.ForEach(o => actors.Add(new OutputData(o.Filename, o.Vars, o.Period, StartTime, DateFormat.Other)));

            _inputActors = null;
            _outputActors = null;

            Console.WriteLine("Init...");
            actors.ForEach(a => a.Init());
            Console.WriteLine("Run " + Iterations + " iterations...");

            SetWatchActions();

            _timer = new Timer(5000);
            _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            _timer.Enabled = true;
            TimeSpan innerLoopTime;
            try
            {
                var start = DateTime.Now;
                for (ulong i = 0; i < Iterations; i++)
                {
                    Iteration = i;
                    actors.ForEach(a => a.Run(i));
                }
                var end = DateTime.Now;
                innerLoopTime = end - start;
                Console.WriteLine("100%");
                actors.ForEach(a => a.Finish());
                Console.WriteLine("inner loop took {0}s", innerLoopTime.TotalSeconds);
            }
            catch(SimulationException e)
            {
                throw;
            }
            catch(Exception e)
            {
                throw new SimulationException("Error in simulation iteration: " + Iteration, e);
            }
            finally
            {
                _timer.Enabled = false;
                FinishWatchActions();
            }

            WriteReports(innerLoopTime);
        }

        private void WriteReports(TimeSpan time)
        {
            var tokens = new Dictionary<string, string>() { { "ASIM_ELAPSEDSECONDS", time.TotalSeconds.ToString() } };
            const string tokenMatch = "%(.*?)%";
            foreach (var report in _reportFiles)
            {
                try
                {
                    Console.WriteLine("Reading template {0}", report.Template);
                    var template = File.ReadAllText(report.Template);
                    MatchCollection matches = Regex.Matches(template, tokenMatch);
                    foreach (Match match in matches)
                    {
                        var tok = template.Substring(match.Index + 1, match.Length - 2);
                        if (!tokens.ContainsKey(tok))
                        {
                            // first try from shared vars
                            var shareVar = _sharedVars.GetOrDefault(tok);
                            if (shareVar != null)
                            {
                                tokens[tok] = shareVar.Val.ToString();
                            }
                            else
                            {
                                // then try environment
                                var env = Environment.GetEnvironmentVariable(tok);
                                if (env != null)
                                {
                                    tokens[tok] = env;
                                }
                                else
                                {
                                    // finally, put the token back in
                                    tokens[tok] = "%" + tok + "%";
                                }
                            }
                        }
                    }
                    var output = Regex.Replace(template, tokenMatch, m => tokens[m.Groups[1].Value]);
                    File.WriteAllText(report.Output, output);
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error ocurred when creating output {0}", report.Output);
                }
            }
        }

        private Delegate LoadSolarDelegate()
        {
            _logger.WriteLine("Load Solar Delegate");
            string dll;
            Delegate toReturn;
            if (Controllers.TryGetValue("SolarController", out dll))
            {
                _logger.WriteLine("Loading delegate from '" + dll + "'.");
                _logger.WriteLine("current directory is '" + Directory.GetCurrentDirectory() + "'.");
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(dll);
                    _logger.WriteLine("DLL loaded.");
                }
                catch (Exception e)
                {
                    throw new SimulationException("Could not load the DLL '" + dll + "': " + e.Message);
                }
                try
                {
                    Type controllerType = assembly.GetTypes().First(t => t.IsClass && t.Name.Equals("SolarController"));
                    _solarController = Activator.CreateInstance(controllerType);
                    MethodInfo handler = controllerType.GetMethod("Control", BindingFlags.Public | BindingFlags.Instance);
                    toReturn = Delegate.CreateDelegate(typeof(Delegates.SolarController), _solarController, handler);
                    _logger.WriteLine("Solar control method instantiated.");
                }
                catch (Exception e)
                {
                    const string error =
                    "The solar controller could not load the specified control method. Please\n" +
                        "ensure your DLL has a class called 'SolarController' with an instance method\n" +
                        "'Control' with the following signature:\n" +
                        "double Control (double pvAvailP, double lastSetP, double genP, double genSpinP,\n" +
                        "         double genIdealP, double loadP, double statSpinSetP, double switchDownP);";
                    throw new SimulationException(error, e);
                }
            }
            else
            {
                // default solar controller built into Solar class
                Delegates.SolarController d = Solar.DefaultSolarController;
                toReturn = d;
            }
            return toReturn;
        }

        private void SetWatchActions()
        {
            if (string.IsNullOrEmpty(Watchfile) || !Watchvars.Any())
                return;

            _watchWriter = new StreamWriter(Watchfile);
            var varlist = _sharedVars.MatchGlobs(Watchvars);
            foreach (var varname in varlist)
            {
                var sv = _sharedVars.GetOrDefault(varname);
                if (sv == null) continue;
                sv.OnValueChanged += (s, e) => _watchWriter.WriteLine(
                    (StartTime.HasValue ? StartTime.Value : Settings.Epoch).AddSeconds(Iteration).ToString("yyyy-MM-dd HH:mm:ss")
                    + "\t" + sv.Name + "\t" + e.OldValue + " -> " + e.NewValue
                    );
            }
        }

        private void FinishWatchActions()
        {
            if (_watchWriter != null)
            {
                _watchWriter.Flush();
                _watchWriter.Close();
            }
        }

        private void PrintState(IDictionary<string, double> varPool)
        {
            foreach (var p in varPool)
            {
                Console.WriteLine("{0}: {1}", p.Key, p.Value);
            }
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Console.WriteLine(string.Format("{0:P0}", (float)Iteration / (float)Iterations));
        }
    }
}
