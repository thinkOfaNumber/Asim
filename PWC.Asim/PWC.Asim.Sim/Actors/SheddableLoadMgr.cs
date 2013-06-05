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
using PWC.Asim.Sim.Contracts;
using PWC.Asim.Sim.Utils;

namespace PWC.Asim.Sim.Actors
{
    class SheddableLoadMgr : IActor
    {
        // Sum of switchable loads
        private readonly Shared _shedLoadP = SharedContainer.GetOrNew("ShedLoadP");
        // Online Sheddable load
        private readonly Shared _shedP = SharedContainer.GetOrNew("ShedP");
        // Offline load
        private readonly Shared _shedOffP = SharedContainer.GetOrNew("ShedOffP");

        private readonly Shared _statBlack = SharedContainer.GetOrNew("StatBlack");
        private readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private readonly Shared _genMaxP = SharedContainer.GetOrNew("GenMaxP");
        private readonly ISheddableLoad[] Load = new ISheddableLoad[Settings.MAX_GENS];
        private readonly ExecutionManager _executionManager = new ExecutionManager();
        private static bool debug = true;
        
        public void Run(ulong iteration)
        {
            _executionManager.RunActions(iteration);

            // todo: fix magic numbers - stop with 2% of max load and start when more than 8%
            double pctMaxLoad = GetLoadFactor();
            bool stop = pctMaxLoad > 99.0D;
            bool start = _statBlack.Val == 0 && pctMaxLoad < 92.0D;

            _shedLoadP.Val = 0;
            _shedP.Val = 0;
            _shedOffP.Val = 0;
            for (int i = 0; i < Settings.MAX_GENS; i++)
            {
                if (Load[i] == null)
                    continue;

                if (stop)
                    Load[i].Stop();
                if (start)
                    Load[i].Start();

                Load[i].Run(iteration);
                _shedP.Val += Load[i].ShedP;
                _shedOffP.Val += Load[i].ShedOffP;
                _shedLoadP.Val += Load[i].ShedLoadP;
            }
        }

        public void Init()
        {
            // create sheddable load (only one for now)
            Load[0] = new StaticLoad(0, _executionManager);
        }

        public void Finish()
        {
        }

        /// <summary>
        /// The sheddable load manager makes decisions based on the load
        /// factor if all sheddabl load was turned on.
        /// </summary>
        /// <returns></returns>
        public double GetLoadFactor()
        {
            return _genMaxP.Val <= 0 ? 0 : (_genP.Val + _shedOffP.Val) / _genMaxP.Val * 100;
        }
    }

    class StaticLoad : ISheddableLoad
    {
        public double ShedLoadP
        {
            get { return _shedLoadP.Val; }
        }

        public double ShedP
        {
            get { return _shedP.Val; }
        }

        public double ShedOffP
        {
            get { return _shedOffP.Val; }
        }

        public double ShedSpinP
        {
            get { return _shedSpinP.Val; }
        }

        // Maximum Off Time
        private readonly Shared _shedLoadMaxT;
        // Size of Load to switch on/off
        private readonly Shared _shedLoadP;
        // online amount of this load
        private readonly Shared _shedP;
        // offline amount of this load
        private readonly Shared _shedOffP;
        // amount this load can shed quickly
        private readonly Shared _shedSpinP;
        // Load shed latency
        private readonly Shared _shedLoadT;
        
        // Maximum Off Time
        private static readonly Shared ShedLoadMaxT = SharedContainer.GetOrNew("ShedLoadMaxT");
        // Load shed latency
        private static readonly Shared ShedLoadT = SharedContainer.GetOrNew("ShedLoadT");

        private ulong _actualOffTime;
        private ulong _actualOnTime;
        private bool _online;
        private bool _busy;
        private ulong _maxCycleTime;
        private ulong _it;

        private ExecutionManager _executionManager;

        private bool MaxOffTimeExpired
        {
            get { return _maxCycleTime != 0 && _actualOffTime > _maxCycleTime; }
        }

        private bool MinOnTimeSatisfied
        {
            get { return _maxCycleTime == 0 || _actualOnTime > _maxCycleTime; }
        }

        public StaticLoad(int id, ExecutionManager executionManager)
        {
            int n = id + 1;
            _online = false;

            _shedLoadMaxT = SharedContainer.GetOrNew("Shed" + n + "LoadMaxT");
            _shedLoadP = SharedContainer.GetOrNew("Shed" + n + "LoadP");
            _shedLoadT = SharedContainer.GetOrNew("Shed" + n + "LoadT");
            _shedP = SharedContainer.GetOrNew("Shed" + n + "P");
            _shedOffP = SharedContainer.GetOrNew("Shed" + n + "OffP");
            _shedSpinP = SharedContainer.GetOrNew("Shed" + n + "SpinP");

            _executionManager = executionManager;
        }


        public void Run(ulong iteration)
        {
            _it = iteration;
            _maxCycleTime = (ulong)Math.Max(_shedLoadMaxT.Val, ShedLoadMaxT.Val);

            if (MaxOffTimeExpired)
                Start();

            if (_online)
            {
                _actualOffTime = 0;
                _actualOnTime ++;
                _shedP.Val = _shedLoadP.Val;
                _shedOffP.Val = 0;
            }
            else
            {
                _actualOffTime++;
                _actualOnTime = 0;
                _shedP.Val = 0;
                _shedOffP.Val = _shedLoadP.Val;
            }
        }

        public void Start()
        {
            _online = true;
        }

        public void Stop()
        {
            if (_busy || !MinOnTimeSatisfied)
                return;

            _busy = true;
            ulong latency = Convert.ToUInt64(Math.Max(_shedLoadT.Val, ShedLoadT.Val));
            _executionManager.After(latency, () => { _online = false; _busy = false; });
        }
    }
}
