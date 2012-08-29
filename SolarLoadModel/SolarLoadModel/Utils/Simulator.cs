using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using SolarLoadModel.Actors;
using SolarLoadModel.Contracts;

namespace SolarLoadModel.Utils
{
    class Simulator
    {
        public bool WaitForKeyPress { get; set; }
        public ulong Iterations { get; set; }
        public string Path = "";

        private class OutputOption
        {
            public string Filename;
            public string[] Vars;
            public uint Period;
        }

        private ulong _iteration;
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
            var varPool = new Dictionary<string, double>();

            _inputActors.ForEach(s => actors.Add(new NextData(Path+s)));
            // add extra simulation actors here.  Order is important:
            actors.Add(new GenMgr());
            _outputActors.ForEach(o => actors.Add(new OutputData(Path+o.Filename, o.Vars, o.Period)));

            _inputActors = null;
            _outputActors = null;
            
            Console.WriteLine("Init...");
            actors.ForEach(a => a.Init(varPool));
            Console.WriteLine("Run " + Iterations + " iterations...");

            _timer = new System.Timers.Timer(5000);
            _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            _timer.Enabled = true;

            //
            //
            //
            var start = DateTime.Now;
            for (ulong i = 0; i < Iterations; i++)
            {
                _iteration = i;
                actors.ForEach(a => a.Run(varPool, i));
            }
            var end = DateTime.Now;
            Console.WriteLine("100%");
            actors.ForEach(a => a.Finish());
            Console.WriteLine(string.Format("inner loop took {0}s", (end - start).TotalSeconds));
            //
            //
            //

            //PrintState(varPool);
            //WaitForKeypress("Press any key to continue...");
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
            Console.WriteLine(string.Format("{0:P0}... ", (float)_iteration / (float)Iterations));
        }
    }
}
