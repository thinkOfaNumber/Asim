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
        private List<Shared> _toScale;

        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            if (_toScale == null)
            {
                _toScale = new List<Shared>();
                var names = SharedContainer.GetAllNames();
                foreach (string name in names)
                {
                    var s = SharedContainer.GetExisting(name);
                    if (s.ScaleFunction != null)
                    {
                        _toScale.Add(s);
                    }
                }
            }

            foreach (var shared in _toScale)
            {
                if (shared.ScaleFunction != null)
                {
                    shared.Val = shared.ScaleFunction(shared.Val);
                    shared.ScaleFunction = null;
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
