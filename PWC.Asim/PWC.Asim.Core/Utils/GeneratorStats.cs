using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PWC.Asim.Core.Utils
{
    public class GeneratorStats : GeneratorBase
    {
        private GeneratorState _lastState;

        public GeneratorStats(int id) : base(id)
        {
            _lastState = GeneratorState.Stopped;
        }

        protected override void Run()
        {
            if (P > 10)
                State = GeneratorState.RunningClosed;
            else if (P < 5)
                State = GeneratorState.Stopped;
            else
                State = _lastState;
            if (_lastState == GeneratorState.Stopped && State == GeneratorState.RunningClosed)
            {
                StartCnt++;
            }
            if (_lastState == GeneratorState.RunningClosed && State == GeneratorState.Stopped)
            {
                StopCnt++;
            }
            _lastState = State;
            base.Run();
        }

        public override void Start()
        {

        }

        public override void Stop()
        {

        }

        protected override void Reset()
        {

        }

        public override void CriticalStop()
        {

        }

        protected override void Service()
        {

        }
    }
}
