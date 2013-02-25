using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolarLoadModel.Utils
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
            State = P > 0 ? GeneratorState.RunningClosed : GeneratorState.Stopped;
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

        public override void CriticalStop()
        {

        }

        protected override void Service()
        {

        }
    }
}
