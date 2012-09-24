using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SolarLoadModel.Contracts;
using SolarLoadModel.Exceptions;
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
        private readonly Shared _statBlack = SharedContainer.GetOrNew("StatBlack");

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            // calc
            _genCfgSetP.Val = Math.Max(0, _loadP.Val - _pvP.Val);
            _statP.Val = _genP.Val + _pvP.Val;
            _statBlack.Val = Convert.ToDouble(_genP.Val <= 0);
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
