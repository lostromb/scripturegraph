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
    public class IntRangeSerializer : JsonConverter<IntRange>
    {
        public override IntRange Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new FormatException("Expected a string value");
            }

            string val = reader.GetString()!;
            int separator = val.IndexOf('-');
            return new IntRange(int.Parse(val.AsSpan(0, separator)), int.Parse(val.AsSpan(separator + 1)));
        }

        public override void Write(Utf8JsonWriter writer, IntRange value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{value.Start}-{value.End}");
        }
    }
}
