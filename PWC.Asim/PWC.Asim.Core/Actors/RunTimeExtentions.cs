using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using PWC.Asim.Core.Contracts;
using PWC.Asim.Core.Exceptions;
using PWC.Asim.Core.Utils;

namespace PWC.Asim.Core.Actors
{
    class RunTimeExtentions : IActor
    {
        private readonly SharedContainer _sharedVars = SharedContainer.Instance;
        private int _asimRteCnt = 0;
        private readonly IList<string> _runTimeExtensions = new List<string>();
        private Delegates.EvalBlock[] _evalBlocks;
        // private readonly IList<double> _times = new List<double>();

        public RunTimeExtentions(IEnumerable<string> files = null)
        {
            if (files == null)
                return;
            foreach (var file in files)
            {
                File.ReadAllLines(file).ToList().ForEach(l => _runTimeExtensions.Add(l));
            }
        }

        public void Init() { }

        public void Read(ulong iteration) { }

        public void Write(ulong iteration) { }

        public void Run(ulong iteration)
        {
            // DateTime st, et;
            if (iteration == 0)
            {
                if (!_runTimeExtensions.Any())
                    return;
                Console.WriteLine("RTE Parsing run-time extensions");
                // st = DateTime.Now;
                ParseLines(_runTimeExtensions);
                // et = DateTime.Now;
                // Console.WriteLine("RTE Loaded run-time extensions in {0}s", (et - st).TotalSeconds);
                Console.WriteLine("RTE Loaded run-time extensions");
            }

            if (_asimRteCnt == 0)
                return;
            // run all evals
            // st = DateTime.Now;
            for (int i = 0; i < _asimRteCnt; i++)
            {
                _evalBlocks[i](iteration);
            }
            //et = DateTime.Now;
            //_times.Add((et - st).TotalSeconds);
        }

        public void Finish()
        {
            if (_asimRteCnt == 0)
                return;
            double tot = 0;
            //_times.ToList().ForEach(t => tot += t);
            //Console.WriteLine("RTE total time {0}s, ave time {1}s", tot, tot / _times.Count);
        }

        private void ParseLines(IList<String> lines)
        {
            var c = new CSharpCodeProvider();
            ICodeCompiler icc = c.CreateCompiler();
            var cp = new CompilerParameters();
            
            cp.CompilerOptions = "/t:library";
            cp.GenerateInMemory = true;

            cp.ReferencedAssemblies.Add("system.dll");
            var thisAsembly = typeof(RunTimeExtentions).Assembly.CodeBase;
            thisAsembly = new Uri(thisAsembly).LocalPath;
            // cp.ReferencedAssemblies.Add("PWC.Asim.Core.dll");
            cp.ReferencedAssemblies.Add(thisAsembly);

            var codeBuilder = new StringBuilder();
            var methods = new StringBuilder();
            var varInstance = new StringBuilder();

            var sharedVars = new List<string>();
            var wordRegex = new Regex(@"\b[A-Z][A-Za-z0-9]+\b");
            // look for MixedCaseWords in the eval lines
            foreach (var line in lines)
            {
                var matches = wordRegex.Matches(line);
                foreach (Match match in matches)
                {
                    sharedVars.Add(match.Value);
                }
            }
            // trim words into an actual list of shared vars:
            sharedVars = _sharedVars.GetAllNames().Intersect(sharedVars).ToList();

            // now go back and replace real shared "Name" with "Name.Val"
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                string replacedLine = line;
                sharedVars.ForEach(s => replacedLine = replacedLine.Replace(s, s + ".Val"));
                // build the list of methods
                methods.Append(string.Format(MethodFmt, _asimRteCnt++, replacedLine, _asimRteCnt));
            }

            foreach (var v in sharedVars)
            {
                // build the list of private shared var instances
                varInstance.Append(string.Format(InstanceFmt, v));
            }
            // build the code
            codeBuilder.Append(string.Format(PlumbingFmt, varInstance, methods));

            var code = codeBuilder.ToString();
            CompilerResults cr = icc.CompileAssemblyFromSource(cp, code);
            if (cr.Errors.Count > 0)
            {
                var e = cr.Errors[0];
                Console.WriteLine("RTE ERROR on line {0}: {1}", e.Line, e.ErrorText);
                Console.WriteLine("==========");
                var l = 1;
                foreach (var line in code.Split('\n'))
                {
                    Console.WriteLine("{0,3}: {1}", l++, line);
                }
                Console.WriteLine("==========");
                throw new SimulationException("The Run Time Evalution file failed to compile.");
            }

            Console.WriteLine("RTE compiled successfully.");

            Assembly a = cr.CompiledAssembly;
            object rteClass = a.CreateInstance("PWC.Asim.Core.Eval.AsimRteClass");
            Type t = rteClass.GetType();
            _evalBlocks = new Delegates.EvalBlock[_asimRteCnt];
            for (int i = 0; i < _asimRteCnt; i++)
            {
                MethodInfo evalFunc = t.GetMethod("EvalMethod" + i);
                var d = Delegate.CreateDelegate(typeof(Delegates.EvalBlock), rteClass, evalFunc);
                _evalBlocks[i] = (Delegates.EvalBlock)d;
            }
        }

        const string PlumbingFmt = @"
using System;
using PWC.Asim.Core.Utils;
namespace PWC.Asim.Core.Eval
{{
    public class AsimRteClass
    {{
        private static readonly SharedContainer Share = SharedContainer.Instance;
// Begin Instances of shared variables
{0}
// End Instances of shared variables

// Begin definition of methods to evaluate
{1}
// End definition of methods to evaluate
    }}
}}";
        const string InstanceFmt = @"        private readonly Shared {0} = Share.GetOrNew(""{0}"");
";
        const string MethodFmt = @"
        public object EvalMethod{0}(ulong it)
        {{
            return {1}; // eval line {2}
        }}
";
    }
}
