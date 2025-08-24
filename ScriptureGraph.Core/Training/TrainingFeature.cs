using ScriptureGraph.Core.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training
{
    public struct TrainingFeature
    {
        public TrainingFeature(KnowledgeGraphNodeId a, KnowledgeGraphNodeId b)
        {
            NodeA = a;
            NodeB = b;
        }

        public KnowledgeGraphNodeId NodeA;
        public KnowledgeGraphNodeId NodeB;

        public override string ToString()
        {
            return $"{NodeA} <-> {NodeB}";
        }
    }
}
