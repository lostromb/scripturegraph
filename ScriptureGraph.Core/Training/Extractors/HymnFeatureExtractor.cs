using Durandal.Common.IO;
using Durandal.Common.Logger;
using Durandal.Common.MathExt;
using Durandal.Common.NLP.Language;
using Durandal.Common.Parsers;
using Durandal.Common.Utils;
using HtmlAgilityPack;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Schemas.Documents;
using System.Globalization;
using System.IO;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class HymnsFeatureExtractor
    {
        private static readonly Regex UrlPathParser = new Regex("\\/media\\/music\\/songs\\/(.+?)(?:\\/|\\?|$)");

        // <script>window\.renderData=([\w\W]+?)<\/script>
        private static readonly Regex GiantScriptBlobParser = new Regex("<script>window\\.renderData=([\\w\\W]+?)<\\/script>");

        private static readonly Regex NewlineReplacer = new Regex("[\\r\\n]+");

        public static void ExtractFeatures(string htmlPage, Uri pageUrl, ILogger logger, Action<TrainingFeature> trainingFeaturesOut)
        {
            try
            {
                DocumentParseModel? parseResult = ParseInternal(htmlPage, pageUrl, logger);
                if (parseResult == null)
                {
                    //logger.Log($"Null parse result: {pageUrl}", LogLevel.Err);
                    return;
                }

                // High-level features
                // Title of the song -> Song
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(parseResult.SongName))
                {
                    trainingFeaturesOut(new TrainingFeature(
                        parseResult.DocumentEntityId,
                        ngram,
                        TrainingFeatureType.WordDesignation));
                }

                // High-level features
                // Song scripture references -> Song
                foreach (var parsedRef in parseResult.References)
                {
                    trainingFeaturesOut(new TrainingFeature(
                        parseResult.DocumentEntityId,
                        parsedRef.Node,
                        parsedRef.LowEmphasis ? TrainingFeatureType.ScriptureReferenceWithoutEmphasis : TrainingFeatureType.ScriptureReference));
                }

                List<TrainingFeature> scratch = new List<TrainingFeature>();
                Verse? previousVerse = null;
                foreach (Verse verse in parseResult.Verses)
                {
                    // Associate this verse with the song
                    trainingFeaturesOut(new TrainingFeature(
                        parseResult.DocumentEntityId,
                        verse.VerseEntityId,
                        TrainingFeatureType.BookAssociation));

                    // And with the previous verse
                    if (previousVerse != null)
                    {
                        trainingFeaturesOut(new TrainingFeature(
                            verse.VerseEntityId,
                            previousVerse.VerseEntityId,
                            TrainingFeatureType.ParagraphAssociation));
                    }

                    previousVerse = verse;

                    string thisVerseWordBreakerText = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, verse.Text);

                    // Common word and ngram level features associated with this verse entity
                    scratch.Clear();
                    EnglishWordFeatureExtractor.ExtractTrainingFeatures(thisVerseWordBreakerText, scratch, verse.VerseEntityId);

                    foreach (var f in scratch)
                    {
                        trainingFeaturesOut(f);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        public static void ExtractSearchIndexFeatures(string htmlPage, Uri pageUrl, ILogger logger, Action<TrainingFeature> trainingFeaturesOut, EntityNameIndex nameIndex)
        {
            try
            {
                DocumentParseModel? parseResult = ParseInternal(htmlPage, pageUrl, logger);
                if (parseResult == null)
                {
                    //logger.Log($"Null parse result: {pageUrl}", LogLevel.Err);
                    return;
                }

                // Extract ngrams from the hymn name and associate it with the hymn
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(parseResult.SongName))
                {
                    trainingFeaturesOut(new TrainingFeature(
                        parseResult.DocumentEntityId,
                        ngram,
                        TrainingFeatureType.WordDesignation));
                }

                nameIndex.Mapping[parseResult.DocumentEntityId] = parseResult.SongName;
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        public static HymnDocument? ParseDocument(string htmlPage, Uri pageUrl, ILogger logger)
        {
            try
            {
                DocumentParseModel? parseResult = ParseInternal(htmlPage, pageUrl, logger);
                if (parseResult == null)
                {
                    //logger.Log($"Null parse result: {pageUrl}", LogLevel.Err);
                    return null;
                }

                HymnDocument returnVal = new HymnDocument()
                {
                    DocumentEntityId = parseResult.DocumentEntityId,
                    DocumentType = GospelDocumentType.Hymn,
                    Language = parseResult.Language,
                    SongNum = parseResult.SongNumber,
                    Title = parseResult.SongName,
                    SongId = parseResult.SongId,
                    Paragraphs = new List<GospelParagraph>(),
                };

                returnVal.Paragraphs.Add(new GospelParagraph()
                {
                    Text = parseResult.SongName,
                    ParagraphEntityId = FeatureToNodeMapping.HymnVerse(parseResult.SongId, "title"),
                    Class = GospelParagraphClass.Header
                });

                foreach (Verse verse in parseResult.Verses)
                {
                    returnVal.Paragraphs.Add(new GospelParagraph()
                    {
                        Text = verse.Text,
                        ParagraphEntityId = verse.VerseEntityId,
                        Class = GospelParagraphClass.Poem,
                    });
                }

                returnVal.Paragraphs.Add(new GospelParagraph()
                {
                    Text = parseResult.Credits,
                    ParagraphEntityId = FeatureToNodeMapping.HymnVerse(parseResult.SongId, "credits"),
                    Class = GospelParagraphClass.Default
                });

                return returnVal;
            }
            catch (Exception e)
            {
                logger.Log(e);
                return null;
            }
        }

        private static DocumentParseModel? ParseInternal(string htmlPage, Uri pageUrl, ILogger logger)
        {
            try
            {
                Match urlParse = UrlPathParser.Match(pageUrl.AbsolutePath);
                if (!urlParse.Success)
                {
                    logger.Log("Failed to parse URL", LogLevel.Err);
                    return null;
                }

                htmlPage = WebUtility.HtmlDecode(htmlPage);
                htmlPage = LdsDotOrgCommonParsers.RemoveNbsp(htmlPage);
                string songId = urlParse.Groups[1].Value;

                int hymnNumber;
                if (!HymnMetadataEnglish.TryGetHymnNumber(songId, out hymnNumber))
                {
                    logger.Log($"No hymn number for {songId}", LogLevel.Err);
                    return null;
                }

                // Rip the render data from the JS script that we get.
                // This gives us the actual structured data used by the internal player,
                // so there's less manual parsing.

                Match bigBlobMatch = GiantScriptBlobParser.Match(htmlPage);
                if (!bigBlobMatch.Success)
                {
                    logger.Log("Could not find json data blob", LogLevel.Err);
                    return null;
                }

                string scriptBlob = bigBlobMatch.Groups[1].Value;
                JsonRenderData? parsedDoc = JsonSerializer.Deserialize<JsonRenderData>(scriptBlob);
                if (parsedDoc == null || parsedDoc.data == null || parsedDoc.data.songData == null)
                {
                    logger.Log("Invalid or missing json data", LogLevel.Err);
                    return null;
                }

                //using (FileStream stream = new FileStream(@"D:\Code\scripturegraph\runtime\test.json", FileMode.Create, FileAccess.Write))
                //using (Utf8JsonWriter jsonWriter = new Utf8JsonWriter(stream))
                //{
                //    JsonSerializer.Serialize(jsonWriter, parsedDoc);
                //}

                DocumentParseModel returnVal = new DocumentParseModel()
                {
                    Language = LanguageCode.ENGLISH,
                    Credits = FormatHtmlFragment(parsedDoc.data.songData.fullCreditsText ?? string.Empty, logger),
                    SongId = songId,
                    SongName = parsedDoc.data.songData.title,
                    SongNumber = hymnNumber,
                    DocumentEntityId = FeatureToNodeMapping.Hymn(songId),
                };

                int chorusNum = 1;
                foreach (var verse in parsedDoc.data.songData.verses)
                {
                    if (string.Equals("Verse", verse.verseType, StringComparison.OrdinalIgnoreCase))
                    {
                        //Console.WriteLine($"VERSE {verse.verseNumber}");
                        //Console.WriteLine(FormatHtmlFragment(verse.verseBody, logger));
                        returnVal.Verses.Add(new Verse()
                        {
                            Text = FormatHtmlFragment(verse.verseBody, logger),
                            VerseEntityId = FeatureToNodeMapping.HymnVerse(songId, $"verse-{verse.verseNumber}"),
                        });
                    }
                    else if (string.Equals("Chorus", verse.verseType, StringComparison.OrdinalIgnoreCase))
                    {
                        //Console.WriteLine($"CHORUS");
                        //Console.WriteLine(FormatHtmlFragment(verse.verseBody, logger));
                        returnVal.Verses.Add(new Verse()
                        {
                            Text = $"<i>{FormatHtmlFragment(verse.verseBody, logger)}</i>",
                            VerseEntityId = FeatureToNodeMapping.HymnVerse(songId, $"chorus-{chorusNum++}"),
                        });
                    }
                    else if (string.Equals("Instructions", verse.verseType, StringComparison.OrdinalIgnoreCase))
                    {
                        // https://www.churchofjesuschrist.org/media/music/songs/father-in-heaven-we-do-believe?lang=eng
                        // Ignore
                    }
                }

                foreach (var scripture in parsedDoc.data.songData.scriptures)
                {
                    foreach (OmniParserOutput scriptureRef in OmniParser.ParseHtml(scripture.linkText, logger, LanguageCode.ENGLISH))
                    {
                        returnVal.References.Add(scriptureRef);
                    }
                }

                return returnVal;
            }
            catch (Exception e)
            {
                logger.Log(e);
                return null;
            }
        }

        private static string FormatHtmlFragment(string htmlFragment, ILogger logger)
        {
            LdsDotOrgCommonParsers.HtmlFragmentParseModel parsedHtml = LdsDotOrgCommonParsers.ParseAndFormatHtmlFragmentNew(htmlFragment, logger, insertLineBreaks: true);
            return StringUtils.RegexReplace(NewlineReplacer, parsedHtml.TextWithInlineFormatTags, "\r\n");
        }

        private class DocumentParseModel
        {
            public required KnowledgeGraphNodeId DocumentEntityId;
            public required LanguageCode Language;
            public required int SongNumber;
            public required string SongName;
            public required string SongId;
            public required string Credits;
            public readonly List<Verse> Verses = new List<Verse>();
            public readonly List<OmniParserOutput> References = new List<OmniParserOutput>();
        }

        private class Verse
        {
            public required KnowledgeGraphNodeId VerseEntityId;
            public required string Text;

            public override string ToString()
            {
                return Text;
            }
        }

        private class JsonRenderData
        {
            public required JsonPageData data { get; set; }
        }

        private class JsonPageData
        {
            public required JsonSongData songData { get; set; }
        }

        private class JsonSongData
        {
            public required string title { get; set; }
            public string? fullCreditsText { get; set; }
            public required List<JsonVerse> verses { get; set; }
            public required List<JsonScriptureLink> scriptures { get; set; }
        }

        private class JsonVerse
        {
            public required string verseBody { get; set; }
            public required int verseNumber { get; set; }
            public required string verseType { get; set; }
        }

        private class JsonScriptureLink
        {
            public required string linkText { get; set; }
            public required string linkUrl { get; set; }
        }

        public static IEnumerable<Uri> GetAllSongUris()
        {
            return HymnMetadataEnglish.GetAllHymnIds().Select(id => new Uri($"https://www.churchofjesuschrist.org/media/music/songs/{id}?lang=eng"));
        }
    }
}
