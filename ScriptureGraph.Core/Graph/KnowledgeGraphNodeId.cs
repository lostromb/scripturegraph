using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    public struct KnowledgeGraphNodeId
    {
        public KnowledgeGraphNodeType Type;
        public string Name;
        private int _cachedHashCode;

        public KnowledgeGraphNodeId(KnowledgeGraphNodeType type, string name)
        {
            Type = type;
            Name = name;
            _cachedHashCode = Type.GetHashCode() + Name.GetHashCode();
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

        public override string? ToString()
        {
            return $"{Enum.GetName(Type)}-{Name}";
        }

        public override int GetHashCode()
        {
            return _cachedHashCode;
        }
    }
}
