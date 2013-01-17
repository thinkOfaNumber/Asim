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

            double loadPmin = Convert.ToDouble(fileArray.ElementAt(2)[1]);
            int loadPminT = Convert.ToInt32(fileArray.ElementAt(2)[2]);
            double loadPmax = Convert.ToDouble(fileArray.ElementAt(2)[3]);
            int loadPmaxT = Convert.ToInt32(fileArray.ElementAt(2)[4]);
            double loadPave = Convert.ToDouble(fileArray.ElementAt(2)[5]);

            // Assert
            // completed successfully
            Assert.AreEqual(retValue, 0);

            // header row, start row and one result
            Assert.AreEqual(3, fileArray.Count());

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

            int loadPminT = Convert.ToInt32(fileArray.ElementAt(2)[2]);
            int loadPmaxT = Convert.ToInt32(fileArray.ElementAt(2)[4]);

            // Assert
            // completed successfully
            Assert.AreEqual(retValue, 0);

            // header row, start row and one result
            Assert.AreEqual(3, fileArray.Count());

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
            var settingsFile = GetTempFilename;
            int iterations = 100000;
            var outFile = GetTempFilename;
            double fuelConst = 0.33;
            double constLoad = 50;

            var values = new SortedDictionary<string, double[]>();
            values["Gen1FuelCons1P"] = new double[] { 0 };
            values["Gen1FuelCons1L"] = new double[] { fuelConst };
            values["Gen1FuelCons2P"] = new double[] { 1 };
            values["Gen1FuelCons2L"] = new double[] { fuelConst };
            values["Gen1FuelCons3P"] = new double[] { 0 };
            values["Gen1FuelCons3L"] = new double[] { 0 };
            values["Gen1FuelCons4P"] = new double[] { 0 };
            values["Gen1FuelCons4L"] = new double[] { 0 };
            values["Gen1FuelCons5P"] = new double[] { 0 };
            values["Gen1FuelCons5L"] = new double[] { 0 };
            values["Gen1MaxP"] = new double[] { 100 };
            values["GenConfig1"] = new double[] { 1 };
            values["GenAvailCfg"] = new double[] { 1 };
            values["GenBlackCfg"] = new double[] { 1 };
            values["LoadP"] = new double[] { constLoad };

            StringBuilder fuelsettings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile, fuelsettings.ToString());

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --input {1} --output {2} {0} Gen1E,Gen1FuelCnt",
                    iterations, settingsFile, outFile));
            var fileArray = CsvFileToArray(outFile);

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);
            
            var totalE = Convert.ToDouble(fileArray[2][1]);
            var totalFuel = Convert.ToDouble(fileArray[2][2]);

            // convert watt-seconds to kWh
            Assert.IsTrue(DoublesAreEqual((iterations - 60) * constLoad / 60 / 60 / 1000, totalE));
            Assert.IsTrue(DoublesAreEqual(totalFuel, fuelConst * totalE));
        }

        [Test]
        public void ServiceCounter()
        {
            var settingsFile = GetTempFilename;
            int nServices = 5;
            int serviceInterval = 300;
            int iterations =
                // make nServices by running for serviceInterval * nServices+1 hours
                serviceInterval * (nServices + 1) * 60 * 60
                // plus every shut down will take 6 hours
                + nServices * 6 * 60 * 60;
            var outFile = GetTempFilename;

            var values = new SortedDictionary<string, double[]>();
            values["Gen1MaxP"] = new double[] { 100 };
            values["Gen1ServiceT"] = new double[] { 300 };
            values["GenConfig1"] = new double[] { 1 };
            values["GenAvailCfg"] = new double[] { 1 };
            values["GenBlackCfg"] = new double[] { 1 };
            values["LoadP"] = new double[] { 50 };

            StringBuilder fuelsettings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile, fuelsettings.ToString());

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --input {1} --output {2} {0} Gen1RunCnt,Gen1ServiceCnt",
                    iterations, settingsFile, outFile));
            var fileArray = CsvFileToArray(outFile);

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);

            var runCnt = Convert.ToDouble(fileArray[2][1]);
            var serviceCnt = Convert.ToInt32(fileArray[2][2]);
            Assert.AreEqual(nServices, serviceCnt);
        }
    }
}
