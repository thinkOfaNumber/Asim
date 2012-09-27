using System;
using System.Runtime.InteropServices;
using SolarLoadModel.Utils;
using SolarLoadModel.Contracts;

namespace SolarLoadModel.Actors
{
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
        private static readonly Generator[] Gen = new Generator[Settings.MAX_GENS];

        private readonly Shared _currCfg = SharedContainer.GetOrNew("GenSetCfg");
        private readonly Shared _genMinRunT = SharedContainer.GetOrNew("GenMinRunT");
        private readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private readonly Shared _genCfgSetP = SharedContainer.GetOrNew("GenCfgSetP");
        private readonly Shared _statHystP = SharedContainer.GetOrNew("StatHystP");
        private readonly Shared _statSpinSetP = SharedContainer.GetOrNew("StatSpinSetP");
        private readonly Shared _genAvailCfg = SharedContainer.GetOrNew("GenAvailCfg");
        private readonly Shared _genBlackCfg = SharedContainer.GetOrNew("GenBlackCfg");
        private readonly Shared _genMinRunTPa = SharedContainer.GetOrNew("GenMinRunTPa");

        private ulong _iteration;
        private readonly Configuration[] _configurations = new Configuration[Settings.MAX_CFG];
        private Double?[] _configurationPower = new Double?[Settings.MAX_CFG];

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
            Generator.RunAll();

            //
            // Set Outputs
            //
            _genP.Val = Generator.GenP;
        }

        public void Init()
        {
            for (int i = 0; i < Settings.MAX_GENS; i++)
            {
                Gen[i] = new Generator(i);
            }

            // test existance of variables we read from
            for (int i = 0; i < Settings.MAX_CFG; i++)
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

        private void GeneratorManager()
        {
            if (_genMinRunT.Val > 0 && ((ushort)_currCfg.Val & Generator.OnlineCfg) == (ushort)_currCfg.Val)
            {
                _genMinRunT.Val--;
            }
            // black start or select
            _configurationPower = new double?[Settings.MAX_CFG];
            //for (int i = 0; i < _configurationPower.Length; i++ )
            //{
            //    _configurationPower[i] = null;
            //}

            ushort newCfg = (ushort)(Generator.OnlineCfg == 0 ? _genBlackCfg.Val : SelectGens());

            if (newCfg != (ushort)_currCfg.Val)
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

            for (int i = 0; i < Settings.MAX_CFG; i++)
            {
                _configurations[i].Pmax = TotalPower((ushort)((ushort)_configurations[i].GenReg.Val & (ushort)_genAvailCfg.Val));
                if (_configurations[i].Pmax >= _genCfgSetP.Val + _statSpinSetP.Val)
                {
                    found = i;
                    break;
                }
            }
            // if nothing was found, switch on everything as a fallback
            if (found == -1)
                bestCfg = (ushort)_genAvailCfg.Val;
            // if no change is required, stay at current config
            else if (_configurations[found].GenReg.Val == _currCfg.Val)
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
            for (ushort i = 0; i < Settings.MAX_GENS; i++)
            {
                ushort genBit = (ushort)(1 << i);
                if ((genBit & cfg) == genBit)
                {
                    minRunTime = Math.Max(minRunTime, Gen[i].MinRunTPa);
                }
            }
            return Math.Max(minRunTime, (ulong)_genMinRunTPa.Val);
        }

        private double TotalPower(ushort cfg)
        {
            if (_configurationPower[cfg] == null)
            {
                double power = 0;
                for (ushort i = 0; i < Settings.MAX_GENS; i++)
                {
                    ushort genBit = (ushort) (1 << i);
                    if ((genBit & cfg) == genBit)
                    {
                        power += Gen[i].MaxP;
                    }
                }
                _configurationPower[cfg] = power;
            }
            return _configurationPower[cfg].Value;
        }

        private void StartStopGens(ushort cfg)
        {
            bool canStop = (cfg & Generator.OnlineCfg) == cfg;
            for (ushort i = 0; i < Settings.MAX_GENS; i++)
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
            for (int i = 0; i < Settings.MAX_GENS; i++)
            {
                if (Gen[i].State == GeneratorState.RunningClosed)
                    onlineCap += Gen[i].MaxP;
            }

            for (ushort i = 0; i < Settings.MAX_GENS; i++)
            {
                if (Gen[i].State == GeneratorState.RunningClosed)
                {
                    double setP = Gen[i].MaxP / onlineCap * _genCfgSetP.Val;
                    if (setP > Gen[i].MaxP || setP < 0)
                    {
                        Gen[i].CriticalStop();
                        Gen[i].P = 0;
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
