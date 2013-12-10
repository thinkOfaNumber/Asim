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
using PWC.Asim.Core.Actors;
using PWC.Asim.Core.Contracts;
using PWC.Asim.Core.Utils;

namespace ConsoleTests
{
    class BatteryTests : TestBase
    {
        [SetUp]
        public void Cleanup()
        {
            CoreCleanup();
        }

        [Test]
        public void OverChargeTest()
        {
            // Arrange
            var sharedVars = SharedContainer.Instance;
            var battSetP = sharedVars.GetOrNew("BattSetP");
            var battMinP = sharedVars.GetOrNew("BattMinP");
            var battMaxP = sharedVars.GetOrNew("BattMaxP");
            var battRatedE = sharedVars.GetOrNew("BattRatedE");
            var battE = sharedVars.GetOrNew("BattE");
            var battImportedE = sharedVars.GetOrNew("BattImportedE");
            var battEfficiencyPct = sharedVars.GetOrNew("BattEfficiencyPct");
            var battSt = sharedVars.GetOrNew("BattSt");
            var genSpinP = sharedVars.GetOrNew("GenSpinP");

            battMinP.Val = -10;
            battMaxP.Val = 10;
            battSetP.Val = -1;
            battRatedE.Val = 50;
            battEfficiencyPct.Val = 100;
            genSpinP.Val = 50;
            IActor battery = new Battery();

            // Act
            battery.Init();
            for (ulong i = 0; i <= 55 * Settings.SecondsInAnHour + 1; i++)
            {
                battery.Run(i);
            }
            battery.Finish();

            // Assert
            Assert.IsTrue(DoublesAreEqual(battRatedE.Val, battImportedE.Val, 0.001));
            Assert.IsTrue(DoublesAreEqual(battRatedE.Val, battE.Val, 0.001));
            Assert.AreEqual(Convert.ToInt32(BatteryState.CanDischarge), Convert.ToInt32(battSt.Val));
        }

        [Test]
        public void DischargeNonNegative()
        {
            // Arrange
            var sharedVars = SharedContainer.Instance;
            var battSetP = sharedVars.GetOrNew("BattSetP");
            var battMinP = sharedVars.GetOrNew("BattMinP");
            var battMaxP = sharedVars.GetOrNew("BattMaxP");
            var battMaxE = sharedVars.GetOrNew("BattMaxE");
            var battE = sharedVars.GetOrNew("BattE");
            var battEfficiencyPct = sharedVars.GetOrNew("BattEfficiencyPct");

            List<double> eTrend = new List<double>();

            battMinP.Val = -10;
            battMaxP.Val = 10;
            battSetP.Val = -1;
            battMaxE.Val = 50;
            battEfficiencyPct.Val = 100;
            IActor battery = new Battery();

            // Act
            battery.Init();
            // charge for 10 hours
            for (ulong i = 0; i <= 10 * Settings.SecondsInAnHour + 1; i++)
            {
                battery.Run(i);
            }

            // discharge for 20 hours
            battSetP.Val = 1;
            for (ulong i = 0; i <= 20 * Settings.SecondsInAnHour + 1; i++)
            {
                battery.Run(i);
                eTrend.Add(battE.Val);
            }
            battery.Finish();

            // Assert
            eTrend.ForEach(e=> Assert.LessOrEqual(0, e));
        }

        [Test]
        public void EffeciencyTest()
        {
            // Arrange
            var sharedVars = SharedContainer.Instance;
            var battSetP = sharedVars.GetOrNew("BattSetP");
            var battMinP = sharedVars.GetOrNew("BattMinP");
            var battMaxP = sharedVars.GetOrNew("BattMaxP");
            var battRatedE = sharedVars.GetOrNew("BattRatedE");
            var battE = sharedVars.GetOrNew("BattE");
            var battEfficiencyPct = sharedVars.GetOrNew("BattEfficiencyPct");
            var genSpinP = sharedVars.GetOrNew("GenSpinP");

            battMinP.Val = -10;
            battMaxP.Val = 10;
            battSetP.Val = -1;
            battRatedE.Val = 50;
            battEfficiencyPct.Val = 75;
            genSpinP.Val = 50;
            IActor battery = new Battery();

            // Act
            battery.Init();
            for (ulong i = 0; i <= 50 * Settings.SecondsInAnHour + 1; i++)
            {
                battery.Run(i);
            }
            battery.Finish();

            // Assert
            Assert.IsTrue(DoublesAreEqual(50 * 0.75, battE.Val, 0.001));
        }

