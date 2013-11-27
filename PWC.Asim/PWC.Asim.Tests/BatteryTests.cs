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
        public void ChargeTest()
        {
            // Arrange
            var sharedVars = SharedContainer.Instance;
            var battSetP = sharedVars.GetOrNew("BattSetP");
            var battMinP = sharedVars.GetOrNew("BattMinP");
            var battMaxP = sharedVars.GetOrNew("BattMaxP");
            var battMaxE = sharedVars.GetOrNew("BattMaxE");
            var battE = sharedVars.GetOrNew("BattE");
            var battImportedE = sharedVars.GetOrNew("BattImportedE");
            var battEfficiencyPct = sharedVars.GetOrNew("BattEfficiencyPct");
            var battSt = sharedVars.GetOrNew("BattSt");

            battMinP.Val = -10;
            battMaxP.Val = 10;
            battSetP.Val = -1;
            battMaxE.Val = 50;
            battEfficiencyPct.Val = 100;
            IActor battery = new Battery();

            // Act
            battery.Init();
            for (ulong i = 0; i <= 55 * Settings.SecondsInAnHour + 1; i++)
            {
                battery.Run(i);
            }
            battery.Finish();

            // Assert
            Assert.IsTrue(DoublesAreEqual(50, battImportedE.Val, 0.001));
            Assert.IsTrue(DoublesAreEqual(50, battE.Val, 0.001));
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
            var battMaxE = sharedVars.GetOrNew("BattMaxE");
            var battE = sharedVars.GetOrNew("BattE");
            var battEfficiencyPct = sharedVars.GetOrNew("BattEfficiencyPct");

            battMinP.Val = -10;
            battMaxP.Val = 10;
            battSetP.Val = -1;
            battMaxE.Val = 50;
            battEfficiencyPct.Val = 75;
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
            var outFile = GetTempFilename;
            const int period = Settings.SecondsInAnHour;

            var values = new SortedDictionary<string, double[]>();
            InsertFuelConsumption(values, 0.33, 4);
            values["BattMinP"] = new double[] { -10 };
            values["BattMaxP"] = new double[] { 10 };
            values["BattMaxE"] = new double[] { 50 };
            values["BattEfficiencyPct"] = new double[] { 100 };
            values["BattRechargeSetP"] = new double[] { 20 };
            values["Gen1MaxP"] = new double[] { 100 };
            values["GenConfig1"] = new double[] { 1 };
            values["GenAvailSet"] = new double[] { 1 };
            values["GenBlackCfg"] = new double[] { 1 };
            values["Gen1FuelCons1P"] = new double[] { 0 };
            values["Gen1FuelCons1L"] = new double[] { 50 };
            values["Gen1FuelCons2P"] = new double[] { 1 };
            values["Gen1FuelCons2L"] = new double[] { 50 };
            var loadProfile = new double[] { 50, 40, 30, 20, 10, 10, 10, 20, 20, 30, 30, 40, 40, 50 };
            var pvProfile = new double[]   {  0,  0, 10, 20, 30, 40, 50, 60, 50, 40, 30, 20, 10,  0 };
            ulong iterations = (ulong)(loadProfile.Count() + 1) * period;

            StringBuilder settings = BuildCsvFor(new List<string> { "LoadP", "PvAvailP" }, new[] { loadProfile, pvProfile }, period);
            File.WriteAllText(settingsFile2, settings.ToString());
            settings = BuildCsvFor(values.Keys.ToList(), values.Values.ToArray());
            File.WriteAllText(settingsFile1, settings.ToString());

            var sim = new Simulator();
            sim.AddInput(settingsFile1);
            sim.AddInput(settingsFile2);
            sim.AddOutput(outFile, new[] { "BattSetP", "BattP", "BattE", "BattIMportedE", "BattSt", "LoadP", "GenP", "PvP" });
            sim.Iterations = iterations;

            // Act
            sim.Simulate();

            // Assert
            var results = CsvFileToColHash(outFile);


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
