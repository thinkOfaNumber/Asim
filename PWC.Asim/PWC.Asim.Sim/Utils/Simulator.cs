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
using System.Timers;
using PWC.Asim.Sim.Actors;
using PWC.Asim.Sim.Contracts;
using PWC.Asim.Sim.Exceptions;

namespace PWC.Asim.Sim.Utils
{
    class Simulator
    {
        public bool WaitForKeyPress { get; set; }

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

        public ulong Iteration { get; private set; }
        private Timer _timer;
        private List<InputOption> _inputActors = new List<InputOption>();
        private List<OutputOption> _outputActors = new List<OutputOption>();
        private StreamWriter _watchWriter;
        private readonly Dictionary<string, string> _controllers = new Dictionary<string, string>();

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

        #endregion Options

        public void Simulate()
        {
            var actors = new List<IActor>();

            _inputActors.ForEach(s => actors.Add(new NextData(s.Filename, StartTime, s.Recycle)));
            // add extra simulation actors here.  Order is important:
            actors.Add(new Load());
            actors.Add(new Station());
            actors.Add(new DispatchMgr());
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
            try
            {
                var start = DateTime.Now;
                for (ulong i = 0; i < Iterations; i++)
                {
                    Iteration = i;
                    actors.ForEach(a => a.Run(i));
                }
                var end = DateTime.Now;
                Console.WriteLine("100%");
                actors.ForEach(a => a.Finish());
                Console.WriteLine(string.Format("inner loop took {0}s", (end - start).TotalSeconds));
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
        }

        private Delegate LoadSolarDelegate()
        {
            string dll;
            Delegate toReturn;
            if (Controllers.TryGetValue("SolarController", out dll))
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(dll);
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
                }
                catch (Exception e)
                {
                    const string error =
                    "The solar controller could not load the specified control method. Please\n" +
                        "ensure your DLL has a class called 'SolarController' with an instance method\n" +
                        "'Control' with the following signature:\n" +
                        "double Control (double lastSetP, double genP, double genIdealP, double loadP);";
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
            var varlist = SharedContainer.MatchGlobs(Watchvars);
            foreach (var varname in varlist)
            {
                var sv = SharedContainer.GetOrDefault(varname);
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
