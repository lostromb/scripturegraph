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
    internal class KnowledgeIdSerializer : JsonConverter<KnowledgeGraphNodeId>
    {
        public override KnowledgeGraphNodeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ReadNodeId(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, KnowledgeGraphNodeId value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("t", (ushort)value.Type);
            writer.WriteString("n", value.Name);
            writer.WriteEndObject();
        }

        internal static KnowledgeGraphNodeId ReadNodeId(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new FormatException("Expected JSON start object");
            }

            NextToken(ref reader);
            KnowledgeGraphNodeType? nodeType = null;
            string? nodeName = null;
            while (reader.TokenType == JsonTokenType.PropertyName && (!nodeType.HasValue || nodeName == null))
            {
                if (reader.ValueTextEquals("t"))
                {
                    NextToken(ref reader);
                    nodeType = (KnowledgeGraphNodeType)reader.GetInt16();
                    NextToken(ref reader);
                }
                else if (reader.ValueTextEquals("n"))
                {
                    NextToken(ref reader);
                    nodeName = reader.GetString();
                    NextToken(ref reader);
                }
            }

            if (!nodeType.HasValue || nodeName == null)
            {
                throw new FormatException("Missing expected json properties");
            }

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new FormatException("Expected JSON end object");
            }

            //NextToken(ref reader);
            return new KnowledgeGraphNodeId(nodeType.Value, nodeName);
        }

        internal static void NextToken(ref Utf8JsonReader reader)
        {
            if (!reader.Read())
            {
                throw new FormatException("Unexpected end of JSON input");
            }
        }
    }

    internal class KnowledgeIdNullableSerializer : JsonConverter<KnowledgeGraphNodeId?>
    {
        public override KnowledgeGraphNodeId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                KnowledgeIdSerializer.NextToken(ref reader);
                return null;
            }

            return KnowledgeIdSerializer.ReadNodeId(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, KnowledgeGraphNodeId? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStartObject();
                writer.WriteNumber("t", (ushort)value.Value.Type);
                writer.WriteString("n", value.Value.Name);
                writer.WriteEndObject();
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
