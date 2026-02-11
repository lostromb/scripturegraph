using ScriptureGraph.Core.Schemas.Serializers;
using System.Text.Json.Serialization;

namespace ScriptureGraph.Core.Schemas.Index
{
    internal class IndexBookChapter : IndexableEntity
    {
        [JsonPropertyName("title")]
        public required string ChapterTitle { get; set; }

        [JsonPropertyName("book")]
        public required string BookId { get; set; }

        [JsonPropertyName("chapNum")]
        public int? ChapterNumber { get; set; }

        [JsonPropertyName("pageRange")]
        [JsonConverter(typeof(IntRangeNullableSerializer))]
        public IntRange? PageRange { get; set; }
    }
}
