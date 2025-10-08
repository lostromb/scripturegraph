using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas.Documents
{
    public class BookChapterDocument : GospelDocument
    {
        /// <summary>
        /// Book ID, such as "atgq" (answers to gospel questions)
        /// </summary>
        [JsonPropertyName("bookId")]
        public required string BookId { get; set; }

        /// <summary>
        /// Internal id of the chapter, used for file naming
        /// </summary>
        [JsonPropertyName("chapterId")]
        public required string ChapterId { get; set; }

        /// <summary>
        /// For reference books, this is the header or title of the specific chapter, e.g. "Life during the Milennium"
        /// Otherwise it's a localized string like "Chapter 12"
        /// </summary>
        [JsonPropertyName("chapterName")]
        public required string ChapterName { get; set; }

        /// <summary>
        /// Entity ID of the previous chapter, if one exists
        /// </summary>
        [JsonPropertyName("prev")]
        [JsonConverter(typeof(KnowledgeIdNullableSerializer))]
        public KnowledgeGraphNodeId? Prev { get; set; }

        /// <summary>
        /// Entity ID of the next chapter, if one exists
        /// </summary>
        [JsonPropertyName("next")]
        [JsonConverter(typeof(KnowledgeIdNullableSerializer))]
        public KnowledgeGraphNodeId? Next { get; set; }
    }
}
