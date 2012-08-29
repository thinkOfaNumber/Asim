using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// Basic Generator class.  Locking is not required on state transitions becuase all commands
    /// happen at specific points in the iteration
    /// </summary>
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

        private const double PerHourToSec = 1 / (60.0 * 60.0);
        private double _fuelConsKws;
        private readonly Object _genThreadLock = new Object();
        private static readonly ExecutionManager ExecutionManager = new ExecutionManager();
        private bool _busy;
        private ulong iteration;
        private int _id;

        // counters
        public int Nstarts { get; private set; }
        public int Nstops { get; private set; }
        public GeneratorState State { get; private set; }

        public Generator(int id)
        {
            _id = id;
            State = GeneratorState.Stopped;
        }

        public void Start()
        {
            lock (_genThreadLock)
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
        }
        public void Stop()
        {
            lock (_genThreadLock)
            {
                if (_busy)
                    return;

                if (State == GeneratorState.RunningClosed)
                {
                    ExecutionManager.After(60, TransitionToStop);
                    Nstops++;
                    _busy = true;
                    State = GeneratorState.RunningOpen;
                }
            }
        }

        public void Tick(ulong iteration)
        {
            this.iteration = iteration;
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
            ExecutionManager.RunActions(iteration);
        }

        private void TransitionToOnline()
        {
            lock (_genThreadLock)
            {
                State = GeneratorState.RunningClosed;
                _busy = false;
            }
        }
        private void TransitionToStop()
        {
            lock (_genThreadLock)
            {
                State = GeneratorState.Stopped;
                _busy = false;
            }
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
        private const ushort MaxCfg = 20;

        private static readonly Generator[] Gen = new Generator[MaxGens];
        private readonly ushort[] _configs = new ushort[MaxCfg];
        private ushort _currCfg;
        private double StatPact;
        private double StatPHyst;
        private double StatPspin;
        private ushort GenAvailCfg;
        private ulong iteration;
        private readonly GenStrings[] _varStr = new GenStrings[MaxGens];

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

                if (iteration == 0)
                {
                    ushort cfg = (ushort)((2 << i) - 1);
                    _configs[i] = cfg;
                }
            }
            StatPact = varPool["StatPact"];
            StatPHyst = varPool["StatPHyst"];
            StatPspin = varPool["StatPspin"];
            GenAvailCfg = Convert.ToUInt16(varPool["GenAvailCfg"]);
            //_executionManager.RunActions(iteration);
            
            //
            // Simulate
            //
            _currCfg = SelectGens();
            StartStopGens(_currCfg);
            LoadShare();
            double genPact = 0;
            bool overload = false;
            for (int i = 0; i < MaxGens; i++)
            {
                Gen[i].Tick(iteration);
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

            // test existance of variables we read from
            TestExistance(varPool, "StatPact");
            TestExistance(varPool, "StatPHyst");
            TestExistance(varPool, "StatPspin");
            TestExistance(varPool, "GenAvailCfg");
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
                throw new VarNotFoundException("GenMgr simulator expected the variable '" + s + "' would exist by now.");
            }
        }

        private ushort SelectGens()
        {
            bool found = false;
            ushort cfg = 0;
            double pwr = 0;

            for (int i = 0; i < MaxCfg; i++)
            {
                var thisPwr = TotalPower((ushort)(_configs[i] & GenAvailCfg));
                if (thisPwr >= StatPact + StatPspin)
                {
                    cfg = _configs[i];
                    pwr = thisPwr;
                    found = true;
                    break;
                }
            }
            if (!found || (cfg == _currCfg))
                return _currCfg;
            if (pwr > TotalPower(_currCfg))
            {
                return cfg;
            }
            // now swithcing to a smaller configuration:
            if ((pwr - StatPHyst) > StatPact)
            {
                return cfg;
            }
            // don't switch to a smaller configuration as Hysteresis wasn't satisfied
            return _currCfg;
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
            for (ushort i = 0; i < MaxGens; i++)
            {
                ushort genBit = (ushort)(1<<i);
                if ((genBit & cfg) == genBit)
                {
                    Gen[i].Start();
                }
                else
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
                ushort genBit = (ushort)(1 << i);
                if (Gen[i].State == GeneratorState.RunningClosed && ((genBit & _currCfg) == genBit))
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
