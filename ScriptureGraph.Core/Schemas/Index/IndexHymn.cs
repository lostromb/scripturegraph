using System.Text.Json.Serialization;

namespace ScriptureGraph.Core.Schemas.Index
{
    internal class IndexHymn : IndexableEntity
    {
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("tune")]
        public string? Tune { get; set; }

        [JsonPropertyName("hymnNo")]
        public required int Number { get; set; }
    }
}
