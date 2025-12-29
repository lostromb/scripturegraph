using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training
{
    public enum TrainingFeatureType
    {
        /// <summary>
        /// Default value, usually means an error
        /// </summary>
        Unknown,

        /// <summary>
        /// Generally low weight. Relations of words and ngrams to entities and to each other.
        /// </summary>
        WordAssociation,

        /// <summary>
        /// Used for entity <-> ngram relations. Typically higher weight as ngrams are much more distinctive than individual words
        /// </summary>
        NgramAssociation,

        /// <summary>
        /// Used for words specifically that directly designate an entity. Think like the name of an entity as opposed
        /// to just random words that describe it (which would be word associations)
        /// </summary>
        WordDesignation,

        /// <summary>
        /// Unique references directly linking a scripture (typically through footnote)
        /// </summary>
        ScriptureReference,

        /// <summary>
        /// When a scripture reference has a range of verses and one "focal" verse, the non-focal verses will use this tag
        /// </summary>
        ScriptureReferenceWithoutEmphasis,

        /// <summary>
        /// Unique references directly between entities (in this case, like a scripture having a footnote linking to another)
        /// </summary>
        EntityReference,

        /// <summary>
        /// Used for paragraphs / verses that appear next to each other or belong to certain chapters
        /// </summary>
        ParagraphAssociation,

        /// <summary>
        /// Used for high-level relations between books, chapters, and paragraphs within those chapters
        /// </summary>
        BookAssociation,

        /// <summary>
        /// Used for sentences that appear next to each other or belong to certain chapters
        /// </summary>
        SentenceAssociation,
    }
}
