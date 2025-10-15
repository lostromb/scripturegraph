using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Training.Extractors;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training
{
    internal struct OmniParserOutput : IEquatable<OmniParserOutput>
    {
        public KnowledgeGraphNodeId Node;
        public bool LowEmphasis;

        public OmniParserOutput(ScriptureReference scripture)
        {
            Node = scripture.ToNodeId();
            LowEmphasis = scripture.LowEmphasis;
        }

        public OmniParserOutput(KnowledgeGraphNodeId node, bool lowEmphasis = false)
        {
            Node = node;
            LowEmphasis = lowEmphasis;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not null && obj is OmniParserOutput other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Node.GetHashCode();
        }

        public bool Equals(OmniParserOutput other)
        {
            return Node.Equals(other.Node);
        }
    }
}
