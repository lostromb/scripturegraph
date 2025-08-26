using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    public class KnowledgeGraphNode
    {
        public KnowledgeGraphNode(int edgeCapacity)
        {
            Edges = new GraphEdgeList(edgeCapacity);
        }

        public GraphEdgeList Edges;
    }
}
