// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of "Asim" - A Renewable Energy Power Station
// Control System Simulator
//
// Asim is free software: you can redistribute it and/or modify
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
        public void ReReadInput()
        {
            var recycledFile = GetTempFilename;
            var readOnceFile = GetTempFilename;
            int iterations = 5 * 60 * 60;
            var outFile = GetTempFilename;
            int period = 10 * 60;

            StringBuilder settings = BuildCsvFor("RecycleCnt", new[] { 10, 20 }, period);
            File.WriteAllText(recycledFile, settings.ToString());
            settings = BuildCsvFor("NoRecycleCnt", new[] { 10, 20 }, period);
            File.WriteAllText(readOnceFile, settings.ToString());

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --input {1} recycle --input {2} --output {3} {4} RecycleCnt,NoRecycleCnt",
                    iterations, recycledFile, readOnceFile, outFile, period));

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);
            var fileArray = CsvFileToArray(outFile);

            var recycleCnt = Convert.ToDouble(fileArray[1][1]);
            var noRecycleCnt = Convert.ToDouble(fileArray[1][2]);
            Assert.IsTrue(DoublesAreEqual(10, recycleCnt));
            Assert.IsTrue(DoublesAreEqual(10, noRecycleCnt));

            recycleCnt = Convert.ToDouble(fileArray[2][1]);
            noRecycleCnt = Convert.ToDouble(fileArray[2][2]);
            Assert.IsTrue(DoublesAreEqual(20, recycleCnt));
            Assert.IsTrue(DoublesAreEqual(20, noRecycleCnt));

            // ignore the last row as it may have a different period
            for (int row = 3; row < fileArray.Count - 1; row++)
            {
                recycleCnt = Convert.ToDouble(fileArray[row][1]);
                noRecycleCnt = Convert.ToDouble(fileArray[row][2]);
                // this should oscillate 10, 20, 10, 20...
                Assert.IsTrue(DoublesAreEqual(10 * (2 - (row % 2)), recycleCnt));
                // this should remain 20
                Assert.IsTrue(DoublesAreEqual(20, noRecycleCnt));
            }
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

            // Assert
            // completed successfully
            Assert.AreEqual(retValue, 0);
            var fileArray = CsvFileToArray(outFile);

            double loadPmin = Convert.ToDouble(fileArray.ElementAt(2)[1]);
            int loadPminT = Convert.ToInt32(fileArray.ElementAt(2)[2]);
            double loadPmax = Convert.ToDouble(fileArray.ElementAt(2)[3]);
            int loadPmaxT = Convert.ToInt32(fileArray.ElementAt(2)[4]);
            double loadPave = Convert.ToDouble(fileArray.ElementAt(2)[5]);

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

            // Assert
            // completed successfully
            Assert.AreEqual(retValue, 0);
            var fileArray = CsvFileToArray(outFile);

            int loadPminT = Convert.ToInt32(fileArray.ElementAt(2)[2]);
            int loadPmaxT = Convert.ToInt32(fileArray.ElementAt(2)[4]);

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
            InsertFuelConsumption(values, fuelConst);
            values["Gen1MaxP"] = new double[] { 100 };
            values["GenConfig1"] = new double[] { 1 };
            values["GenAvailSet"] = new double[] { 1 };
            values["GenBlackCfg"] = new double[] { 1 };
            values["LoadP"] = new double[] { constLoad };

            StringBuilder fuelsettings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile, fuelsettings.ToString());

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --input {1} --output {2} {0} Gen1E,Gen1FuelCnt",
                    iterations, settingsFile, outFile));

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);
            var fileArray = CsvFileToArray(outFile);
            
            var totalE = Convert.ToDouble(fileArray[2][1]);
            var totalFuel = Convert.ToDouble(fileArray[2][2]);

            // convert kWs to kWh
            Assert.IsTrue(DoublesAreEqual((iterations - 61) * constLoad / 60 / 60, totalE));
            Assert.IsTrue(DoublesAreEqual(totalFuel, fuelConst * totalE));
        }

        [Test]
        public void ServiceCounter()
        {
            var settingsFile = GetTempFilename;
            int nServices = 5;
            int serviceInterval = 120;
            int serviceOutage = 5;
            int iterations =
                // make nServices by running for serviceInterval * nServices+1 hours
                serviceInterval * (nServices + 1) * 60 * 60
                // plus every shut down will take x hours
                + nServices * serviceOutage * 60 * 60;
            var outFile = GetTempFilename;

            var values = new SortedDictionary<string, double[]>();
            InsertFuelConsumption(values, 0.33);
            values["Gen1MaxP"] = new double[] { 100 };
            values["Gen1ServiceT"] = new double[] { serviceInterval };
            values["Gen1ServiceOutT"] = new double[] { serviceOutage };
            values["GenConfig1"] = new double[] { 1 };
            values["GenAvailSet"] = new double[] { 1 };
            values["GenBlackCfg"] = new double[] { 1 };
            values["LoadP"] = new double[] { 50 };

            StringBuilder fuelsettings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile, fuelsettings.ToString());

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --input {1} --output {2} {0} Gen1RunCnt,Gen1ServiceCnt",
                    iterations, settingsFile, outFile));

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);
            var fileArray = CsvFileToArray(outFile);

            var runCnt = Convert.ToDouble(fileArray[2][1]);
            var serviceCnt = Convert.ToInt32(fileArray[2][2]);
            Assert.AreEqual(nServices, serviceCnt);
        }

        [Test]
        public void DispatchableLoad()
        {
            var settingsFile1 = GetTempFilename;
            var settingsFile2 = GetTempFilename;
            var outFile = GetTempFilename;
            int maxOffTime = 20 * 60;
            int offLatency = 120;
            int period = 10 * 60;

            var values = new SortedDictionary<string, double[]>();
            InsertFuelConsumption(values, 0.33);
            values["StatSpinSetP"] = new double[] { 50 };
            values["Gen1MaxP"] = new double[] { 500 };
            values["GenConfig1"] = new double[] { 1 };
            values["GenAvailSet"] = new double[] { 1 };
            values["GenBlackCfg"] = new double[] { 1 };
            values["DisLoadMaxT"] = new double[] { maxOffTime };
            values["DisLoadT"] = new double[] { offLatency };
            values["Dis1LoadP"] = new double[] { 40 };
            var loadProfile = new double[] { 50, 100, 200, 300, 440, 440, 440, 440, 400 };
            int iterations = (loadProfile.Count() + 1) * period;

            StringBuilder settings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile1, settings.ToString());
            settings = BuildCsvFor("LoadP", loadProfile, period);
            File.WriteAllText(settingsFile2, settings.ToString());

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --input {1} --input {2} --output {3} Gen1P,DisLoadP,GenSpinP,DisP",
                    iterations, settingsFile1, settingsFile2, outFile));

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);
            var fileArray = CsvFileToArray(outFile);
            var gen1P = fileArray.Select(col => col[1]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();
            var disLoadP = fileArray.Select(col => col[2]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();
            var geSpinP = fileArray.Select(col => col[3]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();
            var disP = fileArray.Select(col => col[4]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();

            // at 60s: load 50 + disLoad 0
            Assert.IsTrue(DoublesAreEqual(50, gen1P.ElementAt(60)));
            // at 65s: load 50 + disload 40
            Assert.IsTrue(DoublesAreEqual(90, gen1P.ElementAt(65)));
            // at 10m: load 100 + disload 40
            Assert.IsTrue(DoublesAreEqual(140, gen1P.ElementAt(10 * 60)));
            // at 20m: load 200 + disload 40
            Assert.IsTrue(DoublesAreEqual(240, gen1P.ElementAt(20 * 60)));
            // at 30m: load 300 + disload 40
            Assert.IsTrue(DoublesAreEqual(340, gen1P.ElementAt(30 * 60)));
            // at 40m: load 440 + disload 40
            Assert.IsTrue(DoublesAreEqual(480, gen1P.ElementAt(40 * 60)));

            // StatSpinSetP is no longer maintained so the dispatchable load
            // should shut off automatically
            // at 40m+latency: load 440 + disload 0
            Assert.IsTrue(DoublesAreEqual(440, gen1P.ElementAt(40 * 60 + offLatency + 5)));

            // at 50m: load 440 + disload 0
            Assert.IsTrue(DoublesAreEqual(440, gen1P.ElementAt(50 * 60)));
            // at 60m: load 440 + disload 0
            Assert.IsTrue(DoublesAreEqual(440, gen1P.ElementAt(60 * 60)));

            // this tests that the dispatchable load turns on when the min run
            // time has expired, regardless of spinning reserve.  This is twenty
            // minutes after it turned off (at 42m).
            // at 60m+5s: load 440 + disload 40
            Assert.IsTrue(DoublesAreEqual(480, gen1P.ElementAt(60 * 60 + offLatency + 5)));

            // at 70m: load 400 + disload 40
            Assert.IsTrue(DoublesAreEqual(440, gen1P.ElementAt(70 * 60)));
        }

        [Test]
        public void LoadCapAl()
        {
            // Arrange
            var settingsFile1 = GetTempFilename;
            var settingsFile2 = GetTempFilename;
            var outFile = GetTempFilename;
            const int period = 10 * 60;

            var values = new SortedDictionary<string, double[]>();
            InsertFuelConsumption(values, 0.33, 4);
            values["LoadCapMargin"] = new double[] { 1 };
            values["Gen1MaxP"] = new double[] { 500 };
            values["Gen2MaxP"] = new double[] { 500 };
            values["Gen3MaxP"] = new double[] { 500 };
            values["Gen4MaxP"] = new double[] { 500 };
            values["GenAvailSet"] = new double[] { 15 };
            values["GenConfig1"] = new double[] { 15 };
            var loadProfile = new double[] { 1400, 1400, 1500, 1500, 1999, 1999, 2000, 2000, 2001 };
            int iterations = (loadProfile.Count() + 1) * period;

            StringBuilder settings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile1, settings.ToString());
            settings = BuildCsvFor("LoadP", loadProfile, period);
            File.WriteAllText(settingsFile2, settings.ToString());

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --input {1} --input {2} --output {3} GenP,LoadP,LoadCapAl",
                    iterations, settingsFile1, settingsFile2, outFile));

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);
            var fileArray = CsvFileToArray(outFile);
            var genP = fileArray.Select(col => col[1]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();
            var loadP = fileArray.Select(col => col[2]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();
            var loadCapAl = fileArray.Select(col => col[3]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();

            for (int i = 0; i < 2400; i++)
            {
                Assert.AreEqual(loadCapAl[i], 0.0D);
            }
            for (int i = 2400; i < loadCapAl.Count; i++)
            {
                Assert.AreEqual(loadCapAl[i], 1.0D);
            }
        }

        /// <summary>
        /// Inserts the given constant fuel consumption into the value dictionary
        /// </summary>
        /// <param name="values">dictionary of values for testing</param>
        /// <param name="fuelConst">fuel consumption L/kWh</param>
        /// <param name="nGens">number of Generators to insert values for</param>
        private void InsertFuelConsumption(SortedDictionary<string, double[]> values, double fuelConst, int nGens = 1)
        {
            for (int i = 1; i < nGens + 1; i++)
            {
                values["Gen" + i + "FuelCons1P"] = new double[] { 0 };
                values["Gen" + i + "FuelCons1L"] = new double[] { fuelConst };
                values["Gen" + i + "FuelCons2P"] = new double[] { 1 };
                values["Gen" + i + "FuelCons2L"] = new double[] { fuelConst };
                values["Gen" + i + "FuelCons3P"] = new double[] { 0 };
                values["Gen" + i + "FuelCons3L"] = new double[] { 0 };
                values["Gen" + i + "FuelCons4P"] = new double[] { 0 };
                values["Gen" + i + "FuelCons4L"] = new double[] { 0 };
                values["Gen" + i + "FuelCons5P"] = new double[] { 0 };
                values["Gen" + i + "FuelCons5L"] = new double[] { 0 };
            }
        }
    }
}
