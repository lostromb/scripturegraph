using System.Text.Json.Serialization;

namespace ScriptureGraph.Core.Schemas.Index
{
    internal class IndexBibleDictionaryTopic : IndexableEntity
    {
        [JsonPropertyName("title")]
        public required string Title { get; set; }
    }
}
