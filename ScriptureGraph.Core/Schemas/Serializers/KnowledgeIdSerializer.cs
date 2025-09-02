using Newtonsoft.Json.Linq;
using ScriptureGraph.Core.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas.Serializers
{
    public class KnowledgeIdSerializer : JsonConverter<KnowledgeGraphNodeId>
    {
        public override KnowledgeGraphNodeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new FormatException("Expected a string value");
            }

            return KnowledgeGraphNodeId.Deserialize(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, KnowledgeGraphNodeId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Serialize());
        }
    }

    public class KnowledgeIdNullableSerializer : JsonConverter<KnowledgeGraphNodeId?>
    {
        public override KnowledgeGraphNodeId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            else if (reader.TokenType != JsonTokenType.String)
            {
                throw new FormatException("Expected a string value or null");
            }

            return KnowledgeGraphNodeId.Deserialize(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, KnowledgeGraphNodeId? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.Serialize());
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
