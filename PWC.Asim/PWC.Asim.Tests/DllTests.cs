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
    class DllTests : TestBase
    {
        [Test]
        public void PvNone()
        {
            // Arrange

            // Act
            int retValue = StartConsoleApplication(@"--iterations 5000 --Algorithm ..\..\..\Algorithms\PWC.Asim.Algorithms.PvNone\bin\Debug\PWC.SLMS.Algorithms.PvNone.dll");

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);
        }

        [Test]
        public void PvSimple()
        {
            // Arrange

            // Act
            int retValue = StartConsoleApplication(@"--iterations 5000 --Algorithm ..\..\..\Algorithms\PWC.Asim.Algorithms.PvSimple\bin\Debug\PWC.SLMS.Algorithms.PvSimple.dll");

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);
        }

        [Test]
        public void PvFsc()
        {
            // Arrange

            // Act
            int retValue = StartConsoleApplication(@"--iterations 5000 --Algorithm ..\..\..\Algorithms\PWC.Asim.Algorithms.PvFsc\bin\Debug\PWC.SLMS.Algorithms.PvFsc.dll");

            // Assert
            // completed successfully
            Assert.AreEqual(0, retValue);
        }
    }
}
