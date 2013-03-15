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
using PWC.Asim.Sim.Contracts;
using PWC.Asim.Sim.Utils;

namespace PWC.Asim.Sim.Actors
{
    class Station : IActor
    {
        private readonly Shared _statP = SharedContainer.GetOrNew("StatP");
        private readonly Shared _statBlackCnt = SharedContainer.GetOrNew("StatBlackCnt");
        private readonly Shared _statSpinP = SharedContainer.GetOrNew("StatSpinP");
        private readonly Shared _statSpinSetP = SharedContainer.GetOrNew("StatSpinSetP");
        private readonly Shared _loadCapAl = SharedContainer.GetOrNew("LoadCapAl");
        private readonly Shared _loadCapMargin = SharedContainer.GetOrNew("LoadCapMargin");
        private readonly Shared _loadMaxP = SharedContainer.GetOrNew("LoadMaxP");
        private readonly Shared _loadP = SharedContainer.GetOrNew("LoadP");
        private readonly Shared _loadMaxLimP = SharedContainer.GetOrNew("LoadMaxLimP");
        private readonly Shared _pvP = SharedContainer.GetOrNew("PvP");
        private readonly Shared _pvCvgPct = SharedContainer.GetOrNew("PvCvgPct");
        private readonly Shared _genCfgSetP = SharedContainer.GetOrNew("GenCfgSetP");
        private readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private readonly Shared _genSetP = SharedContainer.GetOrNew("GenSetP");
        private readonly Shared _genOnlineCfg = SharedContainer.GetOrNew("GenOnlineCfg");
        private readonly Shared _genSpinP = SharedContainer.GetOrNew("GenSpinP");
        private readonly Shared _genCapP = SharedContainer.GetOrNew("GenCapP");
        private readonly Shared _disP = SharedContainer.GetOrNew("DisP");
        private static bool _lastStatBlack;
        private readonly Shared _statBlack = SharedContainer.GetOrNew("StatBlack");

        public static bool BlackStartInit { get; private set; }
        public static bool IsBlack { get; private set; }
        public static double GenSetP { get; private set; }

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            // calc
            if (_loadMaxLimP.Val > 0)
                _loadP.Val = Math.Min(_loadP.Val, _loadMaxLimP.Val);

            double pvCoverage = _pvP.Val * _pvCvgPct.Val / 100D;
            double reserve = Math.Max(0, Math.Max(_statSpinSetP.Val, pvCoverage) - _disP.Val);

            // generator coverage setpoint
            _genCfgSetP.Val = _loadP.Val - _pvP.Val + reserve;
            // actual generator loading setpoint
            GenSetP = _genSetP.Val = _loadP.Val - _pvP.Val;
            // station output
            _statP.Val = _genP.Val + _pvP.Val;
            // station spinning reserve
            _statSpinP.Val = _genSpinP.Val + _disP.Val;

            // maximun load value
            _loadMaxP.Val = Math.Max(_loadP.Val, _loadMaxP.Val);
            // load capacity warning
            _loadCapAl.Val = _genCapP.Val < (_loadMaxP.Val * _loadCapMargin.Val) ? 1.0D : 0.0D;
            
            // blackout detection
            IsBlack = _genOnlineCfg.Val <= 0;
            _statBlack.Val = IsBlack ? 1 : 0;
            BlackStartInit = false;
            if (IsBlack && !_lastStatBlack)
            {
                _statBlackCnt.Val++;
                BlackStartInit = true;
            }
            _lastStatBlack = IsBlack;
        }

        public void Init()
        {

        }

        public void Finish()
        {

        }

        #endregion
    }
}
