using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas.Serializers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScriptureGraph.Core.Schemas
{
    public class GospelDocumentMeta
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

        public static GospelDocumentMeta ParseHeader(Stream readStream)
        {
            GospelDocumentMeta? parsedDoc = JsonSerializer.Deserialize<GospelDocumentMeta>(readStream);
            if (parsedDoc == null)
            {
                throw new InvalidDataException("Did not parse any valid JSON data from read stream");
            }

            return parsedDoc;
        }
    }

    /// <summary>
    /// Generic class for a document to be shown in a reader, consisting of header metadata and a list of paragraphs
    /// with entity node references for each
    /// </summary>
    public class GospelDocument : GospelDocumentMeta
    {
        [JsonPropertyName("para")]
        public required List<GospelParagraph> Paragraphs { get; set; }

        public static GospelDocument ParsePolymorphic(Stream readStream)
        {
            JsonDocument jsonStructure = JsonDocument.Parse(readStream);
            GospelDocumentMeta? parsedMetaDoc = jsonStructure.Deserialize<GospelDocumentMeta>();
            if (parsedMetaDoc == null)
            {
                throw new InvalidDataException("Did not parse any valid JSON data from read stream");
            }

            GospelDocument? parsedDoc;
            switch (parsedMetaDoc.DocumentType)
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
                case GospelDocumentType.GospelBookChapter:
                    parsedDoc = jsonStructure.Deserialize<BookChapterDocument>();
                    break;
                default:
                    throw new InvalidDataException("Unknown document type: " + parsedMetaDoc.DocumentType.ToString());
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
                else if (document is BookChapterDocument chap)
                {
                    JsonSerializer.Serialize(jsonWriter, chap);
                }
                else
                {
                    throw new InvalidCastException("Unknown gospel document subtype");
                }
            }
        }
    }
}
