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
    public class ProclamationDocument : GospelDocument
    {
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("procid")]
        public required string ProclamationId { get; set; }
    }
}
