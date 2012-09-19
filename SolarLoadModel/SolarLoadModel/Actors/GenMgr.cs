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
        public double Pmax { get; set; }
        public double Pact { get; set; }
        public UInt32 TminRun { get; set; }
        public double LoadFactor { get { return Pact / Pmax; } }
        public ulong RunTime { get; private set; }
        public double KwhTotal { get; private set; }
        public double FuelCons { set { _fuelConsKws = value * PerHourToSec; } }
        public double FuelUsed { get; private set; }
        public double MinRunTime { get; set; }
        public static ushort OnlineGens { get; private set; }

        private const double PerHourToSec = 1 / (60.0 * 60.0);
        private double _fuelConsKws;
        private static readonly ExecutionManager ExecutionManager = new ExecutionManager();
        private bool _busy;
        private static ulong _iteration;
        private int _id;
        private ushort _idBit;

        // counters
        public int Nstarts { get; private set; }
        public int Nstops { get; private set; }
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
                Nstarts++;
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
                Nstops++;
                State = GeneratorState.RunningOpen;
                OnlineGens &= (ushort)~_idBit;
            }
        }

        public void Tick()
        {
            if (State == GeneratorState.RunningOpen)
            {
                RunTime++;
            }
            else if (State == GeneratorState.RunningClosed)
            {
                RunTime++;
                KwhTotal += PerHourToSec;
                FuelUsed += (_fuelConsKws * Pact);
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
            OnlineGens |= _idBit;
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
        public string Pmax;
        public string Pact;
        public string Nstarts;
        public string Nstops;
        public string TminRun;
        public string LoadFactor;
        public string RunTime;
        public string KwhTotal;
        public string FuelCons;
        public string FuelUsed;
    }

    public class GenMgr : IActor
    {
        private const ushort MaxGens = 8;
        private const ushort MaxCfg = 1 << MaxGens;

        private static readonly Generator[] Gen = new Generator[MaxGens];
        private ushort _currCfg;
        private double StatPact;
        private double StatPHyst;
        private double StatPspin;
        private ushort GenAvailCfg;
        private ushort BlackCfg;
        private ulong  _minRunAct;
        private ulong TminRun;
        private ulong iteration;
        private readonly GenStrings[] _varStr = new GenStrings[MaxGens];
        private readonly Configuration[] _configurations = new Configuration[MaxCfg];
        private readonly string[] _configStrings = new string[MaxCfg];
        // temp variables for playing
        private double _d;

        //private readonly ExecutionManager _executionManager = new ExecutionManager();

        #region Implementation of IActor

        public void Run(Dictionary<string, double> varPool, ulong iteration)
        {
            this.iteration = iteration;
            //
            // Read Inputs
            //
            for (int i = 0; i < MaxGens; i++)
            {
                Gen[i].Pmax = varPool[_varStr[i].Pmax];
                Gen[i].Pact = varPool[_varStr[i].Pact];
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
            StatPact = varPool["StatPact"];
            StatPHyst = varPool["StatPHyst"];
            StatPspin = varPool["StatPspin"];
            GenAvailCfg = Convert.ToUInt16(varPool["GenAvailCfg"]);
            BlackCfg = Convert.ToUInt16(varPool["BlackCfg"]);
            TminRun = Convert.ToUInt64(varPool["TminRun"]);

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
                genPact += Gen[i].Pact;
                overload = overload || (Gen[i].LoadFactor > 1);
            }

            //
            // Set Outputs
            //
            for (int i = 0; i < MaxGens; i++)
            {
                varPool[_varStr[i].Pmax] = Gen[i].Pmax;
                varPool[_varStr[i].Pact] = Gen[i].Pact;
                varPool[_varStr[i].Nstarts] = Gen[i].Nstarts;
                varPool[_varStr[i].Nstops] = Gen[i].Nstops;
                varPool[_varStr[i].TminRun] = Gen[i].TminRun;
                varPool[_varStr[i].LoadFactor] = Gen[i].LoadFactor;
                varPool[_varStr[i].RunTime] = Gen[i].RunTime;
                varPool[_varStr[i].KwhTotal] = Gen[i].KwhTotal;
                // don't write FuelCons as it's only read
                varPool[_varStr[i].FuelUsed] = Gen[i].FuelUsed;
                varPool[_varStr[i].RunTime] = Gen[i].RunTime;
            }
            varPool["GenPact"] = genPact;
            varPool["GenOverload"] = overload ? 1.0 : 0.0;
            varPool["TminRunAct"] = _minRunAct;
            varPool["GenOnlineAct"] = Generator.OnlineGens;
            varPool["GenWantedAct"] = _currCfg;
        }

        public void Init(Dictionary<string, double> varPool)
        {
            for (int i = 0; i < MaxGens; i++)
            {
                Gen[i] = new Generator(i);
                int genNo = i + 1;
                // create keys (faster than doing concats in the Run() method)
                _varStr[i].Pmax = "Gen" + genNo + "Pmax";
                _varStr[i].Pact = "Gen" + genNo + "Pact";
                _varStr[i].Nstarts = "Gen" + genNo + "Nstarts";
                _varStr[i].Nstops = "Gen" + genNo + "Nstops";
                _varStr[i].TminRun = "Gen" + genNo + "TminRun";
                _varStr[i].LoadFactor = "Gen" + genNo + "LoadFactor";
                _varStr[i].RunTime = "Gen" + genNo + "RunTime";
                _varStr[i].KwhTotal = "Gen" + genNo + "KwhTotal";
                _varStr[i].FuelCons = "Gen" + genNo + "FuelCons";
                _varStr[i].FuelUsed = "Gen" + genNo + "FuelUsed";

                // create variables in varPool for variables we write to
                varPool[_varStr[i].Pmax] = 0;
                varPool[_varStr[i].Pact] = 0;
                varPool[_varStr[i].Nstarts] = 0;
                varPool[_varStr[i].Nstops] = 0;
                varPool[_varStr[i].TminRun] = 0;
                varPool[_varStr[i].LoadFactor] = 0;
                varPool[_varStr[i].RunTime] = 0;
                varPool[_varStr[i].KwhTotal] = 0;
                varPool[_varStr[i].FuelUsed] = 0;

                // test existance of variables we read from
                TestExistance(varPool, _varStr[i].FuelCons);
            }
            // create variables in varPool for variables we write to
            varPool["GenPact"] = 0;
            varPool["GenOverload"] = 0;
            varPool["TminRunAct"] = 0;
            varPool["GenOnlineAct"] = 0;
            varPool["GenWantedAct"] = 0;

            // test existance of variables we read from
            TestExistance(varPool, "StatPact");
            TestExistance(varPool, "StatPHyst");
            TestExistance(varPool, "StatPspin");
            TestExistance(varPool, "GenAvailCfg");
            TestExistance(varPool, "BlackCfg");
            TestExistance(varPool, "TminRun");

            // build configuration var names
            for (int i = 0; i < MaxCfg; i ++)
            {
                _configStrings[i] = "Config" + (i + 1);
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
            if (_minRunAct > 0 && (_currCfg & Generator.OnlineGens) == _currCfg)
            {
                _minRunAct--;
            }
            ushort newCfg;
            // black start
            if (Generator.OnlineGens == 0)
                newCfg = BlackCfg;
            else
                newCfg = SelectGens();

            if (newCfg != _currCfg)
            {
                // prime the minimum run timer on config changes
                _minRunAct = MinimumRunTime(newCfg);
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
                _configurations[i].Pmax = TotalPower((ushort)(_configurations[i].GenReg & GenAvailCfg));
                if (_configurations[i].Pmax >= StatPact + StatPspin)
                {
                    found = i;
                    break;
                }
            }
            // if nothing was found, switch on everything as a fallback
            if (found == -1)
                bestCfg = GenAvailCfg;
            // if no change is required, stay at current config
            else if (_configurations[found].GenReg == _currCfg)
                bestCfg = _currCfg;
            // switch to a higher configuration without waiting
            else if (_configurations[found].Pmax > currCfgPower)
                bestCfg = _configurations[found].GenReg;
            // only switch to a lower configurations if min run timer is expired
            else if (_minRunAct > 0)
                bestCfg = _currCfg;
            // only switch to a lower configuration if it is below the hysteresis of the current configuration
            else if (_configurations[found].Pmax < (currCfgPower - StatPHyst))
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
                    minRunTime = Math.Max(minRunTime, Gen[i].TminRun);
                }
            }
            return Math.Max(minRunTime, TminRun);
        }

        private double TotalPower(ushort cfg)
        {
            double power = 0;
            for (ushort i = 0; i < MaxGens; i++)
            {
                ushort genBit = (ushort)(1 << i);
                if ((genBit & cfg) == genBit)
                {
                    power += Gen[i].Pmax;
                }
            }
            return power;
        }

        private void StartStopGens(ushort cfg)
        {
            bool canStop = (cfg & Generator.OnlineGens) == cfg;
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
                    onlineCap += Gen[i].Pmax;
            }

            for (ushort i = 0; i < MaxGens; i++)
            {
                if (Gen[i].State == GeneratorState.RunningClosed)
                {
                    Gen[i].Pact = Gen[i].Pmax / onlineCap * StatPact;
                }
                else
                {
                    Gen[i].Pact = 0;
                }
            }
        }
    }
}
