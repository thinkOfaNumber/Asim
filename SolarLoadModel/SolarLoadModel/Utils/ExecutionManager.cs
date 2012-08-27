using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolarLoadModel.Utils
{
    class DelayedExecution
    {
        public ulong ItToRun { get; set; }
        public Action Callback { get; set; }

        public DelayedExecution(ulong itToRun, Action callback)
        {
            ItToRun = itToRun;
            Callback = callback;
        }
    }

    public class ExecutionManager
    {
        private readonly List<DelayedExecution> _todo = new List<DelayedExecution>();

        public void After(ulong t, Action a)
        {
            _todo.Add(new DelayedExecution(t, a));
        }

        public void RunActions(ulong iter)
        {
            for (int i = _todo.Count - 1; i >= 0; i--)
            {
                var todo = _todo.ElementAt(i);
                if (todo.ItToRun == iter)
                {
                    todo.Callback();
                    _todo.RemoveAt(i);
                }
            }
        }
    }
}
