using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PWC.Asim.Core.Utils;

namespace ConsoleTests
{
    class RteTests : TestBase
    {
        [SetUp]
        public void Cleanup()
        {
            CoreCleanup();
        }

        [Test]
        public void RunTimeExtension()
        {
            // Arrange
            var settingsFile = GetTempFilename;
            var rteFile = GetTempFilename;
            var outFile = GetTempFilename;
            int iterations = 1000;

            var values = new SortedDictionary<string, double[]>();
            InsertFuelConsumption(values, 0.33);
            values["LoadP"] = new double[] { 50 };
            values["FooVar"] = new double[] { 0 };

            File.WriteAllLines(rteFile, new []
                {
                    "FooVar = LoadP * 1.5D",
                    ""
                });

            StringBuilder simSettings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile, simSettings.ToString());

            var sim = new Simulator();
            sim.AddInput(settingsFile);
            sim.AddOutput(outFile, new[] {"LoadP{Act}", "FooVar{Act}"}, (uint)iterations);
            sim.AddEval(rteFile);
            sim.Iterations = (ulong)iterations;

            // Act
            sim.Simulate();

            // Assert
            var fileArray = CsvFileToArray(outFile);

            var loadP = Convert.ToDouble(fileArray[2][1]);
            var fooVar = Convert.ToInt32(fileArray[2][2]);

            Assert.AreEqual(75.0D, fooVar);
        }
    }
}
