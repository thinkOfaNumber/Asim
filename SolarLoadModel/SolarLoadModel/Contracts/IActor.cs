using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolarLoadModel.Contracts
{
    public interface IActor
    {
        void Run(Dictionary<string, double> varPool, ulong iteration);
        void Init(Dictionary<string, double> varPool);
        void Finish();
    }
}
