namespace ScriptureGraph.Core.Graph
{
    public struct KnowledgeGraphNodeId
    {
        public KnowledgeGraphNodeType Type;
        public string Name;
        private int _cachedHashCode;

        public KnowledgeGraphNodeId(KnowledgeGraphNodeType type, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length > 255)
            {
                throw new IndexOutOfRangeException("Graph node ID name is too long; must be 255 chars or less");
            }

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
            return Type == other.Type && string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        public override string? ToString()
        {
            return $"{Enum.GetName(Type)}-{Name}";
        }

        public override int GetHashCode()
        {
            return _cachedHashCode;
        }

        public string Serialize()
        {
            return $"{(ushort)Type} {Name}";
        }

        public static KnowledgeGraphNodeId Deserialize(ReadOnlySpan<char> input)
        {
            int space = input.IndexOf(' ');
            if (space <= 0)
            {
                throw new FormatException("Invalid format: " + input.ToString());
            }

            ushort parsedVal;
            if (!ushort.TryParse(input.Slice(0, space), out parsedVal))
            {
                throw new FormatException("Couldn't parse node type " + input.ToString());
            }

            return new KnowledgeGraphNodeId((KnowledgeGraphNodeType)parsedVal, input.Slice(space + 1).ToString());
        }
    }
}