        [Test]
        public void BatteryIntegration()
        {
            // Arrange
            var settingsFile1 = GetTempFilename;
            var settingsFile2 = GetTempFilename;
            var outFile = GetTempFilename; // @"C:\Users\iain.buchanan\workProjects\pwc.asim.git\batt-test-tmp\output.csv";
            const int period = Settings.SecondsInAnHour;

            const double battMaxE = 35;
            const double battMinE = 10;
            const double battRatedE = 50;
            const double statSpinSet = 0;
            const double pvBattExtraP = 0;

            var values = new SortedDictionary<string, double[]>();
            InsertFuelConsumption(values, 0.33, 4);
            values["BattRatedE"] = new double[] { battRatedE }; // battery rating kWh
            values["BattE"] = new double[] { 30 }; // starting charge kWh
            values["BattMinP"] = new double[] { -10 }; // max charge rate kW
            values["BattMaxP"] = new double[] { 10 }; // max discharge rate kW
            values["BattMinE"] = new double[] { battMinE }; // need diesels (depleted)
            values["BattMaxE"] = new double[] { battMaxE }; // can sustain station (Diesel off)
            values["BattEfficiencyPct"] = new double[] { 100 };
            values["BattRechargeSetP"] = new double[] { 5 }; // min recharge rate when diesel on
            values["Gen1MaxP"] = new double[] { 100 };
            values["GenConfig1"] = new double[] { 0 }; // allow diesel off mode
            values["GenConfig2"] = new double[] { 1 };
            values["GenAvailSet"] = new double[] { 1 };
            values["GenBlackCfg"] = new double[] { 1 };
            values["Gen1FuelCons1P"] = new double[] { 0 };
            values["Gen1FuelCons1L"] = new double[] { 50 };
            values["Gen1FuelCons2P"] = new double[] { 1 };
            values["Gen1FuelCons2L"] = new double[] { 50 };
            values["Gen1OverLoadT"] = new double[] { 5 };
            values["Gen1OverloadPctP"] = new double[] { 10 };
            values["Gen1UnderloadT"] = new double[] { 5 };
            values["Gen1UnderloadPctP"] = new double[] { 10 };
            // values["GenCfgSetK"] = new double[] { 0.1 };
            values["GenSwitchUpDelayT"] = new double[] { 2 };
            values["GenSwitchDownDelayT"] = new double[] { 2 };
            var loadProfile = new double[] { 50, 40, 30, 20, 10, 10, 10, 20, 20, 30, 10, 20, 20, 20, 20 };
            var pvProfile = new double[]   {  0,  0, 10, 10, 20, 20, 25, 20, 15, 15, 20, 10, 10, 10, 10 };

            ulong iterations = (ulong)(loadProfile.Count() + 1) * period;

            StringBuilder settings = BuildCsvFor(new List<string> { "LoadP", "PvAvailP" }, new[] { loadProfile, pvProfile }, period);
            File.WriteAllText(settingsFile2, settings.ToString());
            settings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile1, settings.ToString());

            var sim = new Simulator();
            sim.AddInput(settingsFile1);
            sim.AddInput(settingsFile2);
            sim.AddOutput(outFile, new[]
            {
                "BattSetP", "BattP", "BattE", "BattSt",
                "LoadP", "StatSt", "StatBlack",
                "GenP", "GenCfgSetP", "GenOnlineCfg", "GenSetCfg", "Gen1StartCnt", "Gen1StopCnt",
                "PvAvailP", "PvP", "PvSpillP"
            });
            sim.Iterations = iterations;

            // Act
            sim.Simulate();

            // Assert
            var results = CsvFileToColHash(outFile);

            // battery never overcharges
            results["BattE"].ForEach(e => Assert.LessOrEqual(e, battRatedE + Settings.Insignificant));

