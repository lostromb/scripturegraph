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
    public class ScriptureChapterDocument : GospelDocument
    {
        [JsonPropertyName("canon")]
        public required string Canon { get; set; }

        [JsonPropertyName("book")]
        public required string Book { get; set; }

        [JsonPropertyName("chapter")]
        public required int Chapter { get; set; }

        [JsonPropertyName("prev")]
        [JsonConverter(typeof(KnowledgeIdNullableSerializer))]
        public KnowledgeGraphNodeId? Prev { get; set; }

        [JsonPropertyName("next")]
        [JsonConverter(typeof(KnowledgeIdNullableSerializer))]
        public KnowledgeGraphNodeId? Next { get; set; }
    }
}
