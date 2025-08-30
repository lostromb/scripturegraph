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
    public class ConferenceTalkDocument : GospelDocument
    {
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("speaker-eid")]
        [JsonConverter(typeof(KnowledgeIdSerializer))]
        public required KnowledgeGraphNodeId SpeakerEntityId { get; set; }

        [JsonPropertyName("speaker")]
        public required string Speaker { get; set; }

        [JsonPropertyName("conference")]
        [JsonConverter(typeof(ConferenceSerializer))]
        public required Conference Conference { get; set; }
    }
}
