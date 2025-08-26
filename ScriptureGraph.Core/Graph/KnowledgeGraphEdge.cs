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

        public override string? ToString()
        {
            return $"{Mass:F3} -> {Target}";
        }
    }
}
