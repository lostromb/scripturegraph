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
    internal class ConferenceSerializer : JsonConverter<Conference>
    {
        public override Conference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new FormatException("Expected JSON string");
            }

            string? parsedText = reader.GetString();
            if (parsedText == null)
            {
                throw new FormatException("Unexpected null string when parsing conference");
            }

            int parsedNum;
            if (!int.TryParse(parsedText.AsSpan(0, 2), out parsedNum))
            {
                throw new FormatException("Could not parse conference string " + parsedText);
            }

            ConferencePhase phase = parsedNum == 4 ? ConferencePhase.April : ConferencePhase.October;
            if (!int.TryParse(parsedText.AsSpan(3), out parsedNum))
            {
                throw new FormatException("Could not parse conference string " + parsedText);
            }

            return new Conference(phase, parsedNum);
        }

        public override void Write(Utf8JsonWriter writer, Conference value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"{(value.Phase == ConferencePhase.April ? "04" : "10")}-{value.Year}");
        }
    }
}
