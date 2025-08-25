using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    public struct KnowledgeGraphNode
    {
        public KnowledgeGraphNodeId Id;
        public GraphEdgeList Edges;
    }
}
