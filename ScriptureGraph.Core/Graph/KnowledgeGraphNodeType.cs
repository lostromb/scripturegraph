using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    public enum KnowledgeGraphNodeType : ushort
    {
        // Default value
        Unknown,

        // A platonic ideal entity, designated by language-sgnostic name
        Entity,

        // A single word with a language code attached
        Word,

        // A series of N words with a language code attached
        NGram,

        // A single designated book + chapter + verse within scripture
        // Multiple verses will be designated by multiple nodes or features.
        ScriptureVerse,

        // A reference to the topical guide, designated by URL keyword (e.g. scriptures/tg/sobriety)
        TopicalGuideKeyword,
    }
}
