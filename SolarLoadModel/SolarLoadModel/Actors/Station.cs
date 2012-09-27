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
        private readonly Shared _loadP = SharedContainer.GetOrNew("LoadP");
        private readonly Shared _pvP = SharedContainer.GetOrNew("PvP");
        private readonly Shared _pvAvailP = SharedContainer.GetOrNew("PvAvailP");
        private readonly Shared _genCfgSetP = SharedContainer.GetOrNew("GenCfgSetP");
        private readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private readonly Shared _genOnlineCfg = SharedContainer.GetOrNew("GenOnlineCfg");
        private readonly Shared _genSpinP = SharedContainer.GetOrNew("GenSpinP");
        private bool _lastStatBlack = false;
        private bool _thisStatBlack = false;

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            // calc
            _genCfgSetP.Val = _loadP.Val - _pvP.Val;
            _statP.Val = _genP.Val + _pvP.Val;
            _statSpinP.Val = _genSpinP.Val;

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
