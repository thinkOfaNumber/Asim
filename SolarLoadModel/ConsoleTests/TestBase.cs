using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ConsoleTests
{
    public enum Arguments
    {
        Iterations = 0,
        Input,
        Output,
        Directory,
        Nopause,
        StartTime
    }

    [TestFixture]
    public class TestBase
    {
        public StringBuilder ConsoleOutput { get; set; }
        private string _tempPath;

        public string GetTempFilename
        {
            get
            {
                string f = Path.GetRandomFileName();
                return Path.GetTempPath() + '\\' + f.Replace(".", "") + "slms.tmp";
            }
        }

        private TextWriter _normalOutput;
        private StringWriter _testingConsole;
        private const string ConsoleDir = @"..\..\..\SolarLoadModel\bin\Debug\";
        private const string ConsoleApp = "SolarLoadModel.exe";


        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            // Set current folder to testing folder
            string assemblyCodeBase = System.Reflection.Assembly. GetExecutingAssembly().CodeBase;

            // Get directory name
            string dirName = Path.GetDirectoryName(assemblyCodeBase);

            // remove URL-prefix if it exists
            if (dirName.StartsWith("file:\\"))
                dirName = dirName.Substring(6);

            // set current folder
            //"C:\\Users\\iain.buchanan\\svn\\radical_pwcsolar\\Development\\trunk\\SolarLoadModel\\ConsoleTests\\bin\\Debug"
            Environment.CurrentDirectory = dirName;

            // Initialize string builder to replace console
            ConsoleOutput = new StringBuilder();
            _testingConsole = new StringWriter(ConsoleOutput);

            // swap normal output console with testing console - to reuse 
            // it later
            _normalOutput = System.Console.Out;
            System.Console.SetOut(_testingConsole);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            // set normal output stream to the console
            System.Console.SetOut(_normalOutput);
        }

        [SetUp]
        public void SetUp()
        {
            // clear string builder
            ConsoleOutput.Remove(0, ConsoleOutput.Length);
        }

        [TearDown]
        public void TearDown()
        {
            // Verbose output in console
            _normalOutput.Write(ConsoleOutput.ToString());
        }

        /// <summary>
        /// Starts the console application.
        /// </summary>
        /// <param name="arguments">The arguments for console application. 
        /// Specify empty string to run with no arguments</param>
        /// <returns>exit code of console app</returns>
        public int StartConsoleApplication(string arguments)
        {
            // Initialize process here
            Process proc = new Process();
            proc.StartInfo.FileName = ConsoleDir + ConsoleApp;
            // add arguments as whole string
            proc.StartInfo.Arguments = arguments;

            // use it to start from testing environment
            proc.StartInfo.UseShellExecute = false;

            // redirect outputs to have it in testing console
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;

            // set working directory
            proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

            // start and wait for exit
            proc.Start();
            proc.WaitForExit();

            // get output to testing console.
            System.Console.WriteLine(proc.StandardOutput.ReadToEnd());
            System.Console.Write(proc.StandardError.ReadToEnd());

            // return exit code
            return proc.ExitCode;
        }

        /// <summary>
        /// Converts an enum argument into a string to be used on the command line
        /// </summary>
        /// <param name="arg">Argument</param>
        /// <returns>string value to pass to console app</returns>
        public string ConsoleArguments(Arguments arg)
        {
            return "--" + arg;
        }

        public StringBuilder BuildCsvFor(string varName, double[] values)
        {
            var sb = new StringBuilder(values.Length * 10);
            sb.Append("t,");
            sb.Append(varName);
            sb.Append("\n");
            for (int i = 0; i < values.Length; i++)
            {
                sb.Append(i);
                sb.Append(",");
                sb.Append(values[i].ToString("0.0000"));
                sb.Append("\n");
            }
            return sb;
        }

        public List<string[]> CsvFileToArray(string filename)
        {
            var file = new StreamReader(filename);
            var values = new List<string[]>();

            string line;
            while ((line = file.ReadLine()) != null)
            {
                values.Add(line.Split(','));
            }
            return values;
        }

        public bool DoublesAreEqual(double a, double b)
        {
            double diff = Math.Abs(a - b);
            if (diff < 0.0001)
            {
                return true;
            }
            else
            {
                Console.WriteLine("Double precision off by: " + diff);
                return false;
            }
        }
    }
}
