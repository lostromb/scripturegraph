using Durandal.Common.NLP.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas.Serializers
{
    /// <summary>
    /// Intereprets a JSON property to/from a locale string e.g. "en-US", "pt-br" and a structured <see cref="LanguageCode"/> object.
    /// When formatting language codes as a string, this will prefer the BCP 47 alpha-2 format where available, for example "en-GB", "de-DE".
    /// However, some locales can only be represented in alpha-3 format (for example Filipino "fil", or meta-locales such as "und" or "mul").
    /// If that is the case, the alpha-3 format will be used.
    /// </summary>
    public class LanguageCodeSerializer : JsonConverter<LanguageCode>
    {
        public override LanguageCode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new FormatException("Expected JSON string");
            }

            string? stringVal = reader.GetString();
            if (stringVal == null)
            {
                throw new JsonException("Unexpected null string when reading language code");
            }

            return LanguageCode.Parse(stringVal);
        }

        public override void Write(Utf8JsonWriter writer, LanguageCode value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
