using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolarLoadModel.Contracts
{
    public interface IDispatchLoad
    {
        /// <summary>
        /// Total / rated dispatchable load
        /// </summary>
        double DisLoadP { get; }
        /// <summary>
        /// Online dispatchable load
        /// </summary>
        double DisP { get; }
        /// <summary>
        /// Offline portion of dispatchable load
        /// </summary>
        double DisOffP { get; }
        /// <summary>
        /// Proportion of load that can be switched off soon
        /// </summary>
        double DisSpinP { get; }

        void Run();
        void Stop();
        void Start();
    }
}
