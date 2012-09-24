using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SolarLoadModel.Utils;

namespace SolarLoadModel.Contracts
{
    public interface IActor
    {
        void Run(ulong iteration);
        void Init();
        void Finish();
    }
}
