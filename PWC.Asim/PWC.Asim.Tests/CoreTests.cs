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
using PWC.Asim.Core.Contracts;
using PWC.Asim.Core.Utils;

namespace ConsoleTests
{
    class CoreTests : TestBase
    {
        [SetUp]
        public void Cleanup()
        {
            CoreCleanup();
        }

        [Test]
        public void SingleServiceCounter()
        {
            // Arrange
            var settingsFile = GetTempFilename;
            int nServices = 5;
            int serviceInterval = 8; // hours
            int serviceOutage = 2; // hours
            int iterations = (nServices * serviceInterval + nServices * serviceOutage + 1) * 60 * 60;
            var outFile = GetTempFilename;

            var values = new SortedDictionary<string, double[]>();
            InsertFuelConsumption(values, 0.33);
            values["Gen1MaxP"] = new double[] { 100 };
            values["Gen1Service1T"] = new double[] { serviceInterval };
            values["Gen1Service1OutT"] = new double[] { serviceOutage };
            values["GenConfig1"] = new double[] { 1 };
            values["GenAvailSet"] = new double[] { 1 };
            values["GenBlackCfg"] = new double[] { 1 };
            values["LoadP"] = new double[] { 50 };

            var sim = new Simulator();
            sim.AddInput(settingsFile);
            sim.AddOutput(outFile, new[] { "Gen1RunCnt", "Gen1ServiceCnt" }, (uint)iterations);
            sim.Iterations = (ulong)iterations;

            StringBuilder simSettings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile, simSettings.ToString());

            // Act
            sim.Simulate();

            // Assert
            var fileArray = CsvFileToArray(outFile);

            var runCnt = Convert.ToDouble(fileArray[2][1]);
            var serviceCnt = Convert.ToInt32(fileArray[2][2]);
            Assert.AreEqual(nServices, serviceCnt);
        }

        [Test]
        public void MultipleServiceCounters()
        {
            // Arrange
            var settingsFile = GetTempFilename;
            int nServices1 = 3;
            int serviceInterval1 = 8; // hours
            int serviceOutage1 = 2; // hours
            int nServices2 = 8;
            int serviceInterval2 = 3; // hours
            int serviceOutage2 = 1; // hours
            int runTime = serviceInterval1 * serviceInterval2;
            int iterations = (runTime + serviceOutage1 * runTime / serviceInterval1 + serviceOutage2 * runTime / serviceInterval2 + 1) * 60 * 60;
            var outFile = GetTempFilename;

            var values = new SortedDictionary<string, double[]>();
            InsertFuelConsumption(values, 0.33);
            values["Gen1MaxP"] = new double[] { 100 };
            values["Gen1Service1T"] = new double[] { serviceInterval1 };
            values["Gen1Service1OutT"] = new double[] { serviceOutage1 };
            values["Gen1Service2T"] = new double[] { serviceInterval2 };
            values["Gen1Service2OutT"] = new double[] { serviceOutage2 };
            values["GenConfig1"] = new double[] { 1 };
            values["GenAvailSet"] = new double[] { 1 };
            values["GenBlackCfg"] = new double[] { 1 };
            values["LoadP"] = new double[] { 50 };

            var sim = new Simulator();
            sim.AddInput(settingsFile);
            sim.AddOutput(outFile, new[] { "Gen1RunCnt", "Gen1ServiceCnt" }, (uint)iterations);
            sim.Iterations = (ulong)iterations;

            StringBuilder simSettings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile, simSettings.ToString());

            // Act
            sim.Simulate();

            // Assert
            var fileArray = CsvFileToArray(outFile);

