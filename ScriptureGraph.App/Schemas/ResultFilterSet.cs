using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.App.Schemas
{
    internal class ResultFilterSet
    {
        public bool Include_OldTestament { get; set; }
        public bool Include_NewTestament { get; set; }
        public bool Include_BookOfMormon { get; set; }
        public bool Include_DC { get; set; }
        public bool Include_PearlGP { get; set; }
        public bool Include_BibleDict { get; set; }
        public bool Include_GenConference { get; set; }
    }
}
