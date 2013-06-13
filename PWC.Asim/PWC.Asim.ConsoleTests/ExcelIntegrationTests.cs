using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace ConsoleTests
{
    public class ExcelIntegrationTests : TestBase
    {
        [Test]
        public void GenericOptions()
        {
            // Arrange
            // (all in [TestSetup])

            // Act
            int retValue = StartExcelApplication(@"--attach --input ..\..\test.xlsx");

            // Assert
            Assert.AreEqual(0, retValue);
            AssertGeneratedFiles(new List<string>
                {
                    "86400 analyse.csv",
                    "test_FuelEfficiency.csv",
                    "test_GenConfigurations.csv",
                    "test_GenStats.csv",
                    "test_Solar.csv",
                    "test_StationStats.csv"
                });
            Assert.IsTrue(CompareFiles("86400 analyse.csv", _analyse));
            Assert.IsTrue(CompareFiles("test_FuelEfficiency.csv", _fuelEfficiency));
            Assert.IsTrue(CompareFiles("test_GenConfigurations.csv", _genConfiguration));
            Assert.IsTrue(CompareFiles("test_GenStats.csv", _genStats));
            Assert.IsTrue(CompareFiles("test_Solar.csv", _solar));
            Assert.IsTrue(CompareFiles("test_StationStats.csv", _stationStats));
        }

        [Test]
        public void PostProcessing()
        {
            // Arrange
            // (all in [TestSetup])

            // Act
            int retValue = StartExcelApplication(@"--attach --input ..\..\testbatch.xlsx");

            // Assert
            Assert.AreEqual(0, retValue);
            string contents = File.ReadAllText(TestDir + "\\test.log");
            Assert.AreEqual(_batchLog, contents);
        }

        private bool CompareFiles(string filename, List<string[]> array)
        {
            var file = CsvFileToArray(TestDir + "\\" + filename);
            if (array.Count != file.Count)
                return false;

            for (int row = 0; row < array.Count; row++)
            {
                if (array[row].Count() != file[row].Count())
                    return false;
                for (int col = 0; col < array[row].Count(); col++)
                {
                    if (array[row][col] != file[row][col])
                        return false;
                }
            }
            return true;
        }

        private void AssertGeneratedFiles(List<string> expectFiles)
        {
            List<string> testFiles = new List<string>();
            Directory.EnumerateFiles(TestDir).ToList().ForEach(f => testFiles.Add(f.Replace(TestDir + "\\", "")));
            var missingExpected = expectFiles.Except(testFiles);
            Assert.IsEmpty(missingExpected);
        }

        [SetUp]
        public void TestSetup()
        {
            ClearTemp();
            Directory.CreateDirectory(TestDir);
            CopyInputFiles();
            Thread.Sleep(500); // wait for directory to be created or the explorer may crash
        }

        [TearDown]
        public void TestTearDown()
        {
            ClearTemp();
        }

        private void ClearTemp()
        {
            try
            {
                Directory.Delete(TestDir, true);
                Thread.Sleep(500); // wait for directory to be deleted or the explorer may crash
            }
            catch { }
        }

        public void CopyInputFiles()
        {
            foreach (var file in Directory.GetFiles(@"..\..\inputfiles"))
                File.Copy(file, Path.Combine(TestDir, Path.GetFileName(file)));
        }

        private readonly List<string[]> _analyse = new List<string[]>()
            {
                new[] {"t", "Gen1P_min", "Gen1P_max", "Gen2P_min", "Gen2P_max", "Gen3P_min", "Gen3P_max", "Gen4P_min", "Gen4P_max"},
                new[] {"2012-10-14 14:25:00", "0", "0", "0", "0", "0", "0", "0", "0"},
                new[] {"2012-10-15 14:24:59", "0", "0", "0", "0", "0", "0", "0", "243.75"}
            };

        private readonly List<string[]> _fuelEfficiency = new List<string[]>()
            {
                new[] {"t", "Gen1FuelCons1P", "Gen1FuelCons1L", "Gen1FuelCons2P", "Gen1FuelCons2L", "Gen1FuelCons3P", "Gen1FuelCons3L", "Gen1FuelCons4P", "Gen1FuelCons4L", "Gen1FuelCons5P", "Gen1FuelCons5L", "Gen2FuelCons1P", "Gen2FuelCons1L", "Gen2FuelCons2P", "Gen2FuelCons2L", "Gen2FuelCons3P", "Gen2FuelCons3L", "Gen2FuelCons4P", "Gen2FuelCons4L", "Gen2FuelCons5P", "Gen2FuelCons5L", "Gen3FuelCons1P", "Gen3FuelCons1L", "Gen3FuelCons2P", "Gen3FuelCons2L", "Gen3FuelCons3P", "Gen3FuelCons3L", "Gen3FuelCons4P", "Gen3FuelCons4L", "Gen3FuelCons5P", "Gen3FuelCons5L", "Gen4FuelCons1P", "Gen4FuelCons1L", "Gen4FuelCons2P", "Gen4FuelCons2L", "Gen4FuelCons3P", "Gen4FuelCons3L", "Gen4FuelCons4P", "Gen4FuelCons4L", "Gen4FuelCons5P", "Gen4FuelCons5L", "Gen5FuelCons1P", "Gen5FuelCons1L", "Gen5FuelCons2P", "Gen5FuelCons2L", "Gen5FuelCons3P", "Gen5FuelCons3L", "Gen5FuelCons4P", "Gen5FuelCons4L", "Gen5FuelCons5P", "Gen5FuelCons5L", "Gen6FuelCons1P", "Gen6FuelCons1L", "Gen6FuelCons2P", "Gen6FuelCons2L", "Gen6FuelCons3P", "Gen6FuelCons3L", "Gen6FuelCons4P", "Gen6FuelCons4L", "Gen6FuelCons5P", "Gen6FuelCons5L", "Gen7FuelCons1P", "Gen7FuelCons1L", "Gen7FuelCons2P", "Gen7FuelCons2L", "Gen7FuelCons3P", "Gen7FuelCons3L", "Gen7FuelCons4P", "Gen7FuelCons4L", "Gen7FuelCons5P", "Gen7FuelCons5L", "Gen8FuelCons1P", "Gen8FuelCons1L", "Gen8FuelCons2P", "Gen8FuelCons2L", "Gen8FuelCons3P", "Gen8FuelCons3L", "Gen8FuelCons4P", "Gen8FuelCons4L", "Gen8FuelCons5P", "Gen8FuelCons5L"},
                new[] {"0", "0", "0.33", "1", "0.33", "0", "0", "0", "0", "0", "0", "0", "0.33", "1", "0.33", "0", "0", "0", "0", "0", "0", "0", "0.33", "1", "0.33", "0", "0", "0", "0", "0", "0", "0", "0.33", "1", "0.33", "0", "0", "0", "0", "0", "0", "0", "0.33", "1", "0.33", "0", "0", "0", "0", "0", "0", "0", "0.33", "1", "0.33", "0", "0", "0", "0", "0", "0", "0", "0.33", "1", "0.33", "0", "0", "0", "0", "0", "0", "0", "0.33", "1", "0.33", "0", "0", "0", "0", "0", "0"}
            };

        private readonly List<string[]> _genConfiguration = new List<string[]>()
            {
                new[] {"t", "GenConfig1", "GenConfig2", "GenConfig3", "GenConfig4", "GenConfig5", "GenConfig6", "GenConfig7", "GenConfig8", "GenConfig9", "GenConfig10", "GenConfig11", "GenConfig12", "GenConfig13", "GenConfig14", "GenConfig15", "GenConfig16", "GenConfig17", "GenConfig18", "GenConfig19"},
                new[] {"0", "8", "4", "2", "1", "3", "5", "6", "9", "10", "12", "18", "48", "7", "11", "97", "99", "103", "111", "127"}
            };

        private readonly List<string[]> _genStats = new List<string[]>()
            {
                new[] {"t", "Gen1MaxP", "Gen2MaxP", "Gen3MaxP", "Gen4MaxP", "Gen5MaxP", "Gen6MaxP", "Gen7MaxP", "Gen8MaxP", "GenAvailSet", "GenBlackCfg", "GenMinRunTPa", "Gen1MinRunTPa", "Gen2MinRunTPa", "Gen3MinRunTPa", "Gen4MinRunTPa", "Gen5MinRunTPa", "Gen6MinRunTPa", "Gen7MinRunTPa", "Gen8MinRunTPa", "Gen1IdealPctP", "Gen2IdealPctP", "Gen3IdealPctP", "Gen4IdealPctP", "Gen5IdealPctP", "Gen6IdealPctP", "Gen7IdealPctP", "Gen8IdealPctP"},
                new[] {"0", "80", "100", "100", "400", "100", "100", "100", "500", "255", "240", "3600", "0", "0", "0", "0", "0", "0", "0", "0", "40", "40", "40", "40", "40", "40", "40", "40"}
            };

        private readonly List<string[]> _solar = new List<string[]>()
            {
                new[] {"t", "PvSetMaxDownP", "PvSetMaxUpP"},
                new[] {"0", "5", "5"}
            };

        private readonly List<string[]> _stationStats = new List<string[]>()
            {
                new[] {"t", "StatHystP", "StatSpinSetP", "LoadCapMargin"},
                new[] {"0", "30", "50", "1"}
            };

        private readonly string _batchLog =
            " " + Environment.NewLine +
            "ASIM environment variables: " + Environment.NewLine +
            " " + Environment.NewLine +
            "ASIM_COMMUNITYNAME=" + Environment.NewLine +
            "ASIM_DIRECTORY=tmp" + Environment.NewLine +
            @"ASIM_EXCELFILE=..\..\testbatch.xlsx" + Environment.NewLine +
            "ASIM_INPUTFILES=load.csv" + Environment.NewLine +
            "ASIM_ITERATIONS=86400" + Environment.NewLine +
            "ASIM_OUTPUTFILES=" + Environment.NewLine +
            "ASIM_STARTTIME=2012-10-14 14:25:00" + Environment.NewLine;
    }
}