            var serviceCnt = Convert.ToInt32(fileArray[2][2]);
            // in 24 hours there should be 3 8h services and 8 3h services, minus one overlapping
            Assert.AreEqual(10, serviceCnt);
        }

        [Test]
        public void LoadRateOfChange()
        {
            // Arrange
            var sharedVars = SharedContainer.Instance;
            var loadP = sharedVars.GetOrNew("LoadP");
            var loadSetP = sharedVars.GetOrNew("LoadSetP");
            var loadMaxUpP = sharedVars.GetOrNew("LoadMaxUpP");
            var loadMaxDownP = sharedVars.GetOrNew("LoadMaxDownP");

            loadMaxUpP.Val = 5;
            loadMaxDownP.Val = 10;
            var loadPinputs = new List<double>() { 0, 5, 10, 15, 100, 500, 100, 32, 5 };
            var loadPexpect = new List<double>() { 0, 5, 10, 15, 20, 25, 30, 32, 22 };
            var limitedLoad = new List<double>();

            IActor loadMgr = new PWC.Asim.Core.Actors.Load();

            // Act
            loadMgr.Init();
            for (int it = 0; it < loadPinputs.Count; it++)
            {
                loadP.Val = loadPinputs[it];
                loadMgr.Run((ulong)it);
                limitedLoad.Add(loadSetP.Val);
            }
            loadMgr.Finish();

            // Assert
            int matchingValues = limitedLoad.Where((v, i) => v.Equals(loadPexpect[i])).Count();
            Assert.AreEqual(loadPexpect.Count, matchingValues);
        }

        [Test]
        public void ConfigDownNoDelay()
        {
            ConfigDownDelay(0);
        }

        [Test]
        public void ConfigDownWithDelay()
        {
            ConfigDownDelay(6 * 60);
        }

        private void ConfigDownDelay(long delayTime)
        {
            // Arrange
            var settingsFile1 = GetTempFilename;
            var settingsFile2 = GetTempFilename;
            var outFile = GetTempFilename;
            const int period = 5 * 60;

            var values = new SortedDictionary<string, double[]>();
            InsertFuelConsumption(values, 0.33, 4);
            values["Gen1MaxP"] = new double[] { 500 };
            values["Gen2MaxP"] = new double[] { 500 };
            values["GenAvailSet"] = new double[] { 0xFF };
            values["GenConfig1"] = new double[] { 1 };
            values["GenConfig2"] = new double[] { 2 };
            values["GenConfig3"] = new double[] { 3 };
            values["StatSpinSetP"] = new double[] { 50 };
            values["GenMinRunT"] = new double[] { 600 };
            values["GenSwitchDownDelayT"] = new double[] { delayTime };
            var loadProfile = new double[] { 600, 600, 600, 400, 499 };
            int iterations = (loadProfile.Count() + 1) * period;

            StringBuilder settings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile1, settings.ToString());
            settings = BuildCsvFor("LoadP", loadProfile, period);
            File.WriteAllText(settingsFile2, settings.ToString());

            // Act
            var sim = new Simulator();
            sim.AddInput(settingsFile1);
            sim.AddInput(settingsFile2);
            sim.AddOutput(outFile, new[] { "GenOnlineCfg", "LoadP" });
            sim.Iterations = (ulong)iterations;
            sim.Simulate();

            // Assert
            var fileArray = CsvFileToArray(outFile);

            var genOnlineCfg = fileArray.Select(col => col[1]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();

            // at 0, nothing is online
            Assert.AreEqual(0, genOnlineCfg[0]);

            // at 1min + 1s, both generators on
            Assert.AreEqual(3, genOnlineCfg[61]);

            // just before 15min, both generators on
            Assert.AreEqual(3, genOnlineCfg[15 * 60 - 1]);

            if (delayTime > period)
            {
                // at 15min-20min, both generators on
                Assert.AreEqual(3, genOnlineCfg[15 * 60]);
                Assert.AreEqual(3, genOnlineCfg[16 * 60]);
                Assert.AreEqual(3, genOnlineCfg[17 * 60]);
                Assert.AreEqual(3, genOnlineCfg[18 * 60]);
                Assert.AreEqual(3, genOnlineCfg[19 * 60]);
                Assert.AreEqual(3, genOnlineCfg[20 * 60]);
            }
            else
            {
                // at 15min, only one generator on
                Assert.AreEqual(1, genOnlineCfg[15 * 60]);
            }

            // at 20min +1min (warmup), both generators on again
            Assert.AreEqual(3, genOnlineCfg[21 * 60]);
        }

        [Test]
        public void SheddableLoadLatency()
        {
            // Arrange
            const int iterations = 1000;
            var shedMangaer = new PWC.Asim.Core.Actors.SheddableLoadMgr();
            var sharedVars = SharedContainer.Instance;

            var shedPprofile = new double[iterations];
            sharedVars.GetOrNew("StatBlack").Val = 0;
            // shed latency - 20s
            var s = sharedVars.GetOrNew("ShedLoadT");
            s.Val = 20;
            // max online generator output - 1000kW
            sharedVars.GetOrNew("GenMaxP").Val = 1000;
            // ideal spot to load the generators
            sharedVars.GetOrNew("ShedIdealPct").Val = 50;
            // total sheddable load available
            sharedVars.GetOrNew("ShedLoadP").Val = 500;

            // act generator output
            var genP = sharedVars.GetOrNew("GenP");
            // offline sheddable load
            var shedOffP = sharedVars.GetOrNew("ShedOffP");
            // online sheddable load
            var shedP = sharedVars.GetOrNew("ShedP");
            // energy not produced 
            var shedE = sharedVars.GetOrNew("ShedE");


            // Act
            for (ulong i = 0; i < iterations; i++)
            {
                genP.Val = i;
                shedMangaer.Run(i);
                shedPprofile[i] = shedOffP.Val;
            }

            // Assert
            // for the first 500 + latency seconds, no load shed as LF < 50
            for (int i = 0; i < 521; i++)
            {
                Assert.IsTrue(DoublesAreEqual(shedPprofile[i], 0));
            }

            for (int i = 521; i < 1000; i++)
            {
                Assert.IsTrue(DoublesAreEqual(shedPprofile[i], i - 521));
            }
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
            values["ShedLoadP"] = new double[] { 40 };
            values["ShedIdealPct"] = new double[] { 99 };
            var loadProfile = new double[] { 50, 100, 200, 300, 496, 496, 496, 496, 400 };
            int iterations = (loadProfile.Count() + 1) * period;

            StringBuilder settings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile1, settings.ToString());
            settings = BuildCsvFor("LoadP", loadProfile, period);
            File.WriteAllText(settingsFile2, settings.ToString());


            var sim = new Simulator();
            sim.AddInput(settingsFile1);
            sim.AddInput(settingsFile2);
            sim.AddOutput(outFile, new[] { "Gen1P", "ShedLoadP", "GenSpinP", "ShedP", "Gen1LoadFact" });
            sim.Iterations = (ulong)iterations;

            // Act
            sim.Simulate();

            // Assert
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
            // should proportionally shut off
            // at 40m+latency: load 99% = 495 (+1)
            Assert.IsTrue(DoublesAreEqual(496, gen1P.ElementAt(40 * 60 + offLatency + 5)));

            // at 50m: load 99% = 495 (+1)
            Assert.IsTrue(DoublesAreEqual(496, gen1P.ElementAt(50 * 60 + offLatency + 5)));
            // at 60m: load 99% = 495 (+1)
            Assert.IsTrue(DoublesAreEqual(496, gen1P.ElementAt(60 * 60 + offLatency + 5)));

            // at 70m: load 400
            Assert.IsTrue(DoublesAreEqual(400, gen1P.ElementAt(80 * 60)));
        }

        [Test]
        // tests overload trips at 100 % when overload factor unset
        public void OverloadUnset()
        {
            // Arrange, Act
            dynamic results = OverUnderLoadTest(new double[] { 900, 1000, 1001, 1001 });
            List<double> genP = results.genP;
            List<double> genOnlineCfg = results.genCfg;

            // Assert

            // ok
            Assert.IsTrue(DoublesAreEqual(900, genP.ElementAt(61)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(61)));

            // ok
            Assert.IsTrue(DoublesAreEqual(1000, genP.ElementAt(1199)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(1199)));

            // tripped
            Assert.IsTrue(DoublesAreEqual(0, genP.ElementAt(1200)));
            Assert.IsTrue(DoublesAreEqual(0, genOnlineCfg.ElementAt(1200)));
        }

        [Test]
        // tests overload doesn't trip until at x % for y seconds when overload factor set
        public void OverloadSetTime()
        {
            // Arrange, Act
            dynamic results = OverUnderLoadTest(new double[] { 900, 1000, 1100, 1100 }, 300, 10);
            List<double> genP = results.genP;
            List<double> genOnlineCfg = results.genCfg;

            // Assert

            // ok
            Assert.IsTrue(DoublesAreEqual(900, genP.ElementAt(61)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(61)));

            // ok
            Assert.IsTrue(DoublesAreEqual(1000, genP.ElementAt(1199)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(1199)));

            // ok
            Assert.IsTrue(DoublesAreEqual(1100, genP.ElementAt(1200)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(1200)));

            // ok
            Assert.IsTrue(DoublesAreEqual(1100, genP.ElementAt(1499)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(1499)));

            // tripped
            Assert.IsTrue(DoublesAreEqual(0, genP.ElementAt(1500)));
            Assert.IsTrue(DoublesAreEqual(0, genOnlineCfg.ElementAt(1500)));
        }

        [Test]
        // tests that set time but unset % doesn't enable overload factor
        public void OverloadPartSet()
        {
            // Arrange, Act
            dynamic results = OverUnderLoadTest(new double[] { 900, 1000, 1001, 1001}, 300, 0 );
            List<double> genP = results.genP;
            List<double> genOnlineCfg = results.genCfg;

            // Assert

            // ok
            Assert.IsTrue(DoublesAreEqual(900, genP.ElementAt(61)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(61)));

            // ok
            Assert.IsTrue(DoublesAreEqual(1000, genP.ElementAt(1199)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(1199)));

            // tripped
            Assert.IsTrue(DoublesAreEqual(0, genP.ElementAt(1200)));
            Assert.IsTrue(DoublesAreEqual(0, genOnlineCfg.ElementAt(1200)));
        }

        [Test]
        // tests overload trips over x % when overload factor set
        public void OverloadSetOver()
        {
            // Arrange, Act
            dynamic results = OverUnderLoadTest(new double[] { 900, 1000, 1101, 1101 }, 300, 10);
            List<double> genP = results.genP;
            List<double> genOnlineCfg = results.genCfg;

            // Assert

            // ok
            Assert.IsTrue(DoublesAreEqual(900, genP.ElementAt(61)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(61)));

            // ok
            Assert.IsTrue(DoublesAreEqual(1000, genP.ElementAt(1199)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(1199)));

            // tripped
            Assert.IsTrue(DoublesAreEqual(0, genP.ElementAt(1200)));
            Assert.IsTrue(DoublesAreEqual(0, genOnlineCfg.ElementAt(1200)));
        }

        [Test]
        // tests underload trips at 0 % when underload factor unset
        public void UnderloadUnset()
        {
            // Arrange, Act
            dynamic results = OverUnderLoadTest(new double[] { 10, 0, -1, -1 });
            List<double> genP = results.genP;
            List<double> genOnlineCfg = results.genCfg;

            // Assert

            // ok
            Assert.IsTrue(DoublesAreEqual(10, genP.ElementAt(61)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(61)));

            // ok
            Assert.IsTrue(DoublesAreEqual(0, genP.ElementAt(1199)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(1199)));

            // tripped
            Assert.IsTrue(DoublesAreEqual(0, genP.ElementAt(1200)));
            Assert.IsTrue(DoublesAreEqual(0, genOnlineCfg.ElementAt(1200)));
        }

        [Test]
        // tests underload doesn't trip until at 0-x % for y seconds when underload factor set
        public void UnderloadSetTime()
        {
            // Arrange, Act
            dynamic results = OverUnderLoadTest(new double[] { 10, 0, -100, -100 }, 0, 0, 300, 10);
            List<double> genP = results.genP;
            List<double> genOnlineCfg = results.genCfg;

            // Assert

            // ok
            Assert.IsTrue(DoublesAreEqual(10, genP.ElementAt(61)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(61)));

            // ok
            Assert.IsTrue(DoublesAreEqual(0, genP.ElementAt(1199)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(1199)));

            // ok
            Assert.IsTrue(DoublesAreEqual(-100, genP.ElementAt(1200)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(1200)));

            // ok
            Assert.IsTrue(DoublesAreEqual(-100, genP.ElementAt(1499)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(1499)));

            // tripped
            Assert.IsTrue(DoublesAreEqual(0, genP.ElementAt(1500)));
            Assert.IsTrue(DoublesAreEqual(0, genOnlineCfg.ElementAt(1500)));
        }
        [Test]
        // tests that set time but unset % doesn't enable underload factor
        public void UnderloadPartSet()
        {
            // Arrange, Act
            dynamic results = OverUnderLoadTest(new double[] { 10, 0, -1, -1 }, 0, 0, 300, 0);
            List<double> genP = results.genP;
            List<double> genOnlineCfg = results.genCfg;

            // Assert

            // ok
            Assert.IsTrue(DoublesAreEqual(10, genP.ElementAt(61)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(61)));

            // ok
            Assert.IsTrue(DoublesAreEqual(0, genP.ElementAt(1199)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(1199)));

            // tripped
            Assert.IsTrue(DoublesAreEqual(0, genP.ElementAt(1200)));
            Assert.IsTrue(DoublesAreEqual(0, genOnlineCfg.ElementAt(1200)));
        }

        [Test]
        // tests underload trips under 0-x % when underload factor set
        public void UnderloadSetUnder()
        {
            // Arrange, Act
            dynamic results = OverUnderLoadTest(new double[] { 10, 0, -101, -101 }, 0, 0, 300, 10);
            List<double> genP = results.genP;
            List<double> genOnlineCfg = results.genCfg;

            // Assert

            // ok
            Assert.IsTrue(DoublesAreEqual(10, genP.ElementAt(61)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(61)));

            // ok
            Assert.IsTrue(DoublesAreEqual(0, genP.ElementAt(1199)));
            Assert.IsTrue(DoublesAreEqual(2, genOnlineCfg.ElementAt(1199)));

            // tripped
            Assert.IsTrue(DoublesAreEqual(0, genP.ElementAt(1200)));
            Assert.IsTrue(DoublesAreEqual(0, genOnlineCfg.ElementAt(1200)));
        }

        private object OverUnderLoadTest(
            double[] loadProfile,
            double genOverLoadT = 0,
            double genOverLoadPct = 0,
            double genUnderLoadT = 0,
            double genUnderLoadPct = 0)
        {
            // Arrange
            var settingsFile1 = GetTempFilename;
            var settingsFile2 = GetTempFilename;
            var outFile = GetTempFilename;

            int period = 10 * 60;
            var values = new SortedDictionary<string, double[]>();
            InsertFuelConsumption(values, 0.33, 8);
            values["Gen2MaxP"] = new double[] { 1000 };
            values["GenConfig1"] = new double[] { 0x2 };
            values["GenAvailSet"] = new double[] { 0x2 };
            values["Gen2OverLoadT"] = new [] { genOverLoadT };
            values["Gen2OverloadPctP"] = new [] { genOverLoadPct };
            values["Gen2UnderloadT"] = new [] { genUnderLoadT };
            values["Gen2UnderloadPctP"] = new [] { genUnderLoadPct };
            int iterations = (loadProfile.Count() + 1) * period;

            StringBuilder settings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile1, settings.ToString());
            settings = BuildCsvFor("LoadP", loadProfile, period);
            File.WriteAllText(settingsFile2, settings.ToString());

            var sim = new Simulator();
            sim.AddInput(settingsFile1);
            sim.AddInput(settingsFile2);
            sim.AddOutput(outFile, new[] { "Gen2P", "GenOnlineCfg" });
            sim.Iterations = (ulong)iterations;

            // Act
            sim.Simulate();

            var fileArray = CsvFileToArray(outFile);
            var gen2P = fileArray.Select(col => col[1]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();
            var genOnlineCfg = fileArray.Select(col => col[2]).Where((s, i) => i > 0).Select(Convert.ToDouble).ToList();

            return new {genP = gen2P, genCfg = genOnlineCfg};
        }
    }
}
