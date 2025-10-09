using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas.Documents
{
    public enum GospelParagraphClass
    {
        /// <summary>
        /// Default style applied
        /// </summary>
        Default = 0,

        /// <summary>
        /// H1 header at the top of the document
        /// </summary>
        Header = 1,

        /// <summary>
        /// H2 header, usually in the middle of documents
        /// </summary>
        SubHeader = 2,

        /// <summary>
        /// Study summary given above scripture verses
        /// </summary>
        StudySummary = 3,

        /// <summary>
        /// A scripture verse
        /// </summary>
        Verse = 4,

        /// <summary>
        /// A quoted block
        /// </summary>
        Quotation = 5,

        /// <summary>
        /// The top-level "Chapter 4", "Section 76", etc. heading
        /// </summary>
        ChapterNum = 6,

        /// <summary>
        /// A line from a poem or song
        /// </summary>
        Poem = 7,
    }
}
