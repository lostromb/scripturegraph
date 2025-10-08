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
        /// A line of quotation (used also for poems, songs, etc.)
        /// </summary>
        Quotation = 5,
    }
}
