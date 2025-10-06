using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas
{
    public enum GospelParagraphClass
    {
        /// <summary>
        /// Default style applied
        /// </summary>
        Default,

        /// <summary>
        /// H1 header at the top of the document
        /// </summary>
        Header,

        /// <summary>
        /// H2 header, usually in the middle of documents
        /// </summary>
        SubHeader,

        /// <summary>
        /// Study summary given above scripture verses
        /// </summary>
        StudySummary,

        /// <summary>
        /// A scripture verse
        /// </summary>
        Verse,

        /// <summary>
        /// A line of quotation (used also for poems, songs, etc.)
        /// </summary>
        Quotation,
    }
}
