using System;
using System.Collections;
using System.Collections.Generic;
using SolarLoadModel.Contracts;
using SolarLoadModel.Utils;

namespace SolarLoadModel.Actors
{
    public class TestActor : IActor
    {
        #region Implementation of IActor

        public void Run(ulong iteration)
        {
            Console.WriteLine("1: hello, world!");
        }

        public void Init()
        {
            Console.WriteLine("1: init.");
        }

        public void Finish()
        {
            Console.WriteLine("1: finish.");
        }

        #endregion
    }
}
