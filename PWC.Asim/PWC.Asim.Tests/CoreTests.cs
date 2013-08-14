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
            // Reset static values to their default (0)
            var sharedVars = SharedContainer.Instance;
            var vars = sharedVars.GetAllNames();
            foreach (string n in vars)
            {
                sharedVars.GetExisting(n).Val = 0;
            }
        }

        [Test]
        public void SingleServiceCounter()
        {
            // Arrange
            var settingsFile = GetTempFilename;
            int nServices = 5;
            int serviceInterval = 8; // hours
            int serviceOutage = 2; // hours
            int iterations = (nServices*serviceInterval + nServices*serviceOutage + 1)*60*60;
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

            StringBuilder fuelsettings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile, fuelsettings.ToString());

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
            int runTime = serviceInterval1*serviceInterval2;
            int iterations = (runTime + serviceOutage1*runTime/serviceInterval1 + serviceOutage2*runTime/serviceInterval2 + 1) * 60 * 60;
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

            StringBuilder fuelsettings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile, fuelsettings.ToString());

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
            var loadPexpect = new List<double>() { 0, 5, 10, 15, 20,  25,  30,  32, 22 };
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
            ConfigDownDelay(6*60);
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
    }
}
