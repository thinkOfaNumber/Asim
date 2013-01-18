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
        
        public void Run(ulong iteration)
        {
            DispatchLoad.RunAll(iteration);
        }

        public void Init()
        {
            // create dispatchable load (only one for now)
            new DispatchLoad(0);
        }

        public void Finish()
        {
        }
    }

    class DispatchLoad
    {
        // Sum of switchable loads
        private static readonly Shared DisLoadP = SharedContainer.GetOrNew("DisLoadP");
        // Online Dispatchable load
        private static readonly Shared DisP = SharedContainer.GetOrNew("DisP");
        private static readonly Shared GenSpinP = SharedContainer.GetOrNew("GenSpinP");
        private static readonly Shared StatSpinSetP = SharedContainer.GetOrNew("StatSpinSetP");
        // Maximum Off Time
        private static readonly Shared DisLoadMaxT = SharedContainer.GetOrNew("DisLoadMaxT");
        // Load shed latency
        private static readonly Shared DisLoadT = SharedContainer.GetOrNew("DisLoadT");

        // Maximum Off Time
        private readonly Shared _disLoadMaxT;
        // Size of Load to switch on/off
        private readonly Shared _disLoadP;
        // Load shed latency
        private readonly Shared _disLoadT;
        private ulong _actualOffTime;
        private bool _online;
        private bool _busy;
        private ulong _maxOffTime;

        private static readonly DispatchLoad[] Load = new DispatchLoad[Settings.MAX_GENS];
        private static readonly ExecutionManager ExecutionManager = new ExecutionManager();

        private bool MaxOffTimeExpired
        {
            get
            {
                _maxOffTime = (ulong)Math.Max(_disLoadMaxT.Val, DisLoadMaxT.Val);
                return (_actualOffTime > _maxOffTime);
            }
        }

        public DispatchLoad(int id)
        {
            int n = id + 1;
            _online = false;

            _disLoadMaxT = SharedContainer.GetOrNew("Dis" + n + "LoadMaxT");
            _disLoadP = SharedContainer.GetOrNew("Dis" + n + "LoadP");
            _disLoadT = SharedContainer.GetOrNew("Dis" + n + "LoadT");
            Load[id] = this;
        }

        public static void RunAll(ulong iteration)
        {
            ExecutionManager.RunActions(iteration);

            bool stop = GenSpinP.Val < StatSpinSetP.Val;
            // start dispatchable loads when the spinning reserve, less any
            // offline dispatchable load, is still less than the station spinning
            // reserve setpoint
            bool start = (GenSpinP.Val - (DisLoadP.Val - DisP.Val)) > StatSpinSetP.Val;

            DisLoadP.Val = 0;
            DisP.Val = 0;
            for (int i = 0; i < Settings.MAX_GENS; i++)
            {
                if (Load[i] == null) continue;
                bool thisstop = stop && !Load[i].MaxOffTimeExpired;
                bool thisstart = start || Load[i].MaxOffTimeExpired;

                if (thisstop)
                {
                    if (Load[i]._online) Load[i].Stop();
                }
                else if (thisstart)
                    Load[i].Start();

                Load[i].Run();
                DisLoadP.Val += Load[i]._disLoadP.Val;
                if (Load[i]._online)
                    DisP.Val += Load[i]._disLoadP.Val;
                ;
            }
        }

        public void Run()
        {
            if (_online)
                _actualOffTime = 0;
            else
                _actualOffTime++;
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
            ExecutionManager.After(latency, () => { _online = false; _busy = false; });
        }
    }
}
