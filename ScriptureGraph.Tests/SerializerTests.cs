using Durandal.Common.IO;
using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Schemas.Serializers;
using ScriptureGraph.Core.Training;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScriptureGraph.Tests
{
    [TestClass]
    public sealed class SerializerTests
    {
        private class EntityIdContainer
        {
            [JsonConverter(typeof(KnowledgeIdSerializer))]
            public KnowledgeGraphNodeId Node { get; set; }

            [JsonConverter(typeof(KnowledgeIdNullableSerializer))]
            public KnowledgeGraphNodeId? NullableNode { get; set; }

            [JsonConverter(typeof(KnowledgeIdNullableSerializer))]
            public KnowledgeGraphNodeId? NullableNode2 { get; set; }
        }
        
        [TestMethod]
        public void TestKnowledgeIdSerializer()
        {
            EntityIdContainer myObject = new EntityIdContainer()
            {
                Node = FeatureToNodeMapping.NGram("test", "one", LanguageCode.ENGLISH),
                NullableNode = FeatureToNodeMapping.NGram("test", "two", LanguageCode.ENGLISH),
                NullableNode2 = null
            };

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            //Console.Write(JsonSerializer.Serialize(myObject, options));
            //Assert.AreEqual(s, null);

            using (RecyclableMemoryStream utf8JsonStream = new RecyclableMemoryStream(RecyclableMemoryStreamManager.Default))
            using (Utf8JsonWriter jsonWriter = new Utf8JsonWriter(utf8JsonStream))
            {
                //while (true)
                {
                    utf8JsonStream.Seek(0, SeekOrigin.Begin);
                    jsonWriter.Reset();
                    JsonSerializer.Serialize(jsonWriter, myObject, options);
                    utf8JsonStream.Seek(0, SeekOrigin.Begin);
                    EntityIdContainer? newObject = JsonSerializer.Deserialize<EntityIdContainer>(utf8JsonStream, options);
                    Assert.IsNotNull(newObject);
                    Assert.AreEqual(myObject.Node, newObject.Node);
                    Assert.AreEqual(myObject.NullableNode, newObject.NullableNode);
                    Assert.AreEqual(myObject.NullableNode2, newObject.NullableNode2);
                }
            }
        }

        private class ConferenceContainer
        {
            [JsonConverter(typeof(ConferenceSerializer))]
            public required Conference Conf1 { get; set; }

            [JsonConverter(typeof(ConferenceSerializer))]
            public required Conference Conf2 { get; set; }
        }

        [TestMethod]
        public void TestConferenceSerializer()
        {
            ConferenceContainer myObject = new ConferenceContainer()
            {
                Conf1 = new Conference(ConferencePhase.April, 1998),
                Conf2 = new Conference(ConferencePhase.October, 2010),
            };

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            Console.Write(JsonSerializer.Serialize(myObject, options));
            //Assert.AreEqual(s, null);

            using (RecyclableMemoryStream utf8JsonStream = new RecyclableMemoryStream(RecyclableMemoryStreamManager.Default))
            using (Utf8JsonWriter jsonWriter = new Utf8JsonWriter(utf8JsonStream))
            {
                //while (true)
                {
                    utf8JsonStream.Seek(0, SeekOrigin.Begin);
                    jsonWriter.Reset();
                    JsonSerializer.Serialize(jsonWriter, myObject, options);
                    utf8JsonStream.Seek(0, SeekOrigin.Begin);
                    ConferenceContainer? newObject = JsonSerializer.Deserialize<ConferenceContainer>(utf8JsonStream, options);
                    Assert.IsNotNull(newObject);
                    Assert.AreEqual(myObject.Conf1, newObject.Conf1);
                    Assert.AreEqual(myObject.Conf2, newObject.Conf2);
                }
            }
        }

        [TestMethod]
        public void TestPolymorphicSerializer()
        {
            ScriptureChapterDocument document = new ScriptureChapterDocument()
            {
                DocumentType = GospelDocumentType.ScriptureChapter,
                Language = LanguageCode.ENGLISH,
                Canon = "bofm",
                Book = "1-ne",
                Chapter = 1,
                Paragraphs = new List<GospelParagraph>(),
                DocumentEntityId = FeatureToNodeMapping.ScriptureChapter("1-ne", 1),
            };

            document.Paragraphs.Add(new GospelParagraph()
            {
                ParagraphEntityId = FeatureToNodeMapping.ScriptureVerse("1-ne", 1, 1),
                Text = "Verse 1 goes here",
            });

            document.Paragraphs.Add(new GospelParagraph()
            {
                ParagraphEntityId = FeatureToNodeMapping.ScriptureVerse("1-ne", 1, 2),
                Text = "Verse 2 goes here",
            });

            //Console.Write(JsonSerializer.Serialize(document, new JsonSerializerOptions() { WriteIndented = true }));

            using (RecyclableMemoryStream utf8JsonStream = new RecyclableMemoryStream(RecyclableMemoryStreamManager.Default))
            using (Utf8JsonWriter jsonWriter = new Utf8JsonWriter(utf8JsonStream))
            {
                //while (true)
                {
                    utf8JsonStream.Seek(0, SeekOrigin.Begin);
                    jsonWriter.Reset();
                    JsonSerializer.Serialize(jsonWriter, document);
                    utf8JsonStream.Seek(0, SeekOrigin.Begin);
                    GospelDocument? genericNewObject = GospelDocument.ParsePolymorphic(utf8JsonStream);
                    Assert.IsNotNull(genericNewObject);
                    ScriptureChapterDocument? newObject = genericNewObject as ScriptureChapterDocument;
                    Assert.IsNotNull(newObject);
                    Assert.AreEqual(document.DocumentType, newObject.DocumentType);
                    Assert.AreEqual(document.Language, newObject.Language);
                    Assert.AreEqual(document.Canon, newObject.Canon);
                    Assert.AreEqual(document.Book, newObject.Book);
                    Assert.AreEqual(document.Chapter, newObject.Chapter);
                    Assert.AreEqual(document.DocumentEntityId, newObject.DocumentEntityId);
                }
            }
        }
    }
}
