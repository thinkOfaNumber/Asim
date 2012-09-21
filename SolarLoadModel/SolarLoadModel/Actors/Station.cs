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
        private double _pvP;
        private double _loadP;
        private double _genCfgSetP;
        private double _genP;
        private double _statP;
        #region Implementation of IActor

        public void Run(Dictionary<string, double> varPool, ulong iteration)
        {
            // inputs
            _loadP = varPool["LoadP"];
            _pvP = varPool["PvP"];
            _genP = varPool["GenP"];

            // calc
            _genCfgSetP = Math.Max(0, _loadP - _pvP);
            _statP = _genP + _pvP;

            // outputs
            varPool["GenCfgSetP"] = _genCfgSetP;
            varPool["StatP"] = _statP;
        }

        public void Init(Dictionary<string, double> varPool)
        {
            varPool["StatP"] = 0;
            varPool["GenCfgSetP"] = 0;
            // only test variables read from input files, not created by other actors
            TestExistance(varPool, "LoadP");
            TestExistance(varPool, "PvP");
        }

        public void Finish()
        {

        }

        #endregion

        private void TestExistance(Dictionary<string, double> varPool, string s)
        {
            double dummy;
            if (!varPool.TryGetValue(s, out dummy))
            {
                throw new SimulationException("Station simulator expected the variable '" + s + "' would exist by now.");
            }
        }
    }
}
