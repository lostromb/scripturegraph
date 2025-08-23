using ScriptureGraph.Core.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training
{
    internal abstract class KnowledgeGraphFeature
    {
        public abstract KnowledgeGraphNodeType Type { get; }
        public abstract string NodeName { get; }
        public override string ToString()
        {
            return NodeName;
        }

        public override bool Equals(object? obj)
        {
            KnowledgeGraphFeature? other = obj as KnowledgeGraphFeature;
            if (other == null)
            {
                return false;
            }

            return NodeName.Equals(other.NodeName) &&
                Type == other.Type;
        }

        public override int GetHashCode()
        {
            return NodeName.GetHashCode() + Type.GetHashCode();
        }
    }
}
