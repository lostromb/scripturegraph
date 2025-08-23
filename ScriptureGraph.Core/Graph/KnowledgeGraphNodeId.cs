using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    internal struct KnowledgeGraphNodeId
    {
        public KnowledgeGraphNodeType Type;
        public string Name;

        public KnowledgeGraphNodeId(KnowledgeGraphNodeType type, string name)
        {
            Type = type;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            KnowledgeGraphNodeId other = (KnowledgeGraphNodeId)obj;
            return Type == other.Type && string.Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode() + Name.GetHashCode();
        }
    }
}
