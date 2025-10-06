using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas
{
    public class BookChapterDocument : GospelDocument
    {
        [JsonPropertyName("bookId")]
        public required string BookId { get; set; }
    }
}
