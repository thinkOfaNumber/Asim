using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SolarLoadModel.Contracts;
using SolarLoadModel.Utils;

namespace SolarLoadModel.Actors
{
    class ScaleValues : IActor
    {
        private Shared[] _values;
        private int _total;

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            if (_values == null)
            {
                var names = SharedContainer.GetAllNames();
                _total = names.Count;

                _values = new Shared[_total];
                for (int i = 0; i < _total; i++)
                {
                    _values[i] = SharedContainer.GetExisting(names[i]);
                }
            }
            for (int i = 0; i < _total; i++)
            {
                if (_values[i].ScaleFunction != null)
                {
                    _values[i].Val = _values[i].ScaleFunction(_values[i].Val);
                    _values[i].ScaleFunction = null;
                }
            }
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
