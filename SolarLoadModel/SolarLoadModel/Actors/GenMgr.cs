// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of the Solar Load Model - A Renewable Energy Power Station
// Control System Simulator
//
// The Solar Load Model is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
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
        public string E;
        public string FuelCnt;
        public string FuelCons;
    }

    public class GenMgr : IActor
    {
        private static readonly Generator[] Gen = new Generator[Settings.MAX_GENS];

        private readonly Shared _genDesiredCfg = SharedContainer.GetOrNew("GenSetCfg");
        private ushort GenSetCfg
        {
            get { return (ushort)_genDesiredCfg.Val; }
            set { _genDesiredCfg.Val = value; }
        }
        private readonly Shared _genMinRunT = SharedContainer.GetOrNew("GenMinRunT");
        private readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private readonly Shared _genLowP = SharedContainer.GetOrNew("GenLowP");
        private readonly Shared _genCfgSetP = SharedContainer.GetOrNew("GenCfgSetP");
        private readonly Shared _genSetP = SharedContainer.GetOrNew("GenSetP");
        private readonly Shared _statHystP = SharedContainer.GetOrNew("StatHystP");
        private readonly Shared _genAvailCfg = SharedContainer.GetOrNew("GenAvailCfg");
        private readonly Shared _genBlackCfg = SharedContainer.GetOrNew("GenBlackCfg");
        private readonly Shared _genMinRunTPa = SharedContainer.GetOrNew("GenMinRunTPa");

        private ulong _iteration;
        private readonly Shared[] _configurations = new Shared[Settings.MAX_CFG];
        private Double?[] _configurationPower = new Double?[Settings.MAX_CFG];

        //private readonly ExecutionManager _executionManager = new ExecutionManager();

        #region Implementation of IActor
        
        public void Read(ulong iteration)
        {
            _iteration = iteration;
        }

        public void Run(ulong iteration)
        {
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
        public void Write(ulong iteration)
        {
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
                _configurations[i] = SharedContainer.GetOrNew(cstr);
                _configurations[i].Val = 0;
            }
        }

        public void Finish()
        {
            
        }

        #endregion

        private void GeneratorManager()
        {
            if (_genMinRunT.Val > 0 && (GenSetCfg & Generator.OnlineCfg) == GenSetCfg)
            {
                _genMinRunT.Val --;
            }
            // force recalculation of configuration power
            _configurationPower = new double?[Settings.MAX_CFG];

            // black start or select
            ushort newCfg;
            double lowerP;
            if (Generator.OnlineCfg == 0 && BlackStartPower() > _genCfgSetP.Val)
            {
                newCfg = (ushort)_genBlackCfg.Val;
                lowerP = 0;
            }
            else
            {
                newCfg = (ushort)((ushort)_genAvailCfg.Val & SelectGens(out lowerP));
            }

            if (newCfg != GenSetCfg)
            {
                // prime the minimum run timer on config changes
                _genMinRunT.Val = MinimumRunTime(newCfg);
            }
            GenSetCfg = newCfg;
            _genLowP.Val = lowerP;

            StartStopGens(newCfg);
            LoadShare();
        }

        private double BlackStartPower()
        {
            return TotalPower((ushort)((ushort)_genAvailCfg.Val & (ushort)_genBlackCfg.Val));
        }

        /// <summary>
        /// Select the best configuration of generators to run, based on setpoint,
        /// hysteresis, switch down timers, etc.
        /// </summary>
        /// <param name="lowerP">output showing the next lower configuration power (if found), or 0.</param>
        /// <returns>The selected configuration to put online.</returns>
        private ushort SelectGens(out double lowerP)
        {
            int found = -1;
            int nextLower = -1;
            ushort bestCfg = 0;
            double currCfgPower = TotalPower(GenSetCfg);

            for (int i = 0; i < Settings.MAX_CFG; i++)
            {
                // ignore configurations with unavailable sets
                if (((ushort)_configurations[i].Val & (ushort)_genAvailCfg.Val) != (ushort)_configurations[i].Val)
                    continue;
                double thisP = TotalPower((ushort)((ushort)_configurations[i].Val & (ushort)_genAvailCfg.Val));
                if (thisP >= _genCfgSetP.Val)
                {
                    found = i;
                    break;
                }
                if (nextLower == -1 || thisP > TotalPower((ushort)_configurations[nextLower].Val))
                {
                    nextLower = i;
                }
            }
            double foundP = found == -1 ? 0 : TotalPower((ushort)_configurations[found].Val);
            // if nothing was found, switch on everything as a fallback
            if (found == -1)
                bestCfg = (ushort)_genAvailCfg.Val;
            // if no change is required, stay at current config
            else if (_configurations[found].Val == GenSetCfg)
                bestCfg = GenSetCfg;
            // switch to a higher configuration without waiting
            else if (foundP > currCfgPower)
                bestCfg = (ushort)_configurations[found].Val;
            // only switch to a lower configurations if min run timer is expired
            else if (_genMinRunT.Val > 0)
                bestCfg = GenSetCfg;
            // only switch to a lower configuration if it is below the hysteresis of the current configuration
            else if (foundP < (currCfgPower - _statHystP.Val))
                bestCfg = (ushort)_configurations[found].Val;
            // don't switch to a smaller configuration as Hysteresis wasn't satisfied
            else
                bestCfg = GenSetCfg;
            
            lowerP = nextLower == -1 ? 0 : TotalPower((ushort)_configurations[nextLower].Val);
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
                if ((genBit & cfg) == genBit && !Gen[i].IsOnline())
                {
                    Gen[i].Start();
                }
                if (((genBit & cfg) == 0) && canStop && !Gen[i].IsStopped())
                {
                    Gen[i].Stop();
                }
            }
        }

        /// <summary>
        /// simple load sharing:
        /// - load is taken / dropped immediatly
        /// - load factor is evened across all online sets
        /// - generators can be loaded up to MaxP at which point they trip
        /// </summary>
        private void LoadShare()
        {
            // figure out actual online capacity
            double onlineCap = 0;
            for (int i = 0; i < Settings.MAX_GENS; i++)
            {
                if (Gen[i].IsOnline())
                    onlineCap += Gen[i].MaxP;
            }

            for (ushort i = 0; i < Settings.MAX_GENS; i++)
            {
                if (Gen[i].IsOnline())
                {
                    double setP = Gen[i].MaxP / onlineCap * _genSetP.Val;
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
