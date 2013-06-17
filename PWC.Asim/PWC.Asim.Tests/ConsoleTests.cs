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
                string.Format("--iterations {0} --input {1} recycle --input {2} --output {3} {4} RecycleCnt NoRecycleCnt",
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

        protected void FuelUsagePointTest(double[] p, double[] l)
        {
            // In these instances we don't care about results as the program should fail
            FuelUsagePointTest(p, l, 0, 0.5, false);
        }

        protected void FuelUsagePointTest(double[] p, double[] l, double expectedConsumption, double loadFactor, bool expectSuccess)
        {
            const int iterations = 1000;
            const double gen1MaxP = 100;
            var settingsFile = GetTempFilename;
            var outFile = GetTempFilename;
            if (loadFactor < 0 || loadFactor > 1)
            {
                throw new ArgumentException("loadFactor must be: 0 <= x <= 1");
            }

            var values = new SortedDictionary<string, double[]>();
            values["Gen1FuelCons1P"] = new [] { p[0] };
            values["Gen1FuelCons1L"] = new [] { l[0] };
            values["Gen1FuelCons2P"] = new [] { p[1] };
            values["Gen1FuelCons2L"] = new [] { l[1] };
            values["Gen1FuelCons3P"] = new [] { p[2] };
            values["Gen1FuelCons3L"] = new [] { l[2] };
            values["Gen1FuelCons4P"] = new [] { p[3] };
            values["Gen1FuelCons4L"] = new [] { l[3] };
            values["Gen1FuelCons5P"] = new [] { p[4] };
            values["Gen1FuelCons5L"] = new [] { l[4] };
            values["Gen1MaxP"] = new double[] { gen1MaxP };
            values["GenConfig1"] = new double[] { 1 };
            values["GenAvailSet"] = new double[] { 1 };
            values["GenBlackCfg"] = new double[] { 1 };
            values["LoadP"] = new double[] { gen1MaxP * loadFactor };

            StringBuilder fuelsettings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile, fuelsettings.ToString());

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --input {1} --output {2} {0} Gen1E Gen1FuelCnt",
                    iterations, settingsFile, outFile));

            // Assert
            // Completed with or without errors
            Assert.AreEqual(expectSuccess ? 0 : 1, retValue);
            if (!expectSuccess)
                return;

            var fileArray = CsvFileToArray(outFile);

            var totalE = Convert.ToDouble(fileArray[2][1]);
            var totalFuel = Convert.ToDouble(fileArray[2][2]);
            
            Assert.IsTrue(DoublesAreEqual(totalFuel, totalE * expectedConsumption));
        }

        [Test]
        public void FuelUsageNoPoints()
        {
            // this will fail as there are no fuel curve points at all
            FuelUsagePointTest(new double[] {0, 0, 0, 0, 0}, new double[] {0, 0, 0, 0, 0});
        }

        [Test]
        public void FuelUsageOnePoint()
        {
            // this will fail as there are no fuel curve points at all
            FuelUsagePointTest(new double[] { 1, 0, 0, 0, 0 }, new double[] { 0.33, 0, 0, 0, 0 });
        }

        [Test]
        public void FuelUsageLoadFactorNotFound()
        {
            const double fuelConst = 0.33;

            // this will pass even though the load facter is beyond the highest point
            FuelUsagePointTest(new double[] { 0, .2, 0.4, 0, 0 }, new double[] { fuelConst, fuelConst, fuelConst, 0, 0 }, fuelConst, 0.5, true );
        }

        [Test]
        public void FuelUsageOnPointStart()
        {
            // This tests that the fuel usage should be equal to the first fuel usage point since the load factor is on that point

            FuelUsagePointTest(new double[] { 0.5, 1, 0, 0, 0 }, new double[] { 0.6, 0.4, 0, 0, 0 }, 0.6, 0.5, true);
        }

        [Test]
        public void FuelUsageOnPointEnd()
        {
            // This tests that the fuel usage should be equal to the second fuel usage point since the load factor is on that point

            FuelUsagePointTest(new double[] { 0.1, 0.5, 1, 0, 0 }, new double[] { 0.1, 0.6, 0.4, 0, 0 }, 0.4, 1, true);
        }

        [Test]
        public void FuelUsageMidWay()
        {
            // This tests that the fuel usage should be midway between the two point since the load factor is midway

            FuelUsagePointTest(new double[] { 0.1, 0.4, 1, 0, 0 }, new double[] { 0.1, 0.7, 0.3, 0, 0, 0 }, 0.5, 0.7, true);
        }

        [Test]
        public void FuelUsageFlatLine()
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
                string.Format("--iterations {0} --input {1} --output {2} {0} Gen1E Gen1FuelCnt",
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
        public void SheddableLoad()
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
            values["ShedLoadMaxT"] = new double[] { maxOffTime };
            values["ShedLoadT"] = new double[] { offLatency };
            values["Shed1LoadP"] = new double[] { 40 };
            var loadProfile = new double[] { 50, 100, 200, 300, 496, 496, 496, 496, 400 };
            int iterations = (loadProfile.Count() + 1) * period;

            StringBuilder settings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile1, settings.ToString());
            settings = BuildCsvFor("LoadP", loadProfile, period);
            File.WriteAllText(settingsFile2, settings.ToString());

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --input {1} --input {2} --output {3} Gen1P ShedLoadP GenSpinP ShedP Gen1LoadFact",
                    iterations, settingsFile1, settingsFile2, outFile));

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);
            var fileArray = CsvFileToArray(outFile);
            var gen1P = fileArray.Select(col => col[1]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();
            var shedLoadP = fileArray.Select(col => col[2]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();
            var geSpinP = fileArray.Select(col => col[3]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();
            var shedP = fileArray.Select(col => col[4]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();
            var loadFact = fileArray.Select(col => col[5]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();

            // at 61s: load 50 - shedLoad 40 = 10
            Assert.IsTrue(DoublesAreEqual(10, gen1P.ElementAt(61)));
            // at 65s: load 50 (shedLoad on)
            Assert.IsTrue(DoublesAreEqual(50, gen1P.ElementAt(65)));
            // at 10m: load 100
            Assert.IsTrue(DoublesAreEqual(100, gen1P.ElementAt(10 * 60)));
            // at 20m: load 200
            Assert.IsTrue(DoublesAreEqual(200, gen1P.ElementAt(20 * 60)));
            // at 30m: load 300
            Assert.IsTrue(DoublesAreEqual(300, gen1P.ElementAt(30 * 60)));
            // at 40m: load 496
            Assert.IsTrue(DoublesAreEqual(496, gen1P.ElementAt(40 * 60)));

            // Load Factor > 99% so the sheddable load
            // should shut off automatically
            // at 40m+latency: load 496 - 40 = 456
            Assert.IsTrue(DoublesAreEqual(456, gen1P.ElementAt(40 * 60 + offLatency + 5)));

            // at 50m: load 496 - 40 = 456
            Assert.IsTrue(DoublesAreEqual(456, gen1P.ElementAt(50 * 60)));
            // at 60m: load 496 - 40 = 456
            Assert.IsTrue(DoublesAreEqual(456, gen1P.ElementAt(60 * 60)));

            // this tests that the sheddable load turns on when the min run
            // time has expired, regardless of load factor.  This is twenty
            // minutes after it turned off (at 40m).
            // at 60m+5s: load 496
            Assert.IsTrue(DoublesAreEqual(496, gen1P.ElementAt(60 * 60 + offLatency + 5)));

            // at 70m: load 400
            Assert.IsTrue(DoublesAreEqual(400, gen1P.ElementAt(80 * 60)));
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
                string.Format("--iterations {0} --input {1} --input {2} --output {3} GenP LoadP LoadCapAl",
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

        [Test]
        public void GeneratorIdealLoading()
        {
            // Arrange
            var settingsFile1 = GetTempFilename;
            var settingsFile2 = GetTempFilename;
            var outFile = GetTempFilename;
            const int period = 10 * 60;

            var values = new SortedDictionary<string, double[]>();
            InsertFuelConsumption(values, 0.33, 4);
            values["Gen1MaxP"] = new double[] { 500 };
            values["Gen2MaxP"] = new double[] { 500 };
            values["Gen3MaxP"] = new double[] { 500 };
            values["Gen4MaxP"] = new double[] { 500 };
            values["GenAvailSet"] = new double[] { 15 };
            values["GenConfig1"] = new double[] { 15 };
            values["Gen1IdealPctP"] = new double[] { 40 };
            values["Gen2IdealPctP"] = new double[] { 40 };
            values["Gen3IdealPctP"] = new double[] { 40 };
            values["Gen4IdealPctP"] = new double[] { 40 };
            values["PvAvailP"] = new double[] { 3000 };
            var loadProfile = new double[] { 500, 500, 800, 800, 1000, 1000, 1500, 1500, 2000, 3000 };
            int iterations = (loadProfile.Count() + 1) * period;

            StringBuilder settings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile1, settings.ToString());
            settings = BuildCsvFor("LoadP", loadProfile, period);
            File.WriteAllText(settingsFile2, settings.ToString());

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --input {1} --input {2} --output {3} LoadP GenP PvP",
                    iterations, settingsFile1, settingsFile2, outFile));

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);
            var fileArray = CsvFileToArray(outFile);
            var loadP = fileArray.Select(col => col[1]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();
            var genP = fileArray.Select(col => col[2]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();
            var pvP = fileArray.Select(col => col[3]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();

            genP.Where((s, i) => i > 1200).ToList().ForEach(v => Assert.IsTrue(v >= 800));
        }

        [Test]
        public void MaintainSpinTrue()
        {
            MaintainSpinTest(true);
        }

        [Test]
        public void MaintainSpinFalse()
        {
            MaintainSpinTest(false);
        }

        [Test]
        public void IgnoreMsTimes()
        {
            // Arrange
            const string msFile = "t,LoadP\r\n2012-10-24T20:54:30.299,393\r\n2012-10-24T20:54:30.399,367\r\n2012-10-24T20:54:30.499,369\r\n2012-10-24T20:54:35.000,5";
            var outFile = GetTempFilename;
            int iterations = 1000;

            var settingsFile1 = GetTempFilename;
            File.WriteAllText(settingsFile1, msFile);

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {1} --input {0} --starttime 2012-10-24T20:54:30 --output {2} 1 LoadP",
                settingsFile1, iterations, outFile));

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);

            var output = CsvFileToArray(outFile);
            string sloadP = output.Select(col => col[1]).Last();
            double loadP = Convert.ToDouble(sloadP);
            // LoadP should end up at the latest input of 5, not get stuck on the first one.
            Assert.IsTrue(DoublesAreEqual(5D, loadP));
        }

        [Test]
        public void StatisticsSelection()
        {
            // Arrange
            var outFile = GetTempFilename;
            const string stats = "LoadP{Min} Gen[1-3]P{MinT} Gen[1-3]StartCnt{MAx} Gen[1-3]StopCnt{MaxT} " +
                "Gen[1-3]E PvP{Max} GenSpinP{MaxT} Gen[1-3]LoadFact{All} ShedLoadP ShedP{Act} GenCfgSetP StatSpinP{Min,Max,Ave}";
            var resultHeaders = new List<string>()
                {
                    "t",
                    "LoadP_min", "Gen1P_minT", "Gen2P_minT", "Gen3P_minT", "Gen1StartCnt", "Gen2StartCnt", "Gen3StartCnt",
                    "Gen1StopCnt", "Gen2StopCnt", "Gen3StopCnt", "Gen1E", "Gen2E", "Gen3E", "PvP_max", "GenSpinP_maxT",
                    "Gen1LoadFact_min", "Gen1LoadFact_max", "Gen1LoadFact_minT", "Gen1LoadFact_maxT", "Gen1LoadFact_ave",
                    "Gen2LoadFact_min", "Gen2LoadFact_max", "Gen2LoadFact_minT", "Gen2LoadFact_maxT", "Gen2LoadFact_ave",
                    "Gen3LoadFact_min", "Gen3LoadFact_max", "Gen3LoadFact_minT", "Gen3LoadFact_maxT", "Gen3LoadFact_ave",
                    "ShedLoadP_min", "ShedLoadP_max", "ShedLoadP_minT", "ShedLoadP_maxT", "ShedLoadP_ave",
                    "ShedP",
                    "GenCfgSetP_min", "GenCfgSetP_max", "GenCfgSetP_minT", "GenCfgSetP_maxT", "GenCfgSetP_ave",
                    "StatSpinP_min", "StatSpinP_max", "StatSpinP_ave"
                };

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --output {1} {0} {2}",
                    86400, outFile, stats));

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);

            var headerRow = CsvFileToArray(outFile)[0].ToList();
            foreach (var h in resultHeaders)
            {
                Console.WriteLine("testing " + h);
                Assert.IsTrue(headerRow.Remove(h));
            }
            Assert.AreEqual(headerRow.Count, 0);
        }

        protected void MaintainSpinTest(bool maintainSpin)
        {
            // Arrange
            var settingsFile1 = GetTempFilename;
            var settingsFile2 = GetTempFilename;
            var outFile = GetTempFilename;
            const int period = 10 * 60;

            var values = new SortedDictionary<string, double[]>();
            InsertFuelConsumption(values, 0.33, 4);
            values["Gen1MaxP"] = new double[] { 500 };
            values["Gen1IdealPctP"] = new double[] { 0 };
            values["GenAvailSet"] = new double[] { 1 };
            values["GenConfig1"] = new double[] { 1 };
            values["StatMaintainSpin"] = new double[] { maintainSpin ? 1 : 0 };
            values["StatSpinSetP"] = new double[] { 50 };
            values["LoadP"] = new double[] { 200 };
            var shedPprofile = new double[] { 0, 10, 20, 30, 40, 50 };
            int iterations = (shedPprofile.Count() + 1) * period;

            StringBuilder settings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile1, settings.ToString());
            settings = BuildCsvFor("Shed1LoadP", shedPprofile, period);
            File.WriteAllText(settingsFile2, settings.ToString());

            // Act
            int retValue = StartConsoleApplication(
                string.Format("--iterations {0} --input {1} --input {2} --output {3} LoadP Gen1P GenCfgSetP",
                    iterations, settingsFile1, settingsFile2, outFile));

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);
            var fileArray = CsvFileToArray(outFile);

            var genCfgSetP = fileArray.Select(col => col[3]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();
            if (maintainSpin)
            {
                genCfgSetP.Where((s, i) => i > 65).ToList().ForEach(v => Assert.IsTrue(DoublesAreEqual(v, 250)));
            }
            else
            {
                Assert.IsTrue(DoublesAreEqual(genCfgSetP[65], 250));
                Assert.IsTrue(DoublesAreEqual(genCfgSetP[605], 240));
                Assert.IsTrue(DoublesAreEqual(genCfgSetP[1205], 230));
                Assert.IsTrue(DoublesAreEqual(genCfgSetP[1805], 220));
                Assert.IsTrue(DoublesAreEqual(genCfgSetP[2405], 210));
                Assert.IsTrue(DoublesAreEqual(genCfgSetP[3005], 200));
            }
        }
    }
}
