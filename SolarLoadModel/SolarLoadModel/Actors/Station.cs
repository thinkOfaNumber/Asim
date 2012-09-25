using System;
using SolarLoadModel.Contracts;
using SolarLoadModel.Utils;

namespace SolarLoadModel.Actors
{
    class Station : IActor
    {
        private readonly Shared _pvP = SharedContainer.GetOrNew("PvP");
        private readonly Shared _loadP = SharedContainer.GetOrNew("LoadP");
        private readonly Shared _genCfgSetP = SharedContainer.GetOrNew("GenCfgSetP");
        private readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private readonly Shared _statP = SharedContainer.GetOrNew("StatP");
        private readonly Shared _statBlackCnt = SharedContainer.GetOrNew("StatBlackCnt");
        private bool _lastStatBlack = false;

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            // calc
            _genCfgSetP.Val = _loadP.Val - _pvP.Val;
            _statP.Val = _genP.Val + _pvP.Val;
            if (!_lastStatBlack && _statP.Val > 0)
            {
                _statBlackCnt.Val++;
            }
            _lastStatBlack = _statP.Val <= 0;
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
