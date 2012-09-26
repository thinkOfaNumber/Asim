using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SolarLoadModel.Contracts;
using SolarLoadModel.Utils;

namespace SolarLoadModel.Actors
{
    class Solar : IActor
    {
        private readonly Shared _pvP = SharedContainer.GetOrNew("PvP");
        private readonly Shared _pvAvailP = SharedContainer.GetOrNew("PvAvailP");
        private readonly Shared _pvSetP = SharedContainer.GetOrNew("PvSetP");
        private readonly Shared _pvSpillP = SharedContainer.GetOrNew("PvSpillP");
        private readonly Shared _pvECnt = SharedContainer.GetOrNew("PvECnt");
        private readonly Shared _pvSetMaxDownP = SharedContainer.GetOrNew("PvSetMaxDownP");
        private readonly Shared _pvSetMaxUpP = SharedContainer.GetOrNew("PvSetMaxUpP");

        private readonly Shared _statSpinP = SharedContainer.GetOrNew("StatSpinP");
        private readonly Shared _genP = SharedContainer.GetOrNew("GenP");
        private readonly Shared _genIdealP = SharedContainer.GetOrNew("GenIdealP");

        private double _wantedSetP = 0;
        private double _deltaSetP = 0;
        private const double PerHourToSec = 1 / (60.0 * 60.0);

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            // calculate desired setpoint
            _wantedSetP = Math.Min(_statSpinP.Val, _genP.Val - _genIdealP.Val);

            // limit setpoint to available solar power
            _wantedSetP = Math.Min(_wantedSetP, _pvAvailP.Val);

            // limit setpoint change up/down per second
            _deltaSetP = _pvSetP.Val - _wantedSetP;
            if (_deltaSetP > 0)
            {
                _deltaSetP = Math.Min(_deltaSetP, _pvSetMaxUpP.Val);
            }
            if (_deltaSetP < 0)
            {
                _deltaSetP = Math.Min(_deltaSetP, _pvSetMaxDownP.Val);
            }

            // apply ramp limited setpoint
            _pvSetP.Val += _deltaSetP;

            // assume solar farm outputs this immediatly
            _pvP.Val = _pvSetP.Val;

            // calculate spill
            _pvSpillP.Val = _pvAvailP.Val - _pvP.Val;

            // calculate energy
            _pvECnt.Val += (_pvP.Val * PerHourToSec);
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
