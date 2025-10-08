using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas
{
    public readonly struct IntRange
    {
        public readonly int Start;
        public readonly int End;

        public IntRange(int start, int end)
        {
            if (end < start)
            {
                throw new IndexOutOfRangeException("Start must come before end");
            }

            Start = start;
            End = end;
        }
    }
}
