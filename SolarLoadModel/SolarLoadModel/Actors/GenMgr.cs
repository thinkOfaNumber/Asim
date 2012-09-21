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
        public SharedValue MaxP { get; set; }

        public SharedValue P { get; set; }
        public SharedValue MinRunTPa { get; set; }
        public SharedValue LoadFact { get; set; }
        public SharedValue RunCnt { get; private set; }
        public SharedValue ECnt { get; private set; }
        public SharedValue FuelCons { get; set; }
        public SharedValue FuelCnt { get; private set; }
        public static SharedValue OnlineCfg { get; private set; }

        private const double PerHourToSec = 1 / (60.0 * 60.0);
        private double _fuelConsKws;
        private static readonly ExecutionManager ExecutionManager = new ExecutionManager();
        private bool _busy;
        private static ulong _iteration;
        private int _id;
        private readonly ushort _idBit;

        // counters
        public SharedValue StartCnt { get; private set; }
        public SharedValue StopCnt { get; private set; }
        public GeneratorState State { get; private set; }

        public Generator(int id)
        {
            _id = id;
            _idBit = (ushort)(1 << id);
            State = GeneratorState.Stopped;
            P = new SharedValue();
            LoadFact = new SharedValue();
            RunCnt = new SharedValue();
            ECnt = new SharedValue();
            OnlineCfg = new SharedValue();
            StartCnt = new SharedValue();
            StopCnt = new SharedValue();
            FuelCnt = new SharedValue();
        }

        public void Start()
        {
            if (_busy)
                return;

            if (State == GeneratorState.Stopped)
            {
                ExecutionManager.After(60, TransitionToOnline);
                StartCnt.Val++;
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
                StopCnt.Val++;
                State = GeneratorState.RunningOpen;
                OnlineCfg.Val = (ushort)OnlineCfg.Val & (ushort)~_idBit;
            }
        }

        public void Tick()
        {
            if (State == GeneratorState.RunningOpen)
            {
                RunCnt.Val++;
            }
            else if (State == GeneratorState.RunningClosed)
            {
                RunCnt.Val++;
                ECnt.Val += PerHourToSec;
                FuelCnt.Val += (_fuelConsKws* P.Val);
                LoadFact.Val = P.Val / MaxP.Val;
                _fuelConsKws = FuelCons.Val * PerHourToSec;
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
            _busy = false;
            OnlineCfg.Val = (ushort)OnlineCfg.Val | _idBit;
        }

        private void TransitionToStop()
        {
            State = GeneratorState.Stopped;
            _busy = false;
        }
    }

    struct Configuration
    {
        public SharedValue GenReg;
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
        private SharedValue _currCfg = new SharedValue();
        private readonly SharedValue _genMinRunT = new SharedValue();
        private SharedValue _genP;
        private readonly SharedValue _genOverload = new SharedValue();
        private SharedValue _genCfgSetP;
        private SharedValue _statHystP;
        private SharedValue _statSpinP;
        private SharedValue _genAvailCfg;
        private SharedValue _genBlackCfg;
        private SharedValue _genMinRunTPa;
        private ulong _iteration;
        //private readonly GenStrings[] _varStr = new GenStrings[MaxGens];
        private readonly Configuration[] _configurations = new Configuration[MaxCfg];
        private readonly string[] _configStrings = new string[MaxCfg];
        // temp variables for playing
        private double _d;

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
                genP += Gen[i].P.Val;
                overload = overload || (Gen[i].LoadFact.Val > 1);
            }

            //
            // Set Outputs
            //
            _genP.Val = genP;
            _genOverload.Val = Convert.ToDouble(overload);
        }

        public void Init(Dictionary<string, SharedValue> varPool)
        {
            for (int i = 0; i < MaxGens; i++)
            {
                Gen[i] = new Generator(i);
                int n = i + 1;

                // create variables in varPool for variables we write to
                varPool["Gen" + n + "P"] = Gen[i].P;
                varPool["Gen" + n + "StartCnt"] = Gen[i].StartCnt;
                varPool["Gen" + n + "StopCnt"] = Gen[i].StopCnt;
                varPool["Gen" + n + "LoadFact"] = Gen[i].LoadFact;
                varPool["Gen" + n + "RunCnt"] = Gen[i].RunCnt;
                varPool["Gen" + n + "ECnt"] = Gen[i].ECnt;
                varPool["Gen" + n + "FuelCnt"] = Gen[i].FuelCnt;

                // get variables we read from
                Gen[i].FuelCons = TestExistance(varPool, "Gen" + n + "FuelCons");
                Gen[i].MaxP = TestExistance(varPool, "Gen" + n + "MaxP");
                Gen[i].MinRunTPa = TestExistance(varPool, "Gen" + n + "MinRunTPa");
            }
            // create variables in varPool for variables we write to
            varPool["GenOverload"] = _genOverload;
            varPool["GenMinRunT"] = _genMinRunT;
            varPool["GenOnlineCfg"] = Generator.OnlineCfg;
            varPool["GenSetCfg"] = _currCfg;

            // test existance of variables we read from
            _statHystP = TestExistance(varPool, "StatHystP");
            _statSpinP = TestExistance(varPool, "StatSpinP");
            _genAvailCfg = TestExistance(varPool, "GenAvailCfg");
            _genBlackCfg = TestExistance(varPool, "GenBlackCfg");
            _genMinRunTPa = TestExistance(varPool, "GenMinRunTPa");
            _genCfgSetP = TestExistance(varPool, "GenCfgSetP");
            _genP = TestExistance(varPool, "GenP");

            for (int i = 0; i < MaxCfg; i ++)
            {
                string cstr = "GenConfig" + (i+1);
                if (!varPool.TryGetValue(cstr, out _configurations[i].GenReg))
                {
                    _configurations[i].GenReg = new SharedValue {Val = 0};
                    varPool[cstr] = _configurations[i].GenReg;
                }
            }
        }

        public void Finish()
        {
            
        }

        #endregion

        private SharedValue TestExistance(Dictionary<string, SharedValue> varPool, string s)
        {
            SharedValue v;
            if (!varPool.TryGetValue(s, out v))
            {
                throw new SimulationException("GenMgr simulator expected the variable '" + s + "' would exist by now.");
            }
            return v;
        }

        private void GeneratorManager()
        {
            if (_genMinRunT.Val > 0 && ((ushort)_currCfg.Val & (ushort)Generator.OnlineCfg.Val) == (ushort)_currCfg.Val)
            {
                _genMinRunT.Val--;
            }
            // black start
            ushort newCfg = (ushort)(Generator.OnlineCfg.Val == 0 ? _genBlackCfg.Val : SelectGens());

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
                    minRunTime = Math.Max(minRunTime, (ulong)Gen[i].MinRunTPa.Val);
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
                    power += Gen[i].MaxP.Val;
                }
            }
            return power;
        }

        private void StartStopGens(ushort cfg)
        {
            bool canStop = (cfg & (ushort)Generator.OnlineCfg.Val) == cfg;
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
                    onlineCap += Gen[i].MaxP.Val;
            }

            for (ushort i = 0; i < MaxGens; i++)
            {
                if (Gen[i].State == GeneratorState.RunningClosed)
                {
                    Gen[i].P.Val = Gen[i].MaxP.Val / onlineCap * _genCfgSetP.Val;
                }
                else
                {
                    Gen[i].P.Val = 0;
                }
            }
        }
    }
}
