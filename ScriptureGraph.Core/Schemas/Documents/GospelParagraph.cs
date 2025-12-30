using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas.Serializers;
using System.Text.Json.Serialization;

namespace ScriptureGraph.Core.Schemas.Documents
{
    public class GospelParagraph
    {
        [JsonPropertyName("eid")]
        [JsonConverter(typeof(KnowledgeIdSerializer))]
        public required KnowledgeGraphNodeId ParagraphEntityId { get; set; }

        [JsonPropertyName("t")]
        public required string Text { get; set; }

        [JsonPropertyName("c")]
        [JsonConverter(typeof(JsonNumberEnumConverter<GospelParagraphClass>))]
        public GospelParagraphClass Class { get; set; }

        [JsonPropertyName("r")]
        public List<GospelParagraphSubregion>? Regions { get; set; }
    }
}
