using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    public enum KnowledgeGraphNodeType : ushort
    {
        /// <summary>
        /// Default value, usually indicates error
        /// </summary>
        Unknown,

        /// <summary>
        /// A platonic ideal entity, designated by language-agnostic name
        /// </summary>
        Entity,

        /// <summary>
        /// A single word with a language code attached (in an invariant form such as lower case)
        /// </summary>
        Word,

        /// <summary>
        /// A series of N words with a language code attached
        /// </summary>
        NGram,

        /// <summary>
        /// A single designated book + chapter + verse within scripture
        /// Multiple verses will be designated by multiple nodes or features.
        /// </summary>
        ScriptureVerse,

        /// <summary>
        /// A reference to the topical guide, designated by URL keyword (e.g. scriptures/tg/sobriety where "sobriety" is the node name)
        /// </summary>
        TopicalGuideKeyword,

        /// <summary>
        /// A chapter of a book of scripture, e.g. "Hebrews 10"
        /// </summary>
        ScriptureChapter,

        /// <summary>
        /// An entire book of scripture, e.g. "Hebrews"
        /// </summary>
        ScriptureBook,
    }
}
