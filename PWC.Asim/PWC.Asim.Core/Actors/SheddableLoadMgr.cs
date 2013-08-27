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
using PWC.Asim.Core.Contracts;
using PWC.Asim.Core.Utils;

namespace PWC.Asim.Core.Actors
{
    public class SheddableLoadMgr : IActor
    {
        private readonly SharedContainer _sharedVars = SharedContainer.Instance;
        // Sum of switchable loads available to be shed
        private readonly Shared _shedLoadP;
        // Online Sheddable load
        private readonly Shared _shedP;
        // Offline load
        private readonly Shared _shedOffP;
        // Percent of online rated load to switch off at (eg 99)
        private readonly Shared _shedIdealPct;
        // energy counter for offline sheddable load
        private readonly Shared _shedE;

        // amount this load can shed quickly
        private readonly Shared _shedSpinP;
        // Load shed latency
        private readonly Shared _shedLoadT;

        private readonly Shared _statBlack;
        private readonly Shared _genP;
        private readonly Shared _genMaxP;
        private readonly ExecutionManager _executionManager = new ExecutionManager();

        private const int MaxLatency = 5 * Settings.SecondsInAMinute;
        private readonly double[] _shedLatencyLoad = new double[MaxLatency + 1];
        private int _delayIt = 0;
        private int _nowIt = 0;
        private double _actLoad;
        
        public SheddableLoadMgr()
        {
            _shedLoadP = _sharedVars.GetOrNew("ShedLoadP");
            _shedP = _sharedVars.GetOrNew("ShedP");
            _shedOffP = _sharedVars.GetOrNew("ShedOffP");
            _statBlack = _sharedVars.GetOrNew("StatBlack");
            _genP = _sharedVars.GetOrNew("GenP");
            _genMaxP = _sharedVars.GetOrNew("GenMaxP");
            _shedIdealPct = _sharedVars.GetOrNew("ShedIdealPct");
            _shedE = _sharedVars.GetOrNew("ShedE");
            _shedLoadT = _sharedVars.GetOrNew("ShedLoadT");
            _shedSpinP = _sharedVars.GetOrNew("ShedSpinP");

            _shedLoadT.OnValueChanged += _shedLoadT_OnValueChanged;
            _statBlack.OnValueChanged += _statBlack_OnValueChanged;
        }

        void _shedLoadT_OnValueChanged(object sender, SharedEventArgs e)
        {
            int latency = Convert.ToInt32(e.NewValue);
            if (latency < 0)
                latency = 0;
            if (latency > MaxLatency)
                latency = MaxLatency;
            _delayIt = _nowIt - latency;
            if (_delayIt < 0)
                _delayIt += MaxLatency + 1;
        }

        void _statBlack_OnValueChanged(object sender, SharedEventArgs e)
        {
            if (e.NewValue < 0)
                return;

            // after a blackout, forget all the load latency values
            for (int i = 0; i < MaxLatency; i++)
                _shedLatencyLoad[i] = 0;
        }

        public void Run(ulong iteration)
        {
            _executionManager.RunActions(iteration);

            if (_statBlack.Val < 1)
            {
                // actual load if all shed load is on
                _actLoad = _genP.Val + _shedOffP.Val;

                // calculate load to shed but put it in the latency array
                _shedLatencyLoad[_nowIt] = Math.Max(0, _genP.Val - _shedIdealPct.Val * _genMaxP.Val / 100);

                // Limit to available sheddable load
                _shedLatencyLoad[_nowIt] = Math.Min(_shedLoadP.Val, _shedLatencyLoad[_nowIt]);

                // apply latent load to actual offline shed load
                _shedOffP.Val = _shedLatencyLoad[_delayIt];

                // remaining online sheddable load
                _shedSpinP.Val = _shedP.Val = _shedLoadP.Val - _shedOffP.Val;

                // energy that should be online
                _shedE.Val += _shedP.Val * Settings.PerHourToSec;
            }
            else
            {
                _actLoad = 0;
                _shedLatencyLoad[_nowIt] = 0;
                _shedOffP.Val = _shedLoadP.Val;
                _shedP.Val = 0;
                _shedSpinP.Val = 0;
            }

            if (++_delayIt > MaxLatency)
                _delayIt = 0;
            if (++_nowIt > MaxLatency)
                _nowIt = 0;
        }

        public void Init()
        {
        }

        public void Finish()
        {
        }

        /// <summary>
        /// The sheddable load manager makes decisions based on the load
        /// factor if all sheddable load was turned on.
        /// </summary>
        /// <returns></returns>
        private double GetLoadFactor()
        {
            return _genMaxP.Val <= 0 ? 0 : (_genP.Val + _shedOffP.Val) / _genMaxP.Val * 100;
        }
    }
}
