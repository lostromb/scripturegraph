using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas
{
    public class ScriptureChapterDocument : GospelDocument
    {
        [JsonPropertyName("canon")]
        public required string Canon { get; set; }

        [JsonPropertyName("book")]
        public required string Book { get; set; }

        [JsonPropertyName("chapter")]
        public required int Chapter { get; set; }

        /// <summary>
        /// This refers to in-text headers e.g. "THE RECORD OF ALMA", not the study summary headers.
        /// Referred to in notation as verse 0 of the chapter, if present.
        /// </summary>
        [JsonPropertyName("header")]
        public GospelParagraph? ChapterHeader { get; set; }

        [JsonPropertyName("prev")]
        [JsonConverter(typeof(KnowledgeIdNullableSerializer))]
        public KnowledgeGraphNodeId? Prev { get; set; }

        [JsonPropertyName("next")]
        [JsonConverter(typeof(KnowledgeIdNullableSerializer))]
        public KnowledgeGraphNodeId? Next { get; set; }
    }
}
