using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolarLoadModel.Exceptions
{
    class VarNotFoundException : System.Exception
    {
        public VarNotFoundException()
        {
        }

        public VarNotFoundException(string message)
            : base(message)
        {
        }

        public VarNotFoundException(string message,
            Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
