using ScriptureGraph.Core.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training
{
    public class TrainingFeature
    {
        public TrainingFeature(
            KnowledgeGraphNodeId a,
            KnowledgeGraphNodeId b,
            TrainingFeatureType featureType)
        {
            NodeA = a;
            NodeB = b;
            EdgeWeight = GetWeightForFeatureType(featureType);
        }

        public KnowledgeGraphNodeId NodeA;
        public KnowledgeGraphNodeId NodeB;
        public float EdgeWeight;

        public override string ToString()
        {
            return $"{NodeA} <-> {NodeB}";
        }

        private static float GetWeightForFeatureType(TrainingFeatureType featureType)
        {
            switch (featureType)
            {
                case TrainingFeatureType.WordAssociation:
                    return 0.2f;
                case TrainingFeatureType.EntityReference:
                    return 1.0f;
                default:
                    return 1.0f;
            }
        }
    }
}
