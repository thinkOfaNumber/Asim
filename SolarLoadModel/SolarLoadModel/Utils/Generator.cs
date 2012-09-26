using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolarLoadModel.Utils
{
    public enum GeneratorState
    {
        /// <summary>
        /// stopped, offline, open, etc.
        /// </summary>
        Stopped,
        /// <summary>
        /// Run counter incrememnted, but no load or fuel used
        /// </summary>
        RunningOpen,
        /// <summary>
        /// Run counter incrememted and loaded
        /// </summary>
        RunningClosed
    }

    /// <summary>
    /// Basic Generator class.
    /// </summary>
    /// <remarks>Locking is not required on state transitions becuase all commands
    /// happen at specific points in the iteration</remarks>
    public class Generator
    {
        public double MaxP
        {
            get { return _maxP.Val; }
            set { _maxP.Val = value; }
        }
        public double P
        {
            get { return _p.Val; }
            set { _p.Val = value; }
        }
        public ulong MinRunTPa
        {
            get { return (ulong)_minRunTPa.Val; }
            set { _minRunTPa.Val = value; }
        }
        public double LoadFact
        {
            get { return _loadFact.Val; }
            set { _loadFact.Val = value; }
        }
        public double FuelCons
        {
            get { return _fuelCons.Val; }
            set { _fuelCons.Val = value; }
        }
        public static ushort OnlineCfg
        {
            get { return (ushort)_onlineCfg.Val; }
            private set { _onlineCfg.Val = value; }
        }
        // counters
        public ulong StartCnt
        {
            get { return (ulong)_startCnt.Val; }
            private set { _startCnt.Val = value; }
        }
        public ulong StopCnt
        {
            get { return (ulong)_stopCnt.Val; }
            private set { _stopCnt.Val = value; }
        }
        public double FuelCnt
        {
            get { return _fuelCnt.Val; }
            private set { _fuelCnt.Val = value; }
        }
        public ulong RunCnt
        {
            get { return (ulong)_runCnt.Val; }
            private set { _runCnt.Val = value; }
        }
        public double ECnt
        {
            get { return _eCnt.Val; }
            private set { _eCnt.Val = value; }
        }

        private readonly Shared _maxP;
        private readonly Shared _p;
        private readonly Shared _minRunTPa;
        private readonly Shared _loadFact;
        private readonly Shared _fuelCons;
        private static readonly  Shared _onlineCfg;
        // counters
        private readonly Shared _startCnt;
        private readonly Shared _stopCnt;
        private readonly Shared _fuelCnt;
        private readonly Shared _runCnt;
        private readonly Shared _eCnt;

        public GeneratorState State { get; private set; }

        private const double PerHourToSec = 1 / (60.0 * 60.0);
        private double _fuelConsKws;
        private static readonly ExecutionManager ExecutionManager = new ExecutionManager();
        private bool _busy;
        private static ulong _iteration;
        private int _id;
        private readonly ushort _idBit;


        public Generator(int id)
        {
            _id = id;
            _idBit = (ushort)(1 << id);
            State = GeneratorState.Stopped;

            int n = id + 1;
            _p = SharedContainer.GetOrNew("Gen" + n + "P");
            _startCnt = SharedContainer.GetOrNew("Gen" + n + "StartCnt");
            _stopCnt = SharedContainer.GetOrNew("Gen" + n + "StopCnt");
            _loadFact = SharedContainer.GetOrNew("Gen" + n + "LoadFact");
            _runCnt = SharedContainer.GetOrNew("Gen" + n + "RunCnt");
            _eCnt = SharedContainer.GetOrNew("Gen" + n + "ECnt");
            _fuelCnt = SharedContainer.GetOrNew("Gen" + n + "FuelCnt");
            _fuelCons = SharedContainer.GetOrNew("Gen" + n + "FuelCons");
            _maxP = SharedContainer.GetOrNew("Gen" + n + "MaxP");
            _minRunTPa = SharedContainer.GetOrNew("Gen" + n + "MinRunTPa");

            // create variables in varPool for variables we write to
        }

        static Generator()
        {
            _onlineCfg = SharedContainer.GetOrNew("GenOnlineCfg");
            _onlineCfg.Val = 0;
        }

        public void Start()
        {
            if (_busy)
                return;

            if (State == GeneratorState.Stopped)
            {
                ExecutionManager.After(60, TransitionToOnline);
                _busy = true;
                State = GeneratorState.RunningOpen;
            }
        }

        public void Stop()
        {
            if (_busy)
                return;

            if (State == GeneratorState.RunningClosed)
            {
                _busy = true;
                ExecutionManager.After(60, TransitionToStop);
                State = GeneratorState.RunningOpen;
                OnlineCfg &= (ushort)~_idBit;
            }
        }

        public void CriticalStop()
        {
            if (_busy)
                return;
            TransitionToStop();
            OnlineCfg &= (ushort)~_idBit;
        }

        public void Tick()
        {
            if (State == GeneratorState.RunningOpen)
            {
                RunCnt++;
                LoadFact = 0;
            }
            else if (State == GeneratorState.RunningClosed)
            {
                RunCnt++;
                ECnt += PerHourToSec;
                FuelCnt += (_fuelConsKws * P);
                LoadFact = P / MaxP;
                _fuelConsKws = FuelCons * PerHourToSec;
            }
            else
            {
                LoadFact = 0;
            }
        }

        public static void UpdateStates(ulong iteration)
        {
            _iteration = iteration;
            ExecutionManager.RunActions(_iteration);
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
