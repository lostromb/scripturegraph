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
    public class ByuSpeechDocument : GospelDocument
    {
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("talk-id")]
        public required string TalkId { get; set; }

        [JsonPropertyName("speaker-eid")]
        [JsonConverter(typeof(KnowledgeIdSerializer))]
        public required KnowledgeGraphNodeId SpeakerEntityId { get; set; }

        [JsonPropertyName("speaker")]
        public required string Speaker { get; set; }

        [JsonPropertyName("kicker")]
        public string? Kicker { get; set; }
    }
}
