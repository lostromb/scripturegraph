using ScriptureGraph.Core.Schemas.Serializers;
using System.Text.Json.Serialization;

namespace ScriptureGraph.Core.Schemas.Index
{
    internal class IndexJODDiscourse : IndexableEntity
    {
        [JsonPropertyName("title")]
        public required string ChapterTitle { get; set; }

        [JsonPropertyName("speaker")]
        public required string Speaker { get; set; }

        [JsonPropertyName("volume")]
        public int JournalVolume { get; set; }

        [JsonPropertyName("pageRange")]
        [JsonConverter(typeof(IntRangeNullableSerializer))]
        public IntRange? PageRange { get; set; }
    }
}
