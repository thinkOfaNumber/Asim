using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SolarLoadModel.Contracts;
using SolarLoadModel.Exceptions;

namespace SolarLoadModel.Actors
{
    class Station : IActor
    {
        private SharedValue _pvP;
        private SharedValue _loadP;
        private readonly SharedValue _genCfgSetP = new SharedValue();
        private readonly SharedValue _genP = new SharedValue();
        private readonly SharedValue _statP = new SharedValue();
        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            // calc
            _genCfgSetP.Val = Math.Max(0, _loadP.Val - _pvP.Val);
            _statP.Val = _genP.Val + _pvP.Val;
        }

        public void Init(Dictionary<string, SharedValue> varPool)
        {
            varPool["StatP"] = _statP;
            varPool["GenCfgSetP"] = _genCfgSetP;
            varPool["GenP"] = _genP;
            // only test variables read from input files, not created by other actors
            _loadP = TestExistance(varPool, "LoadP");
            _pvP = TestExistance(varPool, "PvP");
        }

        public void Finish()
        {

        }

        #endregion

        private SharedValue TestExistance(Dictionary<string, SharedValue> varPool, string s)
        {
            SharedValue v;
            if (!varPool.TryGetValue(s, out v))
            {
                throw new SimulationException("Station simulator expected the variable '" + s + "' would exist by now.");
            }
            return v;
        }
    }
}
