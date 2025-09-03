using ScriptureGraph.Core.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.App.Schemas
{
    internal class SlowSearchQuery
    {
        public required List<KnowledgeGraphNodeId[]> SearchScopes;
        public int MaxResults;
    }
}
