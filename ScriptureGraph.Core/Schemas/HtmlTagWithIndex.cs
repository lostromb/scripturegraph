using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas
{
    public class HtmlTagWithIndex : IComparable<HtmlTagWithIndex>
    {
        public required HtmlTag Tag { get; init; }
        public required int Index { get; init; }

        public int CompareTo(HtmlTagWithIndex? other)
        {
            if (other is null)
            {
                return -1;
            }

            return Index.CompareTo(other.Index);
        }
    }
}
