// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of "Asim" - A Renewable Energy Power Station
// Control System Simulator
//
// Asim is free software: you can redistribute it and/or modify
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
using PWC.Asim.Sim.Exceptions;

namespace PWC.Asim.Sim.Utils
{
    /// <summary>
    /// Generator states.  Values are subject to change.
    /// </summary>
    [Flags]
    public enum GeneratorState
    {
        /// <summary>
        /// stopped, offline, open, etc.
        /// </summary>
        Stopped = 0x1,
        /// <summary>
        /// Running & CB open. Run counter incrememnted, but no load or fuel used
        /// </summary>
        RunningOpen = 0x2,
        /// <summary>
        /// Running & CB closed.  Run counter incrememted and set loaded
        /// </summary>
        RunningClosed = 0x4,
        /// <summary>
        /// Generator service counter reached, waiting for Gen#ServiceT to expire
        /// </summary>
        InService = 0x8,
        /// <summary>
        /// The Set is not available to be put online
        /// </summary>
        Unavailable = 0x10
    }

    /// <summary>
    /// Basic Generator class.
    /// </summary>
    /// <remarks>Locking is not required on state transitions becuase all commands
    /// happen at specific points in the iteration</remarks>
    public abstract class GeneratorBase
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
            protected set { _idealP.Val = value; }
        }
        public double LoadFact
        {
            get { return _loadFact.Val; }
            protected set { _loadFact.Val = value; }
        }
        public static ushort OnlineCfg
        {
            get { return (ushort)_onlineCfg.Val; }
            protected set { _onlineCfg.Val = value; }
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
            get { return _genOverload.Val != 0.0D; }
            set { _genOverload.Val = Convert.ToDouble(value); }
        }
        // counters
        public ulong StartCnt
        {
            get { return (ulong)_startCnt.Val; }
            protected set { _startCnt.Val = value; }
        }
        public ulong StopCnt
        {
            get { return (ulong)_stopCnt.Val; }
            protected set { _stopCnt.Val = value; }
        }
        public double FuelCnt
        {
            get { return _fuelCnt.Val; }
            protected set { _fuelCnt.Val = value; }
        }
        public double RunCnt
        {
            get { return _runCnt.Val; }
            protected set { _runCnt.Val = value; }
        }
        public double E
        {
            get { return _e.Val; }
            private set { _e.Val = value; }
        }

        private readonly Shared _maxP;
        protected readonly Shared _p;
        private readonly Shared _minRunTPa;
        private readonly Shared _idealPctP;
        private readonly Shared _idealP;
        private readonly Shared _loadFact;
        protected readonly Shared _serviceT;
        protected readonly Shared _serviceOutT;
        protected readonly Shared _serviceCnt;
        private readonly Shared[] _fuelCurveP = new Shared[Settings.FuelCurvePoints];
        private readonly Shared[] _fuelCurveL = new Shared[Settings.FuelCurvePoints];
        private static readonly Shared _onlineCfg = SharedContainer.GetOrNew("GenOnlineCfg");
        private static readonly Shared _genIdealP = SharedContainer.GetOrNew("GenIdealP");
        private static readonly Shared _genMaxP = SharedContainer.GetOrNew("GenMaxP");
        private static readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private static readonly Shared _genOverload = SharedContainer.GetOrNew("GenOverload");
        private static readonly Shared _genSpinP = SharedContainer.GetOrNew("GenSpinP");
        private static readonly Shared _genCapP = SharedContainer.GetOrNew("GenCapP");
        private static readonly Shared _genAvailSet = SharedContainer.GetOrNew("GenAvailSet");
        private static readonly Shared _genAvailCfg = SharedContainer.GetOrNew("GenAvailCfg");

        // counters
        private readonly Shared _startCnt;
        private readonly Shared _stopCnt;
        private readonly Shared _fuelCnt;
        private readonly Shared _runCnt;
        private readonly Shared _e;

        protected GeneratorState State { get; set; }

        protected static readonly ExecutionManager ExecutionManager = new ExecutionManager();
        private static ulong _iteration;
        private int _id;
        protected readonly ushort _idBit;
        protected double _spinP;

        private static readonly GeneratorBase[] Gen = new GeneratorBase[Settings.MAX_GENS];

        public GeneratorBase(int id)
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
            _e = SharedContainer.GetOrNew("Gen" + n + "E");
            _fuelCnt = SharedContainer.GetOrNew("Gen" + n + "FuelCnt");
            _maxP = SharedContainer.GetOrNew("Gen" + n + "MaxP");
            _minRunTPa = SharedContainer.GetOrNew("Gen" + n + "MinRunTPa");
            _idealPctP = SharedContainer.GetOrNew("Gen" + n + "IdealPctP");
            _idealP = SharedContainer.GetOrNew("Gen" + n + "IdealP");
            _serviceT = SharedContainer.GetOrNew("Gen" + n + "ServiceT");
            _serviceOutT = SharedContainer.GetOrNew("Gen" + n + "ServiceOutT");
            _serviceCnt = SharedContainer.GetOrNew("Gen" + n + "ServiceCnt");
            for (int i = 0; i < Settings.FuelCurvePoints; i++)
            {
                // Gen1Cons1P, Gen1Cons1L; Gen1Cons2P, Gen1Cons2L; ...
                _fuelCurveP[i] = SharedContainer.GetOrNew("Gen" + n + "FuelCons" + (i + 1) + "P");
                _fuelCurveL[i] = SharedContainer.GetOrNew("Gen" + n + "FuelCons" + (i + 1) + "L");
            }
            Gen[id] = this;
        }

        static GeneratorBase()
        {
            _onlineCfg.Val = 0;
            _genIdealP.Val = 0;
            _genP.Val = 0;
            _genOverload.Val = 0;
            _genCapP.Val = 0;
        }

        public static void RunAll()
        {
            GenIdealP = 0;
            GenP = 0;
            Overload = false;
            _genSpinP.Val = 0;
            _genAvailCfg.Val = 0;
            _genCapP.Val = 0;
            _genMaxP.Val = 0;
            int largestGeni = 0;
            double largestGenP = 0;
            for (int i = 0; i < Settings.MAX_GENS; i ++)
            {
                if (((ushort)_genAvailSet.Val & (1<<i)) == 0)
                    continue;

                Gen[i].Run();
                GenIdealP += Gen[i].IdealP;
                Overload = Overload || (Gen[i].LoadFact > 1);

                // don't add non-available generators to these calculations
                if (!Gen[i].IsAvailable())
                    continue;
                _genAvailCfg.Val = ((ushort)_genAvailCfg.Val | (1 << i));
                GenP += Gen[i].P;
                _genSpinP.Val += Gen[i]._spinP;
                _genCapP.Val += Gen[i]._maxP.Val;
                _genMaxP.Val += Gen[i].IsOnline() ? Gen[i]._maxP.Val : 0;
                if (Gen[i].MaxP > largestGenP)
                {
                    largestGenP = Gen[i].MaxP;
                    largestGeni = i;
                }
            }
            _genCapP.Val = _genCapP.Val - Gen[largestGeni].P;
        }

        public static void ResetAllAvailableSets()
        {
            for (int i = 0; i < Settings.MAX_GENS; i++)
            {
                if (!Gen[i].IsAvailable())
                    continue;

                Gen[i].Reset();
            }
        }

        protected abstract void Reset();

        public abstract void Start();

        public abstract void Stop();

        public abstract void CriticalStop();

        protected abstract void Service();

        protected virtual void Run()
        {
            LoadFact = 0;
            IdealP = 0;
            _spinP = 0;
            if (IsRunningOffline())
            {
                RunCnt += Settings.PerHourToSec;
                FuelCnt += FuelConsumptionSecond();
            }
            else if (IsOnline())
            {
                RunCnt += Settings.PerHourToSec;
                E += P * Settings.PerHourToSec;
                LoadFact = P / MaxP;
                FuelCnt += FuelConsumptionSecond();
                IdealP = MaxP * IdealPctP / 100;
                _spinP = Math.Max(MaxP - P, 0);
                if (_serviceT.Val > 0 && RunCnt > _serviceT.Val)
                {
                    Service();
                }
            }
        }

        /// <summary>
        /// Calculates fuel consumption for one second of operation at the current load factor.
        /// </summary>
        /// <returns>Fuel used this second, in L</returns>
        protected double FuelConsumptionSecond()
        {
            int i = FindFirstPoint();

            // slope
            double m = (_fuelCurveL[i].Val - _fuelCurveL[i + 1].Val) / (_fuelCurveP[i].Val - _fuelCurveP[i + 1].Val);

            // use the point slope formula where x1,y1 is the point using [i], and x is the load factor
            // y - y1 = m(x - x1)
            // or
            // y = m(x - x1) + y1

            double y = m*(LoadFact - _fuelCurveP[i].Val) + _fuelCurveL[i].Val;

            // now y is the L/kWh, but we want L so multiply by kWh per iteration
            double consumptionSecond = y*P*Settings.PerHourToSec;

            return consumptionSecond;
        }

        /// <summary>
        /// Finds the first of two x,y (LoadFactor vs L/kWh) points that a
        /// given Load Factor falls within.
        /// </summary>
        /// <returns></returns>
        private int FindFirstPoint()
        {
            int? lastPoint = null;

            // find the last point
            for (int i = 1; i < Settings.FuelCurvePoints; i++)
            {
                if (_fuelCurveP[i].Val != 0 || _fuelCurveL[i].Val != 0)
                {
                    lastPoint = i;
                }
            }
            if (!lastPoint.HasValue)
            {
                throw new SimulationException("Only one fuel curve point was found for Generator " + (_id + 1) + ", iteration " + _iteration);
            }

            // found is set to zero since we now know at least 2 points exist, and if the LoadFactor
            // is less than all given points, we should extend the first point backwards.
            int found = 0;
            for (int i = 0; i < lastPoint; i++)
            {
                if (LoadFact > _fuelCurveP[i].Val)
                    found = i;
            }
            // if the load factor is beyond the end of the array, then use the
            // previous two points and extend the curve
            if (found == Settings.FuelCurvePoints - 1)
                found = Settings.FuelCurvePoints - 2;

            return found;
        }

        public static void UpdateStates(ulong iteration)
        {
            _iteration = iteration;
            ExecutionManager.RunActions(_iteration);
        }

        #region State Helpers

        public bool IsOnline()
        {
            return (State & GeneratorState.RunningClosed) == GeneratorState.RunningClosed;
        }

        public bool IsRunningOffline()
        {
            return (State & GeneratorState.RunningOpen) == GeneratorState.RunningOpen;
        }

        public bool IsStopped()
        {
            return (State & GeneratorState.Stopped) == GeneratorState.Stopped;
        }

        public bool IsAvailable()
        {
            return (State & GeneratorState.Unavailable) == 0;
        }

        #endregion State Helpers
    }
}
