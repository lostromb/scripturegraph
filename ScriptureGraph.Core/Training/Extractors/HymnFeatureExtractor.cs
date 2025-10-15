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
                if (!HYMN_ID_TO_NUMBER.TryGetValue(songId, out hymnNumber))
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
                    foreach (OmniParserOutput scriptureRef in OmniParser.ParseHtml(scripture.linkText, logger))
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
            return HYMN_ID_TO_NUMBER.Keys.Select(id => new Uri($"https://www.churchofjesuschrist.org/media/music/songs/{id}?lang=eng"));
        }

        private static readonly IReadOnlyDictionary<string, int> HYMN_ID_TO_NUMBER = new Dictionary<string, int>()
        {
            { "the-morning-breaks", 1 },
        #region A giant table
            { "the-spirit-of-god", 2 },
            { "now-let-us-rejoice", 3 },
            { "truth-eternal", 4 },
            { "high-on-the-mountain-top", 5 },
            { "redeemer-of-israel", 6 },
            { "israel-israel-god-is-calling", 7 },
            { "awake-and-arise", 8 },
            { "come-rejoice", 9 },
            { "come-sing-to-the-lord", 10 },
            { "what-was-witnessed-in-the-heavens", 11 },
            { "twas-witnessed-in-the-morning-sky", 12 },
            { "an-angel-from-on-high", 13 },
            { "sweet-is-the-peace-the-gospel-brings", 14 },
            { "i-saw-a-mighty-angel-fly", 15 },
            { "what-glorious-scenes-mine-eyes-behold", 16 },
            { "awake-ye-saints-of-god-awake", 17 },
            { "the-voice-of-god-again-is-heard", 18 },
            { "we-thank-thee-o-god-for-a-prophet", 19 },
            { "god-of-power-god-of-right", 20 },
            { "come-listen-to-a-prophets-voice", 21 },
            { "we-listen-to-a-prophets-voice", 22 },
            { "we-ever-pray-for-thee", 23 },
            { "god-bless-our-prophet-dear", 24 },
            { "now-well-sing-with-one-accord", 25 },
            { "joseph-smiths-first-prayer", 26 },
            { "praise-to-the-man", 27 },
            { "saints-behold-how-great-jehovah", 28 },
            { "a-poor-wayfaring-man-of-grief", 29 },
            { "come-come-ye-saints", 30 },
            { "o-god-our-help-in-ages-past", 31 },
            { "the-happy-day-at-last-has-come", 32 },
            { "our-mountain-home-so-dear", 33 },
            { "o-ye-mountains-high", 34 },
            { "for-the-strength-of-the-hills", 35 },
            { "they-the-builders-of-the-nation", 36 },
            { "the-wintry-day-descending-to-its-close", 37 },
            { "come-all-ye-saints-of-zion", 38 },
            { "o-saints-of-zion", 39 },
            { "arise-o-glorious-zion", 40 },
            { "let-zion-in-her-beauty-rise", 41 },
            { "hail-to-the-brightness-of-zions-glad-morning", 42 },
            { "zion-stands-with-hills-surrounded", 43 },
            { "beautiful-zion-built-above", 44 },
            { "lead-me-into-life-eternal", 45 },
            { "glorious-things-of-thee-are-spoken", 46 },
            { "we-will-sing-of-zion", 47 },
            { "glorious-things-are-sung-of-zion", 48 },
            { "adam-ondi-ahman", 49 },
            { "come-thou-glorious-day-of-promise", 50 },
            { "sons-of-michael-he-approaches", 51 },
            { "the-day-dawn-is-breaking", 52 },
            { "let-earths-inhabitants-rejoice", 53 },
            { "behold-the-mountain-of-the-lord", 54 },
            { "lo-the-mighty-god-appearing", 55 },
            { "softly-beams-the-sacred-dawning", 56 },
            { "were-not-ashamed-to-own-our-lord", 57 },
            { "come-ye-children-of-the-lord", 58 },
            { "come-o-thou-king-of-kings", 59 },
            { "battle-hymn-of-the-republic", 60 },
            { "raise-your-voices-to-the-lord", 61 },
            { "all-creatures-of-our-god-and-king", 62 },
            { "great-king-of-heaven", 63 },
            { "on-this-day-of-joy-and-gladness", 64 },
            { "come-all-ye-saints-who-dwell-on-earth", 65 },
            { "rejoice-the-lord-is-king", 66 },
            { "glory-to-god-on-high", 67 },
            { "a-mighty-fortress-is-our-god", 68 },
            { "all-glory-laud-and-honor", 69 },
            { "sing-praise-to-him", 70 },
            { "with-songs-of-praise", 71 },
            { "praise-to-the-lord-the-almighty", 72 },
            { "praise-the-lord-with-heart-and-voice", 73 },
            { "praise-ye-the-lord", 74 },
            { "in-hymns-of-praise", 75 },
            { "god-of-our-fathers-we-come-unto-thee", 76 },
            { "great-is-the-lord", 77 },
            { "god-of-our-fathers-whose-almighty-hand", 78 },
            { "with-all-the-power-of-heart-and-tongue", 79 },
            { "god-of-our-fathers-known-of-old", 80 },
            { "press-forward-saints", 81 },
            { "for-all-the-saints", 82 },
            { "guide-us-o-thou-great-jehovah", 83 },
            { "faith-of-our-fathers", 84 },
            { "how-firm-a-foundation", 85 },
            { "how-great-thou-art", 86 },
            { "god-is-love", 87 },
            { "great-god-attend-while-zion-sings", 88 },
            { "the-lord-is-my-light", 89 },
            { "from-all-that-dwell-below-the-skies", 90 },
            { "father-thy-children-to-thee-now-raise", 91 },
            { "for-the-beauty-of-the-earth", 92 },
            { "prayer-of-thanksgiving", 93 },
            { "come-ye-thankful-people", 94 },
            { "now-thank-we-all-our-god", 95 },
            { "dearest-children-god-is-near-you", 96 },
            { "lead-kindly-light", 97 },
            { "i-need-thee-every-hour", 98 },
            { "nearer-dear-savior-to-thee", 99 },
            { "nearer-my-god-to-thee", 100 },
            { "guide-me-to-thee", 101 },
            { "jesus-lover-of-my-soul", 102 },
            { "precious-savior-dear-redeemer", 103 },
            { "jesus-savior-pilot-me", 104 },
            { "master-the-tempest-is-raging", 105 },
            { "god-speed-the-right", 106 },
            { "lord-accept-our-true-devotion", 107 },
            { "the-lord-is-my-shepherd", 108 },
            { "the-lord-my-pasture-will-prepare", 109 },
            { "cast-thy-burden-upon-the-lord", 110 },
            { "rock-of-ages", 111 },
            { "savior-redeemer-of-my-soul", 112 },
            { "our-saviors-love", 113 },
            { "come-unto-him", 114 },
            { "come-ye-disconsolate", 115 },
            { "come-follow-me", 116 },
            { "come-unto-jesus", 117 },
            { "ye-simple-souls-who-stray", 118 },
            { "come-we-that-love-the-lord", 119 },
            { "lean-on-my-ample-arm", 120 },
            { "im-a-pilgrim-im-a-stranger", 121 },
            { "though-deepening-trials", 122 },
            { "oh-may-my-soul-commune-with-thee", 123 },
            { "be-still-my-soul", 124 },
            { "how-gentle-gods-commands", 125 },
            { "how-long-o-lord-most-holy-and-true", 126 },
            { "does-the-journey-seem-long", 127 },
            { "when-faith-endures", 128 },
            { "where-can-i-turn-for-peace", 129 },
            { "be-thou-humble", 130 },
            { "more-holiness-give-me", 131 },
            { "god-is-in-his-holy-temple", 132 },
            { "father-in-heaven", 133 },
            { "i-believe-in-christ", 134 },
            { "my-redeemer-lives", 135 },
            { "i-know-that-my-redeemer-lives", 136 },
            { "testimony", 137 },
            { "bless-our-fast-we-pray", 138 },
            { "in-fasting-we-approach-thee", 139 },
            { "did-you-think-to-pray", 140 },
            { "jesus-the-very-thought-of-thee", 141 },
            { "sweet-hour-of-prayer", 142 },
            { "let-the-holy-spirit-guide", 143 },
            { "secret-prayer", 144 },
            { "prayer-is-the-souls-sincere-desire", 145 },
            { "gently-raise-the-sacred-strain", 146 },
            { "sweet-is-the-work", 147 },
            { "sabbath-day", 148 },
            { "as-the-dew-from-heaven-distilling", 149 },
            { "o-thou-kind-and-gracious-father", 150 },
            { "we-meet-dear-lord", 151 },
            { "god-be-with-you-till-we-meet-again", 152 },
            { "lord-we-ask-thee-ere-we-part", 153 },
            { "father-this-hour-has-been-one-of-joy", 154 },
            { "we-have-partaken-of-thy-love", 155 },
            { "sing-we-now-at-parting", 156 },
            { "thy-spirit-lord-has-stirred-our-souls", 157 },
            { "before-thee-lord-i-bow-my-head", 158 },
            { "now-the-day-is-over", 159 },
            { "softly-now-the-light-of-day", 160 },
            { "the-lord-be-with-us", 161 },
            { "lord-we-come-before-thee-now", 162 },
            { "lord-dismiss-us-with-thy-blessing", 163 },
            { "great-god-to-thee-my-evening-song", 164 },
            { "abide-with-me-tis-eventide", 165 },
            { "abide-with-me", 166 },
            { "come-let-us-sing-an-evening-hymn", 167 },
            { "as-the-shadows-fall", 168 },
            { "as-now-we-take-the-sacrament", 169 },
            { "god-our-father-hear-us-pray", 170 },
            { "with-humble-heart", 171 },
            { "in-humility-our-savior", 172 },
            { "while-of-these-emblems-we-partake-saul", 173 },
            { "while-of-these-emblems-we-partake-aeolian", 174 },
            { "o-god-the-eternal-father", 175 },
            { "tis-sweet-to-sing-the-matchless-love-meredith", 176 },
            { "tis-sweet-to-sing-the-matchless-love-hancock", 177 },
            { "o-lord-of-hosts", 178 },
            { "again-our-dear-redeeming-lord", 179 },
            { "father-in-heaven-we-do-believe", 180 },
            { "jesus-of-nazareth-savior-and-king", 181 },
            { "well-sing-all-hail-to-jesus-name", 182 },
            { "in-remembrance-of-thy-suffering", 183 },
            { "upon-the-cross-of-calvary", 184 },
            { "reverently-and-meekly-now", 185 },
            { "again-we-meet-around-the-board", 186 },
            { "god-loved-us-so-he-sent-his-son", 187 },
            { "thy-will-o-lord-be-done", 188 },
            { "o-thou-before-the-world-began", 189 },
            { "in-memory-of-the-crucified", 190 },
            { "behold-the-great-redeemer-die", 191 },
            { "he-died-the-great-redeemer-died", 192 },
            { "i-stand-all-amazed", 193 },
            { "there-is-a-green-hill-far-away", 194 },
            { "how-great-the-wisdom-and-the-love", 195 },
            { "jesus-once-of-humble-birth", 196 },
            { "o-savior-thou-who-wearest-a-crown", 197 },
            { "that-easter-morn", 198 },
            { "he-is-risen", 199 },
            { "christ-the-lord-is-risen-today", 200 },
            { "joy-to-the-world", 201 },
            { "oh-come-all-ye-faithful", 202 },
            { "angels-we-have-heard-on-high", 203 },
            { "silent-night", 204 },
            { "once-in-royal-davids-city", 205 },
            { "away-in-a-manger", 206 },
            { "it-came-upon-the-midnight-clear", 207 },
            { "o-little-town-of-bethlehem", 208 },
            { "hark-the-herald-angels-sing", 209 },
            { "with-wondering-awe", 210 },
            { "while-shepherds-watched-their-flocks", 211 },
            { "far-far-away-on-judeas-plains", 212 },
            { "the-first-noel", 213 },
            { "i-heard-the-bells-on-christmas-day", 214 },
            { "ring-out-wild-bells", 215 },
            { "we-are-sowing", 216 },
            { "come-let-us-anew", 217 },
            { "we-give-thee-but-thine-own", 218 },
            { "because-i-have-been-given-much", 219 },
            { "lord-i-would-follow-thee", 220 },
            { "dear-to-the-heart-of-the-shepherd", 221 },
            { "hear-thou-our-hymn-o-lord", 222 },
            { "have-i-done-any-good", 223 },
            { "i-have-work-enough-to-do", 224 },
            { "we-are-marching-on-to-glory", 225 },
            { "improve-the-shining-moments", 226 },
            { "there-is-sunshine-in-my-soul-today", 227 },
            { "you-can-make-the-pathway-bright", 228 },
            { "today-while-the-sun-shines", 229 },
            { "scatter-sunshine", 230 },
            { "father-cheer-our-souls-tonight", 231 },
            { "let-us-oft-speak-kind-words", 232 },
            { "nay-speak-no-ill", 233 },
            { "jesus-mighty-king-in-zion", 234 },
            { "should-you-feel-inclined-to-censure", 235 },
            { "lord-accept-into-thy-kingdom", 236 },
            { "do-what-is-right", 237 },
            { "behold-thy-sons-and-daughters-lord", 238 },
            { "choose-the-right", 239 },
            { "know-this-that-every-soul-is-free", 240 },
            { "count-your-blessings", 241 },
            { "praise-god-from-whom-all-blessings-flow", 242 },
            { "let-us-all-press-on", 243 },
            { "come-along-come-along", 244 },
            { "this-house-we-dedicate-to-thee", 245 },
            { "onward-christian-soldiers", 246 },
            { "we-love-thy-house-o-god", 247 },
            { "up-awake-ye-defenders-of-zion", 248 },
            { "called-to-serve", 249 },
            { "we-are-all-enlisted", 250 },
            { "behold-a-royal-army", 251 },
            { "put-your-shoulder-to-the-wheel", 252 },
            { "like-ten-thousand-legions-marching", 253 },
            { "true-to-the-faith", 254 },
            { "carry-on", 255 },
            { "as-zions-youth-in-latter-days", 256 },
            { "rejoice-a-glorious-sound-is-heard", 257 },
            { "o-thou-rock-of-our-salvation", 258 },
            { "hope-of-israel", 259 },
            { "whos-on-the-lords-side", 260 },
            { "thy-servants-are-prepared", 261 },
            { "go-ye-messengers-of-glory", 262 },
            { "go-forth-with-faith", 263 },
            { "hark-all-ye-nations", 264 },
            { "arise-o-god-and-shine", 265 },
            { "the-time-is-far-spent", 266 },
            { "how-wondrous-and-great", 267 },
            { "come-all-whose-souls-are-lighted", 268 },
            { "jehovah-lord-of-heaven-and-earth", 269 },
            { "ill-go-where-you-want-me-to-go", 270 },
            { "oh-holy-words-of-truth-and-love", 271 },
            { "oh-say-what-is-truth", 272 },
            { "truth-reflects-upon-our-senses", 273 },
            { "the-iron-rod", 274 },
            { "men-are-that-they-might-have-joy", 275 },
            { "come-away-to-the-sunday-school", 276 },
            { "as-i-search-the-holy-scriptures", 277 },
            { "thanks-for-the-sabbath-school", 278 },
            { "thy-holy-word", 279 },
            { "welcome-welcome-sabbath-morning", 280 },
            { "help-me-teach-with-inspiration", 281 },
            { "we-meet-again-in-sabbath-school", 282 },
            { "the-glorious-gospel-light-has-shone", 283 },
            { "if-you-could-hie-to-kolob", 284 },
            { "god-moves-in-a-mysterious-way", 285 },
            { "oh-what-songs-of-the-heart", 286 },
            { "rise-ye-saints-and-temples-enter", 287 },
            { "how-beautiful-thy-temples-lord", 288 },
            { "holy-temples-on-mount-zion", 289 },
            { "rejoice-ye-saints-of-latter-days", 290 },
            { "turn-your-hearts", 291 },
            { "o-my-father", 292 },
            { "each-life-that-touches-ours-for-good", 293 },
            { "love-at-home", 294 },
            { "o-love-that-glorifies-the-son", 295 },
            { "our-father-by-whose-name", 296 },
            { "from-homes-of-saints-glad-songs-arise", 297 },
            { "home-can-be-a-heaven-on-earth", 298 },
            { "children-of-our-heavenly-father", 299 },
            { "families-can-be-together-forever", 300 },
            { "i-am-a-child-of-god", 301 },
            { "i-know-my-father-lives", 302 },
            { "keep-the-commandments", 303 },
            { "teach-me-to-walk-in-the-light", 304 },
            { "the-light-divine", 305 },
            { "gods-daily-care", 306 },
            { "in-our-lovely-deseret", 307 },
            { "love-one-another", 308 },
            { "as-sisters-in-zion", 309 },
            { "a-key-was-turned-in-latter-days", 310 },
            { "we-meet-again-as-sisters", 311 },
            { "we-ever-pray-for-thee-women", 312 },
            { "god-is-love-women", 313 },
            { "how-gentle-gods-commands-women", 314 },
            { "jesus-the-very-thought-of-thee-women", 315 },
            { "the-lord-is-my-shepherd-women", 316 },
            { "sweet-is-the-work-women", 317 },
            { "love-at-home-women", 318 },
            { "ye-elders-of-israel", 319 },
            { "the-priesthood-of-our-lord", 320 },
            { "ye-who-are-called-to-labor", 321 },
            { "come-all-ye-sons-of-god", 322 },
            { "rise-up-o-men-of-god-mens-choir", 323 },
            { "rise-up-o-men-of-god", 324 },
            { "see-the-mighty-priesthood-gathered", 325 },
            { "come-come-ye-saints-mens-choir", 326 },
            { "go-ye-messengers-of-heaven-mens-choir", 327 },
            { "an-angel-from-on-high-mens-choir", 328 },
            { "thy-servants-are-prepared-men", 329 },
            { "see-the-mighty-angel-flying-mens-choir", 330 },
            { "oh-say-what-is-truth-mens-choir", 331 },
            { "come-o-thou-king-of-kings-mens-choir", 332 },
            { "high-on-the-mountain-top-mens-choir", 333 },
            { "i-need-thee-every-hour-mens-choir", 334 },
            { "brightly-beams-our-fathers-mercy-mens-choir", 335 },
            { "school-thy-feelings", 336 },
            { "o-home-beloved", 337 },
            { "america-the-beautiful", 338 },
            { "my-country-tis-of-thee", 339 },
            { "the-star-spangled-banner", 340 },
            { "god-save-the-king", 341 },
            { "come-thou-fount-of-every-blessing", 1001 },
            { "when-the-savior-comes-again", 1002 },
            { "it-is-well-with-my-soul", 1003 },
            { "i-will-walk-with-jesus", 1004 },
            { "his-eye-is-on-the-sparrow", 1005 },
            { "think-a-sacred-song", 1006 },
            { "as-bread-is-broken", 1007 },
            { "bread-of-life-living-water", 1008 },
            { "gethsemane", 1009 },
            { "amazing-grace", 1010 },
            { "holding-hands-around-the-world", 1011 },
            { "anytime-anywhere", 1012 },
            { "gods-gracious-love", 1013 },
            { "my-shepherd-will-supply-my-need", 1014 },
            { "oh-the-deep-deep-love-of-jesus", 1015 },
            { "behold-the-wounds-in-jesus-hands", 1016 },
            { "this-is-the-christ", 1017 },
            { "come-lord-jesus", 1018 },
            { "to-love-like-thee-release-3", 1019 },
            { "softly-and-tenderly-jesus-is-calling-release-3", 1020 },
            { "i-know-that-my-savior-loves-me-release-3", 1021 },
            { "faith-in-every-footstep-release-3", 1022 },
            { "standing-on-the-promises-release-3", 1023 },
            { "i-have-faith-in-the-lord-jesus-christ-release-3", 1024 },
            { "take-my-heart-and-let-it-be-consecrated-release-3", 1025 },
            { "holy-places-release-3", 1026 },
            { "welcome-home-release-3", 1027 },
            { "this-little-light-of-mine-release-3", 1028 },
            { "i-cant-count-them-all-release-3", 1029 },
            { "close-as-a-quiet-prayer-release-3", 1030 },
            { "come-hear-the-word-the-lord-has-spoken-release-3", 1031 },
            { "look-unto-christ", 1032 },
            { "oh-how-great-is-our-joy", 1033 },
            { "im-a-pioneer-too", 1034 },
            { "as-i-keep-the-sabbath-day", 1035 },
            { "read-the-book-of-mormon-and-pray", 1036 },
            { "im-gonna-live-so-god-can-use-me", 1037 },
            { "the-lords-my-shepherd", 1038 },
            { "because", 1039 },
            { "his-voice-as-the-sound", 1040 },
            { "o-lord-who-gave-thy-life-for-me", 1041 },
            { "thou-gracious-god-whose-mercy-lends", 1042 },
            { "help-us-remember", 1043 },
            { "how-did-the-savior-minister", 1044 },
            { "jesus-is-the-way", 1045 },
            { "can-you-count-the-stars-in-heaven", 1046 },
            { "he-cares-for-me", 1047 },
            { "our-prayer-to-thee", 1048 },
            { "joseph-prayed-in-faith", 1049 },
            { "stand-by-me", 1050 },
            { "this-day-is-a-good-day-lord", 1051 },
            { "hail-the-day-that-sees-him-rise", 1201 },
            { "he-is-born-the-divine-christ-child", 1202 },
            { "what-child-is-this", 1203 },
            { "star-bright", 1204 },
            { "let-easter-anthems-ring-release-3", 1205 },
            { "were-you-there-release-3", 1206 },
            { "still-still-still", 1207 },
            { "go-tell-it-on-the-mountain", 1208 },
            { "little-baby-in-a-manger", 1209 },
            #endregion
        };
    }
}
