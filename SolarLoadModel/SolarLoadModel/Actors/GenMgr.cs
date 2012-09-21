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
        public double MaxP { get; set; }
        public double P { get; set; }
        public UInt32 MinRunTPa { get; set; }
        public double LoadFact { get { return P / MaxP; } }
        public ulong RunCnt { get; private set; }
        public double ECnt { get; private set; }
        public double FuelCons { set { _fuelConsKws = value * PerHourToSec; } }
        public double FuelCnt { get; private set; }
        public static ushort OnlineCfg { get; private set; }

        private const double PerHourToSec = 1 / (60.0 * 60.0);
        private double _fuelConsKws;
        private static readonly ExecutionManager ExecutionManager = new ExecutionManager();
        private bool _busy;
        private static ulong _iteration;
        private int _id;
        private readonly ushort _idBit;

        // counters
        public int StartCnt { get; private set; }
        public int StopCnt { get; private set; }
        public GeneratorState State { get; private set; }

        public Generator(int id)
        {
            _id = id;
            _idBit = (ushort)(1 << id);
            State = GeneratorState.Stopped;
        }

        public void Start()
        {
            if (_busy)
                return;

            if (State == GeneratorState.Stopped)
            {
                ExecutionManager.After(60, TransitionToOnline);
                StartCnt++;
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
                StopCnt++;
                State = GeneratorState.RunningOpen;
                OnlineCfg &= (ushort)~_idBit;
            }
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
                FuelCnt += (_fuelConsKws * P);
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
            OnlineCfg |= _idBit;
        }

        private void TransitionToStop()
        {
            State = GeneratorState.Stopped;
            _busy = false;
        }
    }

    struct Configuration
    {
        public ushort GenReg;
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
        private ushort _currCfg;
        private double _genCfgSetP;
        private double _statHystP;
        private double _statSpinP;
        private ushort _genAvailCfg;
        private ushort _genBlackCfg;
        private ulong  _genMinRunT;
        private ulong _genMinRunTPa;
        private ulong _iteration;
        private readonly GenStrings[] _varStr = new GenStrings[MaxGens];
        private readonly Configuration[] _configurations = new Configuration[MaxCfg];
        private readonly string[] _configStrings = new string[MaxCfg];
        // temp variables for playing
        private double _d;

        //private readonly ExecutionManager _executionManager = new ExecutionManager();

        #region Implementation of IActor

        public void Run(Dictionary<string, double> varPool, ulong iteration)
        {
            _iteration = iteration;
            //
            // Read Inputs
            //
            for (int i = 0; i < MaxGens; i++)
            {
                Gen[i].MaxP = varPool[_varStr[i].MaxP];
                Gen[i].P = varPool[_varStr[i].P];
                Gen[i].FuelCons = varPool[_varStr[i].FuelCons];
            }
            for (int i = 0; i < MaxCfg; i ++)
            {
                if (varPool.TryGetValue(_configStrings[i], out _d))
                {
                    _configurations[i].GenReg = Convert.ToUInt16(_d);
                    //_configurations[i].Pmax = TotalPower(_configurations[i].GenReg);
                }
                else
                {
                    _configurations[i].GenReg = 0;
                }
                _configurations[i].Pmax = 0;
            }
            _genCfgSetP = varPool["GenCfgSetP"];
            _statHystP = varPool["StatHystP"];
            _statSpinP = varPool["StatSpinP"];
            _genAvailCfg = Convert.ToUInt16(varPool["GenAvailCfg"]);
            _genBlackCfg = Convert.ToUInt16(varPool["GenBlackCfg"]);
            _genMinRunTPa = Convert.ToUInt64(varPool["GenMinRunTPa"]);

            //
            // Simulate
            //
            Generator.UpdateStates(iteration);
            GeneratorManager();
            double genPact = 0;
            bool overload = false;
            for (int i = 0; i < MaxGens; i++)
            {
                Gen[i].Tick();
                genPact += Gen[i].P;
                overload = overload || (Gen[i].LoadFact > 1);
            }

            //
            // Set Outputs
            //
            for (int i = 0; i < MaxGens; i++)
            {
                varPool[_varStr[i].MaxP] = Gen[i].MaxP;
                varPool[_varStr[i].P] = Gen[i].P;
                varPool[_varStr[i].StartCnt] = Gen[i].StartCnt;
                varPool[_varStr[i].StopCnt] = Gen[i].StopCnt;
                varPool[_varStr[i].MinRunTPa] = Gen[i].MinRunTPa;
                varPool[_varStr[i].LoadFact] = Gen[i].LoadFact;
                varPool[_varStr[i].RunCnt] = Gen[i].RunCnt;
                varPool[_varStr[i].ECnt] = Gen[i].ECnt;
                // don't write FuelCons as it's only read
                varPool[_varStr[i].FuelCnt] = Gen[i].FuelCnt;
                varPool[_varStr[i].RunCnt] = Gen[i].RunCnt;
            }
            varPool["GenP"] = genPact;
            varPool["GenOverload"] = overload ? 1.0 : 0.0;
            varPool["GenMinRunT"] = _genMinRunT;
            varPool["GenOnlineCfg"] = Generator.OnlineCfg;
            varPool["GenSetCfg"] = _currCfg;
        }

        public void Init(Dictionary<string, double> varPool)
        {
            for (int i = 0; i < MaxGens; i++)
            {
                Gen[i] = new Generator(i);
                int genNo = i + 1;
                // create keys (faster than doing concats in the Run() method)
                _varStr[i].MaxP = "Gen" + genNo + "MaxP";
                _varStr[i].P = "Gen" + genNo + "P";
                _varStr[i].StartCnt = "Gen" + genNo + "StartCnt";
                _varStr[i].StopCnt = "Gen" + genNo + "StopCnt";
                _varStr[i].MinRunTPa = "Gen" + genNo + "MinRunTPa";
                _varStr[i].LoadFact = "Gen" + genNo + "LoadFact";
                _varStr[i].RunCnt = "Gen" + genNo + "RunCnt";
                _varStr[i].ECnt = "Gen" + genNo + "ECnt";
                _varStr[i].FuelCons = "Gen" + genNo + "FuelCons";
                _varStr[i].FuelCnt = "Gen" + genNo + "FuelCnt";

                // create variables in varPool for variables we write to
                varPool[_varStr[i].P] = 0;
                varPool[_varStr[i].StartCnt] = 0;
                varPool[_varStr[i].StopCnt] = 0;
                varPool[_varStr[i].LoadFact] = 0;
                varPool[_varStr[i].RunCnt] = 0;
                varPool[_varStr[i].ECnt] = 0;
                varPool[_varStr[i].FuelCnt] = 0;

                // test existance of variables we read from
                TestExistance(varPool, _varStr[i].FuelCons);
                TestExistance(varPool, _varStr[i].MaxP);
                TestExistance(varPool, _varStr[i].MinRunTPa);
            }
            // create variables in varPool for variables we write to
            varPool["GenP"] = 0;
            varPool["GenOverload"] = 0;
            varPool["GenMinRunT"] = 0;
            varPool["GenOnlineCfg"] = 0;
            varPool["GenSetCfg"] = 0;

            // test existance of variables we read from
            TestExistance(varPool, "StatHystP");
            TestExistance(varPool, "StatSpinP");
            TestExistance(varPool, "GenAvailCfg");
            TestExistance(varPool, "GenBlackCfg");
            TestExistance(varPool, "GenMinRunTPa");

            // build configuration var names
            for (int i = 0; i < MaxCfg; i ++)
            {
                _configStrings[i] = "GenConfig" + (i + 1);
            }
        }

        public void Finish()
        {
            
        }

        #endregion

        private void TestExistance(Dictionary<string, double> varPool, string s)
        {
            double dummy;
            if (!varPool.TryGetValue(s, out dummy))
            {
                throw new SimulationException("GenMgr simulator expected the variable '" + s + "' would exist by now.");
            }
        }

        private void GeneratorManager()
        {
            if (_genMinRunT > 0 && (_currCfg & Generator.OnlineCfg) == _currCfg)
            {
                _genMinRunT--;
            }
            // black start
            ushort newCfg = Generator.OnlineCfg == 0 ? _genBlackCfg : SelectGens();

            if (newCfg != _currCfg)
            {
                // prime the minimum run timer on config changes
                _genMinRunT = MinimumRunTime(newCfg);
            }
            _currCfg = newCfg;

            StartStopGens(_currCfg);
            LoadShare();
        }

        private ushort SelectGens()
        {
            int found = -1;
            ushort bestCfg = 0;
            double currCfgPower = TotalPower(_currCfg);

            for (int i = 0; i < MaxCfg; i++)
            {
                _configurations[i].Pmax = TotalPower((ushort)(_configurations[i].GenReg & _genAvailCfg));
                if (_configurations[i].Pmax >= _genCfgSetP + _statSpinP)
                {
                    found = i;
                    break;
                }
            }
            // if nothing was found, switch on everything as a fallback
            if (found == -1)
                bestCfg = _genAvailCfg;
            // if no change is required, stay at current config
            else if (_configurations[found].GenReg == _currCfg)
                bestCfg = _currCfg;
            // switch to a higher configuration without waiting
            else if (_configurations[found].Pmax > currCfgPower)
                bestCfg = _configurations[found].GenReg;
            // only switch to a lower configurations if min run timer is expired
            else if (_genMinRunT > 0)
                bestCfg = _currCfg;
            // only switch to a lower configuration if it is below the hysteresis of the current configuration
            else if (_configurations[found].Pmax < (currCfgPower - _statHystP))
                bestCfg = _configurations[found].GenReg;
            // don't switch to a smaller configuration as Hysteresis wasn't satisfied
            else
                bestCfg = _currCfg;

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
                    minRunTime = Math.Max(minRunTime, Gen[i].MinRunTPa);
                }
            }
            return Math.Max(minRunTime, _genMinRunTPa);
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
            bool canStop = (cfg & Generator.OnlineCfg) == cfg;
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
                    Gen[i].P = Gen[i].MaxP / onlineCap * _genCfgSetP;
                }
                else
                {
                    Gen[i].P = 0;
                }
            }
        }
    }
}
