namespace ScriptureGraph.Core.Graph
{
    public class KnowledgeGraphEdge
    {
        public KnowledgeGraphEdge(KnowledgeGraphNodeId target)
        {
            Target = target;
        }

        public KnowledgeGraphNodeId Target;
        public uint Mass;
        public float Weight;
    }
}
