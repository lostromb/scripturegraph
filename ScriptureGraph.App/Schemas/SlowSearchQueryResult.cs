using ScriptureGraph.Core.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.App.Schemas
{
    internal class SlowSearchQueryResult
    {
        internal required List<Tuple<KnowledgeGraphNodeId, float>> EntityIds;
        internal required Dictionary<KnowledgeGraphNodeId, float> ActivatedWords;
    }
}
