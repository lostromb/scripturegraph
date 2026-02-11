using ScriptureGraph.Core.Schemas.Serializers;
using System.Text.Json.Serialization;

namespace ScriptureGraph.Core.Schemas.Index
{
    internal class IndexConferenceTalk : IndexableEntity
    {
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("speaker")]
        public required string Speaker { get; set; }

        [JsonPropertyName("conference")]
        [JsonConverter(typeof(ConferenceSerializer))]
        public required Conference Conference { get; set; }

        [JsonPropertyName("pageRange")]
        [JsonConverter(typeof(IntRangeNullableSerializer))]
        public IntRange? ConferenceReportPageRange { get; set; }
    }
}
