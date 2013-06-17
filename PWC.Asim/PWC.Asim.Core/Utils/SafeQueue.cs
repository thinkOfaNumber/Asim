using System;
using System.Collections.Generic;

namespace PWC.Asim.Core.Utils
{
    public class SafeQueue<T> : Queue<T>
    {
        public SafeQueue() : base()
        {
        }

        public SafeQueue (IEnumerable<T> e) : base(e)
        {
        }

        public new T Peek()
        {
            return Count == 0 ? default(T) : base.Peek();
        }

        public new T Dequeue()
        {
            return Count == 0 ? default(T) : base.Dequeue();
        }
    }
}
