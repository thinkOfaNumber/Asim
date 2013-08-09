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
    class Station : IActor
    {
        private readonly SharedContainer _sharedVars = SharedContainer.Instance;
        private readonly Shared _statP;
        private readonly Shared _statBlackCnt;
        private readonly Shared _statSpinP;
        private readonly Shared _statSpinSetP;
        private readonly Shared _statMaintainSpin;
        private readonly Shared _loadCapAl;
        private readonly Shared _loadCapMargin;
        private readonly Shared _loadMaxP;
        private readonly Shared _loadP;
        private readonly Shared _pvP;
        private readonly Shared _pvCvgPct;
        private readonly Shared _genCfgSetP;
        private readonly Shared _genCfgSetK;
        private readonly Shared _genP;
        private readonly Shared _genSetP;
        private readonly Shared _genOnlineCfg;
        private readonly Shared _genSpinP;
        private readonly Shared _genCapP;
        private readonly Shared _shedP;
        private readonly Shared _shedOffP;
        private static bool _lastStatBlack;
        private readonly Shared _statBlack;
        private double _genCoverP;

        public static bool BlackStartInit { get; private set; }
        public static bool IsBlack { get; private set; }
        public static double GenSetP { get; private set; }

        public Station()
        {
            _statP = _sharedVars.GetOrNew("StatP");
            _statBlackCnt = _sharedVars.GetOrNew("StatBlackCnt");
            _statSpinP = _sharedVars.GetOrNew("StatSpinP");
            _statSpinSetP = _sharedVars.GetOrNew("StatSpinSetP");
            _statMaintainSpin = _sharedVars.GetOrNew("StatMaintainSpin");
            _loadCapAl = _sharedVars.GetOrNew("LoadCapAl");
            _loadCapMargin = _sharedVars.GetOrNew("LoadCapMargin");
            _loadMaxP = _sharedVars.GetOrNew("LoadMaxP");
            _loadP = _sharedVars.GetOrNew("LoadSetP");
            _pvP = _sharedVars.GetOrNew("PvP");
            _pvCvgPct = _sharedVars.GetOrNew("PvCvgPct");
            _genCfgSetP = _sharedVars.GetOrNew("GenCfgSetP");
            _genCfgSetK = _sharedVars.GetOrNew("GenCfgSetK");
            _genP = _sharedVars.GetOrNew("GenP");
            _genSetP = _sharedVars.GetOrNew("GenSetP");
            _genOnlineCfg = _sharedVars.GetOrNew("GenOnlineCfg");
            _genSpinP = _sharedVars.GetOrNew("GenSpinP");
            _genCapP = _sharedVars.GetOrNew("GenCapP");
            _shedP = _sharedVars.GetOrNew("ShedP");
            _shedOffP = _sharedVars.GetOrNew("ShedOffP");
            _statBlack = _sharedVars.GetOrNew("StatBlack");
        }

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            // calc
            double pvCoverage = _pvP.Val * _pvCvgPct.Val / 100D;
            double reserve;

            if (_statMaintainSpin.Val > 0)
            {
                reserve = Math.Max(_statSpinSetP.Val, pvCoverage - _shedP.Val + _shedOffP.Val);
            }
            else
            {
                reserve = Math.Max(0, Math.Max(_statSpinSetP.Val, pvCoverage) - _shedP.Val + _shedOffP.Val);
            }

            // generator coverage setpoint
            _genCoverP = (_loadP.Val - _pvP.Val) + reserve;
            if (_genCfgSetK.Val <= 0 || _genCfgSetK.Val > 1)
                _genCfgSetK.Val = 1.0D;
            _genCfgSetP.Val = _genCfgSetP.Val * (1.0D - _genCfgSetK.Val) + _genCfgSetK.Val * _genCoverP;

            // actual generator loading setpoint
            GenSetP = _genSetP.Val = _loadP.Val - _pvP.Val;
            // station output
            _statP.Val = _genP.Val + _pvP.Val;
            // station spinning reserve
            _statSpinP.Val = _genSpinP.Val + _shedP.Val;

            // maximun load value
            _loadMaxP.Val = Math.Max(_loadP.Val, _loadMaxP.Val);
            // load capacity warning (GenCapP not set until after Station Run())
            _loadCapAl.Val = iteration > 0 && _genCapP.Val < (_loadMaxP.Val * _loadCapMargin.Val) ? 1.0D : 0.0D;
            
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
