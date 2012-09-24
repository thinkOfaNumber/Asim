using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SolarLoadModel.Exceptions;
using SolarLoadModel.Utils;
using SolarLoadModel.Contracts;

namespace SolarLoadModel.Actors
{
    enum GeneratorState
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
    class Generator
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

        public void Overload()
        {
            if (_busy)
                return;
            TransitionToStop();
        }

        public void Tick()
        {
            if (State == GeneratorState.RunningOpen)
            {
                RunCnt++;
            }
            else if (State == GeneratorState.RunningClosed)
            {
                RunCnt++;
                ECnt += PerHourToSec;
                FuelCnt += (_fuelConsKws* P);
                LoadFact = P / MaxP;
                _fuelConsKws = FuelCons * PerHourToSec;
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

    struct Configuration
    {
        public Shared GenReg;
        public double Pmax;
    }

    struct GenStrings
    {
        public string MaxP;
        public string P;
        public string StartCnt;
        public string StopCnt;
        public string MinRunTPa;
        public string LoadFact;
        public string RunCnt;
        public string ECnt;
        public string FuelCnt;
        public string FuelCons;
    }

    public class GenMgr : IActor
    {
        private const ushort MaxGens = 8;
        private const ushort MaxCfg = 1 << MaxGens;

        private static readonly Generator[] Gen = new Generator[MaxGens];

        private readonly Shared _currCfg = SharedContainer.GetOrNew("GenSetCfg");
        private readonly Shared _genMinRunT = SharedContainer.GetOrNew("GenMinRunT");
        private readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private readonly Shared _genOverload = SharedContainer.GetOrNew("GenOverload");
        private readonly Shared _genCfgSetP = SharedContainer.GetOrNew("GenCfgSetP");
        private readonly Shared _statHystP = SharedContainer.GetOrNew("StatHystP");
        private readonly Shared _statSpinP = SharedContainer.GetOrNew("StatSpinP");
        private readonly Shared _genAvailCfg = SharedContainer.GetOrNew("GenAvailCfg");
        private readonly Shared _genBlackCfg = SharedContainer.GetOrNew("GenBlackCfg");
        private readonly Shared _genMinRunTPa = SharedContainer.GetOrNew("GenMinRunTPa");

        private ulong _iteration;
        private readonly Configuration[] _configurations = new Configuration[MaxCfg];

        //private readonly ExecutionManager _executionManager = new ExecutionManager();

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            _iteration = iteration;

            //
            // Simulate
            //
            Generator.UpdateStates(iteration);
            GeneratorManager();
            bool overload = false;
            double genP = 0;
            for (int i = 0; i < MaxGens; i++)
            {
                Gen[i].Tick();
                genP += Gen[i].P;
                overload = overload || (Gen[i].LoadFact > 1);
            }

            //
            // Set Outputs
            //
            _genP.Val = genP;
            _genOverload.Val = Convert.ToDouble(overload);
        }

        public void Init()
        {
            for (int i = 0; i < MaxGens; i++)
            {
                Gen[i] = new Generator(i);
                int n = i + 1;
            }

            // test existance of variables we read from
            for (int i = 0; i < MaxCfg; i ++)
            {
                string cstr = "GenConfig" + (i+1);
                _configurations[i].GenReg = SharedContainer.GetOrNew(cstr);
                _configurations[i].GenReg.Val = 0;
            }
        }

        public void Finish()
        {
            
        }

        #endregion

        private Shared TestExistance(Dictionary<string, Shared> varPool, string s)
        {
            Shared v;
            if (!varPool.TryGetValue(s, out v))
            {
                throw new SimulationException("GenMgr simulator expected the variable '" + s + "' would exist by now.");
            }
            return v;
        }

        private void GeneratorManager()
        {
            if (_genMinRunT.Val > 0 && ((ushort)_currCfg.Val & Generator.OnlineCfg) == (ushort)_currCfg.Val)
            {
                _genMinRunT.Val--;
            }
            // black start
            ushort newCfg = (ushort)(Generator.OnlineCfg == 0 ? _genBlackCfg.Val : SelectGens());

            if (newCfg != _currCfg.Val)
            {
                // prime the minimum run timer on config changes
                _genMinRunT.Val = MinimumRunTime(newCfg);
            }
            _currCfg.Val = newCfg;

            StartStopGens((ushort)_currCfg.Val);
            LoadShare();
        }

        private ushort SelectGens()
        {
            int found = -1;
            ushort bestCfg = 0;
            double currCfgPower = TotalPower((ushort)_currCfg.Val);

            for (int i = 0; i < MaxCfg; i++)
            {
                _configurations[i].Pmax = TotalPower((ushort)((ushort)_configurations[i].GenReg.Val & (ushort)_genAvailCfg.Val));
                if (_configurations[i].Pmax >= _genCfgSetP.Val + _statSpinP.Val)
                {
                    found = i;
                    break;
                }
            }
            // if nothing was found, switch on everything as a fallback
            if (found == -1)
                bestCfg = (ushort)_genAvailCfg.Val;
            // if no change is required, stay at current config
            else if (_configurations[found].GenReg == _currCfg)
                bestCfg = (ushort)_currCfg.Val;
            // switch to a higher configuration without waiting
            else if (_configurations[found].Pmax > currCfgPower)
                bestCfg = (ushort)_configurations[found].GenReg.Val;
            // only switch to a lower configurations if min run timer is expired
            else if (_genMinRunT.Val > 0)
                bestCfg = (ushort)_currCfg.Val;
            // only switch to a lower configuration if it is below the hysteresis of the current configuration
            else if (_configurations[found].Pmax < (currCfgPower - _statHystP.Val))
                bestCfg = (ushort)_configurations[found].GenReg.Val;
            // don't switch to a smaller configuration as Hysteresis wasn't satisfied
            else
                bestCfg = (ushort)_currCfg.Val;

            return bestCfg;
        }

        public ulong MinimumRunTime(ushort cfg)
        {
            ulong minRunTime = 0;
            for (ushort i = 0; i < MaxGens; i++)
            {
                ushort genBit = (ushort)(1 << i);
                if ((genBit & cfg) == genBit)
                {
                    minRunTime = Math.Max(minRunTime, (ulong)Gen[i].MinRunTPa);
                }
            }
            return Math.Max(minRunTime, (ulong)_genMinRunTPa.Val);
        }

        private double TotalPower(ushort cfg)
        {
            double power = 0;
            for (ushort i = 0; i < MaxGens; i++)
            {
                ushort genBit = (ushort)(1 << i);
                if ((genBit & cfg) == genBit)
                {
                    power += Gen[i].MaxP;
                }
            }
            return power;
        }

        private void StartStopGens(ushort cfg)
        {
            bool canStop = (cfg & (ushort)Generator.OnlineCfg) == cfg;
            for (ushort i = 0; i < MaxGens; i++)
            {
                ushort genBit = (ushort)(1<<i);
                if ((genBit & cfg) == genBit && Gen[i].State != GeneratorState.RunningClosed)
                {
                    Gen[i].Start();
                }
                if (((genBit & cfg) == 0) && canStop && Gen[i].State != GeneratorState.Stopped)
                {
                    Gen[i].Stop();
                }
            }
        }

        private void LoadShare()
        {
            // simple load sharing:
            // - load is taken / dropped immediatly
            // - load factor is evened across all online sets
            // - generators can be loaded infinitely

            // figure out actual online capacity
            double onlineCap = 0;
            for (int i = 0; i < MaxGens; i++)
            {
                if (Gen[i].State == GeneratorState.RunningClosed)
                    onlineCap += Gen[i].MaxP;
            }

            for (ushort i = 0; i < MaxGens; i++)
            {
                if (Gen[i].State == GeneratorState.RunningClosed)
                {
                    double setP = Gen[i].MaxP / onlineCap * _genCfgSetP.Val;
                    if (setP > Gen[i].MaxP)
                    {
                        Gen[i].Overload();
                    }
                    else
                    {
                        Gen[i].P = setP;
                    }
                }
                else
                {
                    Gen[i].P = 0;
                }
            }
        }
    }
}