            int i = 0;
            for (; i < (int)iterations; i++)
            {
                if (results["GenOnlineCfg"][i] > 0) // station online
                    break;
            }
            Assert.AreEqual(61, i); // station is online in 1 minute
            Assert.AreEqual(0, results["StatBlack"][i]); // station online
            Assert.LessOrEqual(results["BattP"][i + 1], 0); // battery starts charging
            Assert.AreEqual(1, results["BattSt"][i]); // battery state can't support discharge (E too low)
            Assert.AreEqual(1, results["StatSt"][i]); // station state is diesel+
            for (; i < (int) iterations; i++)
            {
                if (results["BattE"][i] > battMaxE) // battery charged
                    break;
            }
            Assert.AreEqual(3661, i);
            Assert.AreEqual(0, results["StatBlack"][i]); // station online
            Assert.AreEqual(2, results["BattSt"][i+1]); // battery state can support discharge
            Assert.AreEqual(1, results["StatSt"][i]); // station state is diesel+ for other constraints
            for (; i < (int) iterations; i++)
            {
                // can go into diesel off
                if (results["PvAvailP"][i] > results["LoadP"][i] + statSpinSet + pvBattExtraP)
                    break;
            }
            Assert.AreEqual(14400, i);
            Assert.AreEqual(0, results["StatBlack"][i]); // station online
            Assert.AreEqual(2, results["BattSt"][i]); // battery state can still support discharge
            Assert.AreEqual(2, results["StatSt"][i]); // station state can support diesel off
            Assert.AreEqual(0, results["GenSetCfg"][i + 4]); // diesel off mode
            i++;
            for (; i < (int)iterations; i++)
            {
                // battery fully charged
                if (results["BattE"][i] >= battRatedE)
                    break;
            }
            Assert.AreEqual(14430, i);
            Assert.AreEqual(0, results["StatBlack"][i]); // station online
            Assert.AreEqual(2, results["BattSt"][i]); // battery state can still support discharge
            Assert.AreEqual(2, results["StatSt"][i]); // station state can support diesel off
            Assert.AreEqual(0, results["GenSetCfg"][i + 2]); // diesel off mode
            i+=2;
            for (; i < (int)iterations; i++)
            {
                // load step / blackout
                if (results["LoadP"][i] != results["BattP"][i] + results["PvP"][i])
                    break;
            }
            Assert.AreEqual(32400, i);
            Assert.AreEqual(1, results["StatBlack"][i + 1]); // station offline
            Assert.AreEqual(1, results["BattSt"][i + 2]); // battery state reset
            Assert.AreEqual(1, results["StatSt"][i + 2]); // station state reset
            Assert.AreEqual(1, results["GenSetCfg"][i]); // diesel on
            for (; i < (int)iterations; i++)
            {
                // can go into diesel off again
                if (results["PvAvailP"][i] > results["LoadP"][i] + statSpinSet + pvBattExtraP)
                    break;
            }
            Assert.AreEqual(36000, i);
            Assert.AreEqual(0, results["StatBlack"][i]); // station online
            Assert.AreEqual(2, results["BattSt"][i]); // battery state can still support discharge
            Assert.AreEqual(2, results["StatSt"][i]); // station state can support diesel off
            Assert.AreEqual(0, results["GenSetCfg"][i + 4]); // diesel off mode
            for (; i < (int)iterations; i++)
            {
                // battery + solar take load
                if (results["BattP"][i] > 0)
                    break;
            }
            Assert.AreEqual(39600, i);
            Assert.AreEqual(0, results["StatBlack"][i]); // station online
            Assert.AreEqual(2, results["BattSt"][i]); // battery state can still support discharge
            Assert.AreEqual(2, results["StatSt"][i]); // station state can support diesel off
            Assert.AreEqual(0, results["GenSetCfg"][i + 4]); // diesel off mode
            for (; i < (int)iterations; i++)
            {
                // battery depleted, request generation
                if (results["BattE"][i] < battMinE)
                    break;
            }
            i++; // double precision means we get here a bit sooner
            Assert.AreEqual(54000, i);
            Assert.AreEqual(0, results["StatBlack"][i]); // station online
            Assert.AreEqual(1, results["BattSt"][i]); // battery state can't support load
            Assert.AreEqual(3, results["StatSt"][i+1]); // station state request diesel
            Assert.AreEqual(1, results["GenSetCfg"][i + 1]); // diesel on mode
            for (; i < (int)iterations; i++)
            {
                // generation online
                if (results["GenOnlineCfg"][i] > 0)
                    break;
            }
            Assert.AreEqual(54061, i);
            Assert.AreEqual(0, results["StatBlack"][i]); // station online
            Assert.AreEqual(1, results["BattSt"][i]); // battery state can't support load
            Assert.AreEqual(1, results["StatSt"][i + 1]); // station state diesel+
            Assert.AreEqual(1, results["GenSetCfg"][i + 1]); // diesel on mode
        }

        [Test]
        public void RechargeOverloadTest()
        {
            // test that a high BattRechargeSetP doesn't overload generators that are near-limit
        }

        private void SetVars(Dictionary<string, double> toSet)
        {
            var sharedVars = SharedContainer.Instance;
            toSet.ToList().ForEach(nv =>
            {
                var s = sharedVars.GetOrNew(nv.Key);
                s.Val = nv.Value;
            });
        }
    }
}
