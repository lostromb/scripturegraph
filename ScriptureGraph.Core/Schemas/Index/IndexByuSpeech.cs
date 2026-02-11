using ScriptureGraph.Core.Schemas.Serializers;
using System.Text.Json.Serialization;

namespace ScriptureGraph.Core.Schemas.Index
{
    internal class IndexByuSpeech : IndexableEntity
    {
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("speaker")]
        public required string Speaker { get; set; }

        [JsonPropertyName("date")]
        public required DateOnly? Date { get; set; }
    }
}
