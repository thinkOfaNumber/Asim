using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            for (ulong i = 0; i <= 50 * 3600 + 1; i++)
            {
                if (i == 50 * 3600)
                    Debugger.Break();
                battery.Run(i);
            }
            battery.Finish();

            // Assert
            Assert.IsTrue(DoublesAreEqual(50, battImportedE.Val, 0.001));
            Assert.IsTrue(DoublesAreEqual(50, battE.Val, 0.001));
            Assert.AreEqual(Convert.ToInt32(BatteryState.CanDischarge), Convert.ToInt32(battSt.Val));
        }

    }
}
