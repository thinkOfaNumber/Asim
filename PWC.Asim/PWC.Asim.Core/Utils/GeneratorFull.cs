using PWC.Asim.Core.Actors;

namespace PWC.Asim.Core.Utils
{
    public class GeneratorFull : GeneratorBase
    {
        private bool _busy;
        private ulong _overloadCnt;
        private ulong _underloadCnt;
        private bool _trip;

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

            _trip = false;
            if (P > MaxP)
            {
                _overloadCnt++;
                _underloadCnt = 0;
            }
            if (P < 0)
            {
                _underloadCnt++;
                _overloadCnt = 0;
            }
            else
                _overloadCnt = _underloadCnt = 0;

            // overload timer
            if (_genOverloadPctP.Val > 0.0D // is overload trip time enabled?
                && _overloadCnt > _genOverloadT.Val)
                _trip = true;
            // max overload value - trip immediately
            if (P > (MaxP + MaxP * _genOverloadPctP.Val * Settings.Percent))
                _trip = true;

            // underload timer
            if (_genUnderloadPctP.Val > 0.0D // is underload trip time enabled?
                && _underloadCnt > _genUnderloadT.Val)
                _trip = true;
            // max underload value - trip immediately
            if (P < (-MaxP * _genUnderloadPctP.Val * Settings.Percent))
                _trip = true;
            
            if (_trip)
            {
                CriticalStop();
                Reset();
                P = 0;
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

        protected override void CriticalStop()
        {
            TransitionToStop();
            OnlineCfg &= (ushort)~_idBit;
            Reset();
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
            ulong serviceTime = 0;
            for (int i = 0; i < Settings.MaxSvcIntervals; i++)
            {
                if (_serviceCounters[i].InService)
                {
                    serviceTime += _serviceCounters[i].ServiceOutage;
                }
            }
            ExecutionManager.After((ulong)(serviceTime), FinishService);
        }

        private void FinishService()
        {
            StartCnt = 0;
            StopCnt = 0;
            _serviceCnt.Val++;
            State = GeneratorState.Stopped;
            for (int i = 0; i < Settings.MaxSvcIntervals; i++)
            {
                _serviceCounters[i].InService = false;
            }
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
