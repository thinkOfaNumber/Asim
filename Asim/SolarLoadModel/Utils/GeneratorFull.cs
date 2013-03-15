using SolarLoadModel.Actors;

namespace SolarLoadModel.Utils
{
    public class GeneratorFull : GeneratorBase
    {
        private bool _busy;

        public GeneratorFull(int id)
            : base(id)
        {
        }

        protected override void Run()
        {
            if (Station.BlackStartInit && IsAvailable())
            {
                Reset();
            }
            base.Run();
        }

        /// <summary>
        /// Removes all pending start/stops.  Use with caution!
        /// </summary>
        protected override void Reset()
        {
            if (IsRunningOffline())
                StopCnt++;
            State = GeneratorState.Stopped;
            ExecutionManager.RemoveActions(TransitionToStop);
            ExecutionManager.RemoveActions(TransitionToOnline);
            OnlineCfg &= (ushort)~_idBit;
            _busy = false;
        }

        public override void Start()
        {
            if (_busy)
                return;

            if (IsStopped() && IsAvailable())
            {
                ExecutionManager.After(Settings.GenStartStopDelay, TransitionToOnline);
                _busy = true;
                State = GeneratorState.RunningOpen;
            }
        }

        public override void Stop()
        {
            if (_busy)
                return;

            if (IsOnline())
            {
                _busy = true;
                ExecutionManager.After(Settings.GenStartStopDelay, TransitionToStop);
                State = GeneratorState.RunningOpen;
                OnlineCfg &= (ushort)~_idBit;
            }
        }

        public override void CriticalStop()
        {
            if (_busy)
                return;
            TransitionToStop();
            OnlineCfg &= (ushort)~_idBit;
        }

        protected override void Service()
        {
            if (_busy)
                return;

            if (IsOnline())
            {
                _busy = true;
                ExecutionManager.After(Settings.GenStartStopDelay, PerformService);
                State = GeneratorState.RunningOpen | GeneratorState.Unavailable;
                OnlineCfg &= (ushort)~_idBit;
            }
            else if (IsStopped())
            {
                _busy = true;
                PerformService();
            }
        }


        private void PerformService()
        {
            State = GeneratorState.Stopped | GeneratorState.InService | GeneratorState.Unavailable;
            // service takes _serviceOutT hours
            ExecutionManager.After((ulong)(_serviceOutT.Val * Settings.SecondsInAnHour), FinishService);
        }

        private void FinishService()
        {
            StartCnt = 0;
            StopCnt = 0;
            _serviceCnt.Val++;
            RunCnt = 0;
            State = GeneratorState.Stopped;
            _busy = false;
        }

        private void TransitionToOnline()
        {
            State = GeneratorState.RunningClosed;
            StartCnt++;
            _busy = false;
            OnlineCfg |= _idBit;
        }

        private void TransitionToStop()
        {
            State = GeneratorState.Stopped;
            StopCnt++;
            _busy = false;
        }
    }
}
