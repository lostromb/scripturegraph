using Durandal.Common.File;
using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Schemas
{
    /// <summary>
    /// Generic class for a document to be shown in a reader, consisting of header metadata and a list of paragraphs
    /// with entity node references for each
    /// </summary>
    public class GospelDocument
    {
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter<GospelDocumentType>))]
        public required GospelDocumentType DocumentType { get; set; }

        [JsonPropertyName("eid")]
        [JsonConverter(typeof(KnowledgeIdSerializer))]
        public required KnowledgeGraphNodeId DocumentEntityId { get; set; }

        [JsonPropertyName("lang")]
        [JsonConverter(typeof(LanguageCodeSerializer))]
        public required LanguageCode Language { get; set; }

        [JsonPropertyName("para")]
        public required List<GospelParagraph> Paragraphs { get; set; }

        public static GospelDocument ParsePolymorphic(Stream readStream)
        {
            JsonDocument jsonStructure = JsonDocument.Parse(readStream);
            GospelDocument? parsedDoc = jsonStructure.Deserialize<GospelDocument>();
            if (parsedDoc == null)
            {
                throw new InvalidDataException("Did not parse any valid JSON data from read stream");
            }

            switch (parsedDoc.DocumentType)
            {
                case GospelDocumentType.ScriptureChapter:
                    parsedDoc = jsonStructure.Deserialize<ScriptureChapterDocument>();
                    break;
                case GospelDocumentType.GeneralConferenceTalk:
                    parsedDoc = jsonStructure.Deserialize<ConferenceTalkDocument>();
                    break;
                case GospelDocumentType.BibleDictionaryEntry:
                    parsedDoc = jsonStructure.Deserialize<BibleDictionaryDocument>();
                    break;
                default:
                    throw new InvalidDataException("Unknown document type: " + parsedDoc.DocumentType.ToString());
            }

            if (parsedDoc == null)
            {
                throw new InvalidDataException("Failed polymorphic JSON parse - null document");
            }

            return parsedDoc;
        }

        public static void SerializePolymorphic(Stream stream, GospelDocument document)
        {
            using (Utf8JsonWriter jsonWriter = new Utf8JsonWriter(stream))
            {
                if (document is ScriptureChapterDocument chapter)
                {
                    JsonSerializer.Serialize(jsonWriter, chapter);
                }
                else if (document is ConferenceTalkDocument conf)
                {
                    JsonSerializer.Serialize(jsonWriter, conf);
                }
                else if (document is BibleDictionaryDocument bd)
                {
                    JsonSerializer.Serialize(jsonWriter, bd);
                }
                else
                {
                    throw new InvalidCastException("Unknown gospel document subtype");
                }
            }
        }
    }
}
