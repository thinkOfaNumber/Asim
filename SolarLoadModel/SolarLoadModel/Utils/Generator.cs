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
        }
        public double P
        {
            get { return _p.Val; }
            set { _p.Val = value; }
        }
        public ulong MinRunTPa
        {
            get { return (ulong)_minRunTPa.Val; }
        }
        public double IdealPctP
        {
            get { return _idealPctP.Val; }
        }
        public double IdealP
        {
            get { return _idealP.Val; }
            private set { _idealP.Val = value; }
        }
        public double LoadFact
        {
            get { return _loadFact.Val; }
            private set { _loadFact.Val = value; }
        }
        public static ushort OnlineCfg
        {
            get { return (ushort)_onlineCfg.Val; }
            private set { _onlineCfg.Val = value; }
        }
        public static double GenIdealP
        {
            get { return _genIdealP.Val; }
            private set { _genIdealP.Val = value; }
        }
        public static double GenP
        {
            get { return _genP.Val; }
            private set { _genP.Val = value; }
        }
        private static bool Overload
        {
            get { return _genOverload.Val != 0.0f; }
            set { _genOverload.Val = Convert.ToDouble(value); }
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
        public double RunCnt
        {
            get { return _runCnt.Val; }
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
        private readonly Shared _idealPctP;
        private readonly Shared _idealP;
        private readonly Shared _loadFact;
        private readonly Shared[] _fuelCurveP = new Shared[Settings.FuelCurvePoints];
        private readonly Shared[] _fuelCurveL = new Shared[Settings.FuelCurvePoints];
        private static readonly Shared _onlineCfg = SharedContainer.GetOrNew("GenOnlineCfg");
        private static readonly Shared _genIdealP = SharedContainer.GetOrNew("GenIdealP");
        private static readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private static readonly Shared _genOverload = SharedContainer.GetOrNew("GenOverload");
        private static readonly Shared _genSpinP = SharedContainer.GetOrNew("GenSpinP");
        // counters
        private readonly Shared _startCnt;
        private readonly Shared _stopCnt;
        private readonly Shared _fuelCnt;
        private readonly Shared _runCnt;
        private readonly Shared _eCnt;

        public GeneratorState State { get; private set; }

        private double _fuelConsKws;
        private static readonly ExecutionManager ExecutionManager = new ExecutionManager();
        private bool _busy;
        private static ulong _iteration;
        private int _id;
        private readonly ushort _idBit;
        private double _spinP;

        private static readonly Generator[] Gen = new Generator[Settings.MAX_GENS];

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
            _maxP = SharedContainer.GetOrNew("Gen" + n + "MaxP");
            _minRunTPa = SharedContainer.GetOrNew("Gen" + n + "MinRunTPa");
            _idealPctP = SharedContainer.GetOrNew("Gen" + n + "IdealPctP");
            _idealP = SharedContainer.GetOrNew("Gen" + n + "IdealP");
            for (int i = 0; i < Settings.FuelCurvePoints; i++)
            {
                // Gen1Cons1P, Gen1Cons1L; Gen1Cons2P, Gen1Cons2L; ...
                _fuelCurveP[i] = SharedContainer.GetOrNew("Gen" + n + "FuelCons" + (i + 1) + "P");
                _fuelCurveL[i] = SharedContainer.GetOrNew("Gen" + n + "FuelCons" + (i + 1) + "L");
            }
            Gen[id] = this;
        }

        static Generator()
        {
            _onlineCfg.Val = 0;
            _genIdealP.Val = 0;
            _genP.Val = 0;
            _genOverload.Val = 0;
        }

        public static void RunAll()
        {
            GenIdealP = 0;
            GenP = 0;
            Overload = false;
            _genSpinP.Val = 0;
            for (int i = 0; i < Settings.MAX_GENS; i ++)
            {
                Gen[i].Run();
                GenIdealP += Gen[i].IdealP;
                GenP += Gen[i].P;
                Overload = Overload || (Gen[i].LoadFact > 1);
                _genSpinP.Val += Gen[i]._spinP;
            }
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

        private void Run()
        {
            LoadFact = 0;
            IdealP = 0;
            _spinP = 0;
            if (State == GeneratorState.RunningOpen)
            {
                RunCnt += Settings.PerHourToSec;
            }
            else if (State == GeneratorState.RunningClosed)
            {
                RunCnt += Settings.PerHourToSec;
                ECnt += P * Settings.PerHourToSec;
                LoadFact = P / MaxP;
                FuelCnt += FuelConsumptionSecond();
                IdealP = MaxP * IdealPctP / 100;
                _spinP = MaxP - P;
            }
        }

        /// <summary>
        /// Calculates fuel consumption for one second of operation at the current load factor.
        /// </summary>
        /// <returns>Fuel used this second, in L</returns>
        private double FuelConsumptionSecond()
        {
            // y = mx + b
            double m = 0;
            double b = double.NaN;
            for (int i = 1; i < Settings.FuelCurvePoints - 1; i++)
            {
                bool aboveMinPercent =
                    // ignore points that are unset (0,0)
                    (_fuelCurveP[i - 1].Val == 0 && _fuelCurveL[i - 1].Val == 0) ||
                    // load is within this range
                    LoadFact >= _fuelCurveP[i].Val;

                bool belowMaxPercent = i == Settings.FuelCurvePoints - 2 ||
                    // ignore points that are unset (0,0)
                    (_fuelCurveP[i + 1].Val == 0 && _fuelCurveL[i + 1].Val == 0) ||
                    // load is within this range
                    LoadFact < _fuelCurveP[i + 1].Val;

                if (aboveMinPercent && belowMaxPercent)
                {
                    m = (_fuelCurveL[i + 1].Val - _fuelCurveL[i].Val) / (_fuelCurveP[i + 1].Val - _fuelCurveP[i].Val);
                    b = _fuelCurveL[i].Val - m * _fuelCurveP[i].Val;
                    break;
                }
            }

            return (m * LoadFact + b) * P * Settings.PerHourToSec;
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
