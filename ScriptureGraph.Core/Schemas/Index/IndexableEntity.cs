using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas.Serializers;
using System.Text.Json.Serialization;

namespace ScriptureGraph.Core.Schemas.Index
{
    internal abstract class IndexableEntity
    {

        [JsonPropertyName("eid")]
        [JsonConverter(typeof(KnowledgeIdSerializer))]
        public required KnowledgeGraphNodeId EntityId { get; set; }
    }
}
