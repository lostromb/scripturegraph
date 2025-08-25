namespace ScriptureGraph.Core.Graph
{
    public struct KnowledgeGraphEdge
    {
        public KnowledgeGraphEdge(KnowledgeGraphNodeId target, float mass)
        {
            Target = target;
            Mass = mass;
        }

        public KnowledgeGraphNodeId Target;
        public float Mass;
    }
}
