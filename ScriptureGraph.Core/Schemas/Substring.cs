using ScriptureGraph.Core.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas
{
    public class Substring
    {
        public Substring(string text, IntRange range, KnowledgeGraphNodeId? entityId)
        {
            Text = text;
            Range = range;
            EntityId = entityId;
        }

        public Substring(string text, Match regexMatch, KnowledgeGraphNodeId? entityId)
        {
            Text = text;
            Range = new IntRange(regexMatch.Index, regexMatch.Index + regexMatch.Length);
            EntityId = entityId;
        }

        public IntRange Range { get; private set; }
        public string Text { get; private set; }
        public KnowledgeGraphNodeId? EntityId { get; private set; }
    }
}
