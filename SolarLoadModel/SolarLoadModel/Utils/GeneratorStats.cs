using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolarLoadModel.Utils
{
    public class GeneratorStats : GeneratorBase
    {
        private double _lastGenP;

        public GeneratorStats(int id) : base(id)
        {
            _lastGenP = 0;
        }

        protected override void Run()
        {
            if (_lastGenP == 0D && GenP > 0D)
            {
                StartCnt++;
                State = GeneratorState.RunningClosed;
            }
            else if (_lastGenP > 0D && GenP == 0D)
            {
                StopCnt++;
                State = GeneratorState.Stopped;
            }
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
