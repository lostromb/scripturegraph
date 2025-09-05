using Durandal.Common.Logger;
using ScriptureGraph.Core.Training;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    public interface IKnowledgeGraph
    {
        List<KeyValuePair<KnowledgeGraphNodeId, float>> Query(KnowledgeGraphQuery query, ILogger logger);
    }
}
