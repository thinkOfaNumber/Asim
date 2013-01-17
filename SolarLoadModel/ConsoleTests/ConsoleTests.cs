// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of the Solar Load Model - A Renewable Energy Power Station
// Control System Simulator
//
// The Solar Load Model is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ConsoleTests
{
    public class ConsoleTests : TestBase
    {
        [Test]
        public void RunWithNoArguments()
        {
            // Check exit is normal
            Assert.AreEqual(0, StartConsoleApplication(""));

            // Check run works
            Assert.IsTrue(ConsoleOutput.ToString().Contains("inner loop took"));
            Assert.IsTrue(ConsoleOutput.ToString().Contains("Press any key to continue"));
        }

        [Test]
        public void ShowUsage()
        {
            // Throw file not found. Exit code is 1
            Assert.AreEqual(1, StartConsoleApplication("FooBarBaz"));

            Assert.IsTrue(ConsoleOutput.ToString().Contains("Error: Unknown argument"));
            Assert.IsTrue(ConsoleOutput.ToString().Contains("Usage"));
        }

        [Test]
        public void TestStatistics()
        {
            // Arrange
            int iterations = 100000;
            var inFile = GetTempFilename;
            var outFile = GetTempFilename;
            double[] random = new double[iterations];
            double sum = 0, min = Double.MaxValue, max = Double.MinValue;
            FillWithRandomData(ref random);
            foreach (var d in random)
            {
                sum += d;
                max = Math.Max(max, d);
                min = Math.Min(min, d);
            }
            double average = sum / iterations;

            File.WriteAllText(inFile, BuildCsvFor("LoadP", random).ToString());

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --input {1} --output {2} {0} LoadP",
                    iterations, inFile, outFile));
            var fileArray = CsvFileToArray(outFile);

            double loadPmin = Convert.ToDouble(fileArray.ElementAt(1)[1]);
            int loadPminT = Convert.ToInt32(fileArray.ElementAt(1)[2]);
            double loadPmax = Convert.ToDouble(fileArray.ElementAt(1)[3]);
            int loadPmaxT = Convert.ToInt32(fileArray.ElementAt(1)[4]);
            double loadPave = Convert.ToDouble(fileArray.ElementAt(1)[5]);

            // Assert
            // completed successfully
            Assert.AreEqual(retValue, 0);

            // header row and one result
            Assert.AreEqual(fileArray.Count(), 2);

            // number of cells in all rows is the same
            Assert.IsTrue(fileArray.TrueForAll(l => l.Length == 6));

            // header row is t,v_min,v_minT,v_max,v_maxT,v_ave
            Assert.AreEqual(
                String.Join(",", fileArray.ElementAt(0)),
                "t,LoadP_min,LoadP_minT,LoadP_max,LoadP_maxT,LoadP_ave");

            // statistics value is close enough
            Assert.IsTrue(DoublesAreEqual(loadPmin, min));
            //Assert.IsTrue(DoublesAreEqual(loadPminT, minT));
            Assert.IsTrue(DoublesAreEqual(loadPmax, max));
            //Assert.IsTrue(DoublesAreEqual(loadPmaxT, maxT));
            Assert.IsTrue(DoublesAreEqual(loadPave, average));
        }

        [Test]
        public void TestMinMaxStats()
        {
            int iterations = 100000;
            var inFile = GetTempFilename;
            var outFile = GetTempFilename;
            int[] random = new int[iterations];
            int min = int.MaxValue, max = int.MinValue, minT = 0, maxT = 0;
            FillWithRandomData(ref random, 0, 1000);
            for (int i = 0; i < random.Length; i++)
            {
                if (random[i] < min)
                {
                    min = random[i];
                    minT = i;
                }
                else if (random[i] > max)
                {
                    max = random[i];
                    maxT = i;
                }
            }

            File.WriteAllText(inFile, BuildCsvFor("LoadP", random).ToString());

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --input {1} --output {2} {0} LoadP",
                    iterations, inFile, outFile));
            var fileArray = CsvFileToArray(outFile);

            int loadPminT = Convert.ToInt32(fileArray.ElementAt(1)[2]);
            int loadPmaxT = Convert.ToInt32(fileArray.ElementAt(1)[4]);

            // Assert
            // completed successfully
            Assert.AreEqual(retValue, 0);

            // header row and one result
            Assert.AreEqual(fileArray.Count(), 2);

            // number of cells in all rows is the same
            Assert.IsTrue(fileArray.TrueForAll(l => l.Length == 6));

            // header row is t,v_min,v_minT,v_max,v_maxT,v_ave
            Assert.AreEqual(
                String.Join(",", fileArray.ElementAt(0)),
                "t,LoadP_min,LoadP_minT,LoadP_max,LoadP_maxT,LoadP_ave");

            // statistics value is close enough
            Assert.AreEqual(loadPminT, minT);
            Assert.AreEqual(loadPmaxT, maxT);
        }

        [Test]
        public void FuelUsage()
        {
            
        }
    }
}
