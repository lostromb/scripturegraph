using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    public class KnowledgeGraphNode
    {
        public KnowledgeGraphNode(ushort edgeCapacity)
        {
            Edges = new GraphEdgeList(edgeCapacity);
        }

        internal KnowledgeGraphNode(GraphEdgeList edges)
        {
            Edges = edges;
        }

        public GraphEdgeList Edges;
    }
}
