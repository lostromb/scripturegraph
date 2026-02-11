using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas
{
    public class HtmlTag
    {
        public required string TagName { get; init; }
        public required bool IsClosing { get; init; }
        public required IDictionary<string, string> Attributes { get; init; }
    }
}
