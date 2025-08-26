using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    public struct KnowledgeGraphNode
    {
        public KnowledgeGraphNode()
        {
            Edges = new GraphEdgeList(256);
        }

        public GraphEdgeList Edges;
    }
}
