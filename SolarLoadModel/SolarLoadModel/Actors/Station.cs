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
    class Station : IActor
    {
        private readonly Shared _statP = SharedContainer.GetOrNew("StatP");
        private readonly Shared _statBlackCnt = SharedContainer.GetOrNew("StatBlackCnt");
        private readonly Shared _statSpinP = SharedContainer.GetOrNew("StatSpinP");
        private readonly Shared _loadCapAl = SharedContainer.GetOrNew("LoadCapAl");
        private readonly Shared _loadCapMargin = SharedContainer.GetOrNew("LoadCapMargin");
        private readonly Shared _loadMaxP = SharedContainer.GetOrNew("LoadMaxP");
        private readonly Shared _loadP = SharedContainer.GetOrNew("LoadP");
        private readonly Shared _loadMaxLimP = SharedContainer.GetOrNew("LoadMaxLimP");
        private readonly Shared _pvP = SharedContainer.GetOrNew("PvP");
        private readonly Shared _genCfgSetP = SharedContainer.GetOrNew("GenCfgSetP");
        private readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private readonly Shared _genOnlineCfg = SharedContainer.GetOrNew("GenOnlineCfg");
        private readonly Shared _genSpinP = SharedContainer.GetOrNew("GenSpinP");
        private readonly Shared _genCapP = SharedContainer.GetOrNew("GenCapP");
        private readonly Shared _disP = SharedContainer.GetOrNew("DisP");
        private bool _lastStatBlack = false;
        private bool _thisStatBlack = false;

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            // calc
            if (_loadMaxLimP.Val > 0)
                _loadP.Val = Math.Min(_loadP.Val, _loadMaxLimP.Val);
            double loadP = _loadP.Val + _disP.Val;

            _genCfgSetP.Val = loadP - _pvP.Val;
            _statP.Val = _genP.Val + _pvP.Val;
            _statSpinP.Val = _genSpinP.Val;

            _loadMaxP.Val = Math.Max(loadP, _loadMaxP.Val);
            _loadCapAl.Val = _genCapP.Val < (_loadMaxP.Val * _loadCapMargin.Val) ? 1.0F : 0.0F;

            _thisStatBlack = _genOnlineCfg.Val <= 0;
            if (_thisStatBlack && !_lastStatBlack)
            {
                _statBlackCnt.Val++;
            }
            _lastStatBlack = _thisStatBlack;
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
