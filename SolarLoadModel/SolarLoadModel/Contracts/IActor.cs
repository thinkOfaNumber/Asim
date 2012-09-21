using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolarLoadModel.Contracts
{
    public interface IActor
    {
        void Run(ulong iteration);
        void Init(Dictionary<string, SharedValue> varPool);
        void Finish();
    }
}
