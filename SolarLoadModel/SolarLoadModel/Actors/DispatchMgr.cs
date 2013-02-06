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
using SolarLoadModel.Contracts;
using SolarLoadModel.Utils;

namespace SolarLoadModel.Actors
{
    class DispatchMgr : IActor
    {
        // Sum of switchable loads
        private readonly Shared _disLoadP = SharedContainer.GetOrNew("DisLoadP");
        // Online Dispatchable load
        private readonly Shared _disP = SharedContainer.GetOrNew("DisP");
        // Offline load
        private readonly Shared _disOffP = SharedContainer.GetOrNew("DisOffP");

        private readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private readonly Shared _genMaxP = SharedContainer.GetOrNew("GenMaxP");
        private readonly IDispatchLoad[] Load = new IDispatchLoad[Settings.MAX_GENS];
        private readonly ExecutionManager _executionManager = new ExecutionManager();

        public void Read(ulong iteration)
        {
        }

        public void Run(ulong iteration)
        {
            _executionManager.RunActions(iteration);
            
            _disLoadP.Val = 0;
            _disP.Val = 0;
            _disOffP.Val = 0;

            // todo: fix magic numbers - stop with 2% of max load and start when more than 8%
            double pctMaxLoad = (_genMaxP.Val - _genP.Val) / _genMaxP.Val * 100;
            bool stop = pctMaxLoad < 2;
            bool start = pctMaxLoad > 8;

            for (int i = 0; i < Settings.MAX_GENS; i++)
            {
                if (Load[i] == null)
                    continue;

                if (stop)
                    Load[i].Stop();
                if (start)
                    Load[i].Start();

                Load[i].Run();
                _disP.Val += Load[i].DisP;
                _disOffP.Val += Load[i].DisOffP;
                _disLoadP.Val += Load[i].DisLoadP;
            }
        }

        public void Write(ulong iteration)
        {
        }

        public void Init()
        {
            // create dispatchable load (only one for now)
            Load[0] = new StaticLoad(0, _executionManager);
        }

        public void Finish()
        {
        }
    }

    class StaticLoad : IDispatchLoad
    {
        public double DisLoadP
        {
            get { return _disLoadP.Val; }
        }

        public double DisP
        {
            get { return _disP.Val; }
        }

        public double DisOffP
        {
            get { return _disOffP.Val; }
        }

        public double DisSpinP
        {
            get { return _disSpinP.Val; }
        }

        // Maximum Off Time
        private readonly Shared _disLoadMaxT;
        // Size of Load to switch on/off
        private readonly Shared _disLoadP;
        // online amount of this load
        private readonly Shared _disP;
        // offline amount of this load
        private readonly Shared _disOffP;
        // amount this load can shed quickly
        private readonly Shared _disSpinP;
        // Load shed latency
        private readonly Shared _disLoadT;
        
        // Maximum Off Time
        private static readonly Shared DisLoadMaxT = SharedContainer.GetOrNew("DisLoadMaxT");
        // Load shed latency
        private static readonly Shared DisLoadT = SharedContainer.GetOrNew("DisLoadT");

        private ulong _actualOffTime;
        private bool _online;
        private bool _busy;
        private ulong _maxOffTime;

        private ExecutionManager _executionManager;

        private bool MaxOffTimeExpired
        {
            get
            {
                _maxOffTime = (ulong)Math.Max(_disLoadMaxT.Val, DisLoadMaxT.Val);
                return (_actualOffTime > _maxOffTime);
            }
        }

        public StaticLoad(int id, ExecutionManager executionManager)
        {
            int n = id + 1;
            _online = false;

            _disLoadMaxT = SharedContainer.GetOrNew("Dis" + n + "LoadMaxT");
            _disLoadP = SharedContainer.GetOrNew("Dis" + n + "LoadP");
            _disLoadT = SharedContainer.GetOrNew("Dis" + n + "LoadT");
            _disP = SharedContainer.GetOrNew("Dis" + n + "P");
            _disOffP = SharedContainer.GetOrNew("Dis" + n + "OffP");
            _disSpinP = SharedContainer.GetOrNew("Dis" + n + "SpinP");

            _executionManager = executionManager;
        }


        public void Run()
        {
            if (MaxOffTimeExpired)
                Start();

            if (_online)
            {
                _actualOffTime = 0;
                _disP.Val = _disLoadP.Val;
                _disOffP.Val = 0;
            }
            else
            {
                _actualOffTime++;
                _disP.Val = 0;
                _disOffP.Val = _disLoadP.Val;
            }
        }

        public void Start()
        {
            _online = true;
        }

        public void Stop()
        {
            if (_busy)
                return;

            _busy = true;
            ulong latency = Convert.ToUInt64(Math.Max(_disLoadT.Val, DisLoadT.Val));
            _executionManager.After(latency, () => { _online = false; _busy = false; });
        }
    }
}
