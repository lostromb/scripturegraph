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
    public class GospelParagraphSubregion
    {
        [JsonPropertyName("eid")]
        [JsonConverter(typeof(KnowledgeIdSerializer))]
        public required KnowledgeGraphNodeId RegionEntityId { get; set; }

        [JsonPropertyName("r")]
        [JsonConverter(typeof(IntRangeSerializer))]
        public required IntRange Range { get; set; }
    }
}
