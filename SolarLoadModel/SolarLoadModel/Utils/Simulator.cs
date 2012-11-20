using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public ulong Iteration { get; private set; }
        public DateTime? StartTime { get; set; }
        private System.Timers.Timer _timer;
        private List<string> _inputActors = new List<string>();
        private List<OutputOption> _outputActors = new List<OutputOption>();

        public void AddInput(string filename)
        {
            _inputActors.Add(filename);
        }

        public void AddOutput(string filename, string[] variables = null, uint period = 1)
        {
            _outputActors.Add(new OutputOption() { Filename = filename, Vars = variables, Period = period });
        }

        public void Simulate()
        {
            var actors = new List<IActor>();

            _inputActors.ForEach(s => actors.Add(new NextData(s, StartTime)));
            actors.Add(new ScaleValues());
            // add extra simulation actors here.  Order is important:
            actors.Add(new Station());
            actors.Add(new GenMgr());
            actors.Add(new Solar());
            _outputActors.ForEach(o => actors.Add(new OutputData(o.Filename, o.Vars, o.Period, StartTime, DateFormat.Other)));

            _inputActors = null;
            _outputActors = null;
            
            Console.WriteLine("Init...");
            actors.ForEach(a => a.Init());
            Console.WriteLine("Run " + Iterations + " iterations...");

            _timer = new System.Timers.Timer(5000);
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
