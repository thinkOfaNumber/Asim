// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of the Solar Load Model - A Renewable Energy Power Station
// Control System Simulator
//
// The Solar Load Model is free software: you can redistribute it and/or modify
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
using System.Timers;
using SolarLoadModel.Actors;
using SolarLoadModel.Contracts;
using SolarLoadModel.Exceptions;

namespace SolarLoadModel.Utils
{
    class Simulator
    {
        public bool WaitForKeyPress { get; set; }
        public ulong Iterations { get; set; }

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
        public DateTime? StartTime { get; set; }
        private Timer _timer;
        private List<InputOption> _inputActors = new List<InputOption>();
        private List<OutputOption> _outputActors = new List<OutputOption>();
        public string Watchfile { get; set; }
        public string[] Watchvars { get; set; }
        private StreamWriter _watchWriter;

        public void AddInput(string filename, bool recycle = false)
        {
            _inputActors.Add(new InputOption() { Filename = filename, Recycle = recycle });
        }

        public void AddOutput(string filename, string[] variables = null, uint period = 1)
        {
            _outputActors.Add(new OutputOption() { Filename = filename, Vars = variables, Period = period });
        }

        public void Simulate()
        {
            var actors = new List<IActor>();

            _inputActors.ForEach(s => actors.Add(new NextData(s.Filename, StartTime, s.Recycle)));
            actors.Add(new ScaleValues());
            // add extra simulation actors here.  Order is important:
            actors.Add(new Station());
            actors.Add(new DispatchMgr());
            actors.Add(new GenMgr());
            actors.Add(new Solar());
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
                sv.SetFunction = (oldval, newval) => _watchWriter.WriteLine(
                    (StartTime.HasValue ? StartTime.Value : Settings.Epoch).AddSeconds(Iteration).ToString("yyyy-MM-dd HH:mm:ss")
                    + "\t" + sv.Name + "\t" + oldval + " -> " + newval
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
