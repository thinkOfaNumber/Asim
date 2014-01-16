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
    class ScalingTests : TestBase
    {
        private readonly TupleList<double,double> _scalingValues = new TupleList<double,double>
        {
            {1, 0},
            {1.1, 1},
            {1.2, 2},
            {1.3, 3},
            {1.4, 4},
            {1.5, 5},
            {1.6, 6},
            {1.7, 7},
            {1.8, 8},
            {1.9, 9},
        };
        private readonly TupleList<double, double> _negScalingValues = new TupleList<double, double>
        {
            {1, 0},
            {0.9, 0},
            {0.8, 0},
            {0.7, 0},
            {0.6, 0},
            {0.5, 0},
            {0.4, 0},
            {0.3, 0},
            {0.2, 0},
            {0.1, 0},
        };

        [SetUp]
        public void Cleanup()
        {
            CoreCleanup();
        }

        [Test]
        public void ScaleOneVariable()
        {
            // Arrange
            var sharedVars = SharedContainer.Instance;
            var foo = sharedVars.GetOrNew("FooVar1");
            var fooProfile = new List<double>();

            var input = GetTempFilename;
            File.WriteAllLines(input, GenerateFile(_scalingValues, "FooVar1", 10));
            IActor readData = new NextData(input);

            // Act
            foo.Val = 1;
            readData.Init();
            for (ulong i = 0; i < 100; i++)
            {
                readData.Run(i);
                fooProfile.Add(foo.Val);
            }
            readData.Finish();

            // Assert
            Assert.AreEqual(100, fooProfile.Count);
            for (int i = 0; i < fooProfile.Count; i++)
            {
                var expectedValue = _scalingValues[i/10].Item1 + _scalingValues[i/10].Item2;
                Assert.AreEqual(expectedValue, fooProfile[i]);
            }
        }

        [Test]
        public void ScaleTwoVariableTwoFiles()
        {
            // Arrange
            var sharedVars = SharedContainer.Instance;
            var foo1 = sharedVars.GetOrNew("FooVar1");
            var foo2 = sharedVars.GetOrNew("FooVar2");
            var fooProfile1 = new List<double>();
            var fooProfile2 = new List<double>();

            var input1 = GetTempFilename;
            var input2 = GetTempFilename;
            File.WriteAllLines(input1, GenerateFile(_scalingValues, "FooVar1", 10));
            File.WriteAllLines(input2, GenerateFile(_negScalingValues, "FooVar2", 10));
            IActor readData1 = new NextData(input1);
            IActor readData2 = new NextData(input2);

            // Act
            foo1.Val = 1;
            foo2.Val = 1;
            readData1.Init();
            readData2.Init();
            for (ulong i = 0; i < 100; i++)
            {
                readData1.Run(i);
                readData2.Run(i);
                fooProfile1.Add(foo1.Val);
                fooProfile2.Add(foo2.Val);
            }
            readData1.Finish();
            readData2.Finish();

            // Assert
            Assert.AreEqual(100, fooProfile1.Count);
            Assert.AreEqual(100, fooProfile2.Count);
            for (int i = 0; i < fooProfile1.Count; i++)
            {
                var expectedValue = _scalingValues[i / 10].Item1 + _scalingValues[i / 10].Item2;
                Assert.AreEqual(expectedValue, fooProfile1[i]);
                expectedValue = _negScalingValues[i / 10].Item1 + _negScalingValues[i / 10].Item2;
                Assert.AreEqual(expectedValue, fooProfile2[i]);
            }
        }

        [Test]
        public void ScaleTwoVariableOneFile()
        {
            // Arrange
            var sharedVars = SharedContainer.Instance;
            var foo1 = sharedVars.GetOrNew("FooVar1");
            var foo2 = sharedVars.GetOrNew("FooVar2");
            var fooProfile1 = new List<double>();
            var fooProfile2 = new List<double>();

            var input = GetTempFilename;
            File.WriteAllLines(input, GenerateFile(_scalingValues, _negScalingValues, "FooVar1", "FooVar2", 10));
            IActor readData1 = new NextData(input);

            // Act
            foo1.Val = 1;
            foo2.Val = 1;
            readData1.Init();
            for (ulong i = 0; i < 100; i++)
            {
                readData1.Run(i);
                fooProfile1.Add(foo1.Val);
                fooProfile2.Add(foo2.Val);
            }
            readData1.Finish();

            // Assert
            Assert.AreEqual(100, fooProfile1.Count);
            Assert.AreEqual(100, fooProfile2.Count);
            for (int i = 0; i < fooProfile1.Count; i++)
            {
                var expectedValue = _scalingValues[i / 10].Item1 + _scalingValues[i / 10].Item2;
                Assert.AreEqual(expectedValue, fooProfile1[i]);
                expectedValue = _negScalingValues[i / 10].Item1 + _negScalingValues[i / 10].Item2;
                Assert.AreEqual(expectedValue, fooProfile2[i]);
            }
        }

        private List<string> GenerateFile(List<Tuple<double, double>> scales, string var, int step)
        {
            int t = 0;
            var output = new List<string>(){"t,>" + var};
            scales.ForEach(s => 
            {
                output.Add(string.Format("{0},*{1}+{2}", t, s.Item1, s.Item2));
                t += step;
            });
            return output;
        }

        private List<string> GenerateFile(List<Tuple<double, double>> scales1, List<Tuple<double, double>> scales2, string var1, string var2, int step)
        {
            int t = 0;
            var output = new List<string>() { string.Format("t,>{0},>{1}", var1, var2) };
            for (int i = 0; i < scales1.Count; i++)
            {
                output.Add(string.Format("{0},*{1}+{2},*{3}+{4}", t, scales1[i].Item1, scales1[i].Item2, scales2[i].Item1, scales2[i].Item2));
                t += step;
            }
            return output;
        }
    }

    public class TupleList<T1, T2> : List<Tuple<T1, T2>>
    {
        public void Add( T1 item, T2 item2 )
        {
            Add( new Tuple<T1, T2>( item, item2 ) );
        }
    }
}
