using Newtonsoft.Json.Converters;
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
    public class GospelParagraph
    {
        [JsonPropertyName("eid")]
        [JsonConverter(typeof(KnowledgeIdSerializer))]
        public required KnowledgeGraphNodeId ParagraphEntityId { get; set; }

        [JsonPropertyName("t")]
        public required string Text { get; set; }

        [JsonPropertyName("c")]
        [JsonConverter(typeof(StringEnumConverter))]
        public GospelParagraphClass Class { get; set; }
    }
}
