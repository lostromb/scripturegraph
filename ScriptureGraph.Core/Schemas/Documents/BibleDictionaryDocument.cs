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
    public class BibleDictionaryDocument : GospelDocument
    {
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("topicid")]
        public required string TopicId { get; set; }
    }
}
