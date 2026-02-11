using System.Text.Json.Serialization;

namespace ScriptureGraph.Core.Schemas.Index
{
    internal class IndexScriptureVerse : IndexableEntity
    {
        [JsonPropertyName("c")]
        public required string Canon { get; set; }

        [JsonPropertyName("b")]
        public required string Book { get; set; }

        [JsonPropertyName("c")]
        public required int Chapter { get; set; }

        [JsonPropertyName("v")]
        public required string Verse { get; set; }
    }
}
