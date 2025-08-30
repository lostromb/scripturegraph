using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using System.Net;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class ScripturePageFeatureExtractor
    {
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/scriptures\\/(.+?)\\/(.+?)\\/(\\d+)");

        // used for resolving next / prev chapters for any given page
        private static readonly string[] BOOKS_IN_ORDER = new string[]
            {
                // ot
                //"gen", "ex", "lev", "num", "deut", "josh", "judg", "ruth", "1-sam", "2-sam", "1-kgs", "2-kgs", "1-chr", "2-chr", "ezra",
                //"neh", "esth", "job", "ps", "prov", "eccl", "song", "isa", "jer", "lam", "ezek", "dan", "hosea", "joel", "amos", "obad",
                //"jonah", "micah", "nahum", "hab", "zeph", "hag", "zech", "mal",
                // nt
                // bofm
                "1-ne", "2-ne", "jacob", "enos", "jarom", "omni", "w-of-m", "mosiah", "alma", "hel", "3-ne", "4-ne", "morm", "ether", "moro",
            };

        private static readonly IReadOnlyDictionary<string, string> BOOK_TO_CANON = new Dictionary<string, string>()
        {
            { "1-ne", "bofm" },
            { "2-ne", "bofm" },
            { "jacob", "bofm" },
            { "enos", "bofm" },
            { "jarom", "bofm" },
            { "omni", "bofm" },
            { "w-of-m", "bofm" },
            { "mosiah", "bofm" },
            { "alma", "bofm" },
            { "hel", "bofm" },
            { "3-ne", "bofm" },
            { "4-ne", "bofm" },
            { "morm", "bofm" },
            { "ether", "bofm" },
            { "moro", "bofm" },
        };

        private static readonly IReadOnlyDictionary<string, int> BOOK_CHAPTER_LENGTHS = new Dictionary<string, int>()
        {
            // bofm
            { "1-ne", 22 },
            { "2-ne", 33 },
            { "jacob", 7 },
            { "enos", 1 },
            { "jarom", 1 },
            { "omni", 1 },
            { "w-of-m", 1 },
            { "mosiah", 29 },
            { "alma", 63 },
            { "hel", 16 },
            { "3-ne", 30 },
            { "4-ne", 1 },
            { "morm", 9 },
            { "ether", 15 },
            { "moro", 10 },
        };

        public static void ExtractFeatures(string htmlPage, Uri pageUrl, ILogger logger, List<TrainingFeature> returnVal)
        {
            try
            {
                Match urlParse = UrlPathParser.Match(pageUrl.AbsolutePath);
                if (!urlParse.Success)
                {
                    logger.Log("Failed to parse URL", LogLevel.Err);
                    return;
                }

                htmlPage = WebUtility.HtmlDecode(htmlPage);
                string canon = urlParse.Groups[1].Value;
                string book = urlParse.Groups[2].Value;
                int chapter = int.Parse(urlParse.Groups[3].Value);

                Dictionary<int, StructuredVerse> verses = LdsDotOrgCommonParsers.ParseVerses(canon, book, chapter, htmlPage);
                Dictionary<string, StructuredFootnote> footnotes = LdsDotOrgCommonParsers.ParseFootnotesFromPage(htmlPage, logger);

                // Now restructure each verse into a series of words with correlated footnotes
                foreach (StructuredVerse verse in verses.Values.OrderBy(s => s.Verse))
                {
                    string plainText;
                    List<SingleWordWithFootnotes> words = LdsDotOrgCommonParsers.ParseParagraphWithStudyNoteRef(verse.Text, logger, footnotes, out plainText);
                    ExtractFeaturesFromSingleVerse(verse, plainText, words, logger, returnVal);
                }

                ExtractChapterLevelFeatures(canon, book, chapter, logger, returnVal);
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        public static ScriptureChapterDocument? ParseDocument(string htmlPage, Uri pageUrl, ILogger logger)
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
                
                string canon = urlParse.Groups[1].Value;
                string book = urlParse.Groups[2].Value;
                int chapter = int.Parse(urlParse.Groups[3].Value);

                ScriptureChapterDocument returnVal = new ScriptureChapterDocument()
                {
                    DocumentType = GospelDocumentType.ScriptureChapter,
                    Canon = canon,
                    Book = book,
                    Chapter = chapter,
                    Language = LanguageCode.ENGLISH,
                    Paragraphs = new List<GospelParagraph>(),
                    DocumentEntityId = FeatureToNodeMapping.ScriptureChapter(canon, book, chapter),
                    Prev = GetPrevChapter(canon, book, chapter),
                    Next = GetNextChapter(canon, book, chapter)
                };

                Dictionary<int, StructuredVerse> verses = LdsDotOrgCommonParsers.ParseVerses(canon, book, chapter, htmlPage);
                
                if (verses.ContainsKey(0))
                {
                    returnVal.ChapterHeader = new GospelParagraph()
                    {
                        ParagraphEntityId = FeatureToNodeMapping.ScriptureVerse(canon, book, chapter, 0),
                        Text = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, verses[0].Text)
                    };
                }

                foreach (var verse in verses.OrderBy(s => s.Key))
                {
                    if (verse.Key == 0)
                    {
                        continue;
                    }

                    returnVal.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = FeatureToNodeMapping.ScriptureVerse(canon, book, chapter, verse.Key),
                        Text = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, verse.Value.Text)
                    });
                }

                return returnVal;
            }
            catch (Exception e)
            {
                logger.Log(e);
            }

            return null;
        }

        private static KnowledgeGraphNodeId? GetPrevChapter(string canon, string book, int chapter)
        {
            if (chapter > 1)
            {
                return FeatureToNodeMapping.ScriptureChapter(canon, book, chapter - 1);
            }
            else
            {
                int thisBookIndex = Array.IndexOf(BOOKS_IN_ORDER, book);
                if (thisBookIndex <= 0)
                {
                    return null;
                }

                string prevBookName = BOOKS_IN_ORDER[thisBookIndex - 1];
                string prevBookCanon = BOOK_TO_CANON[prevBookName];
                int prevBookChapter = BOOK_CHAPTER_LENGTHS[prevBookName];
                return FeatureToNodeMapping.ScriptureChapter(prevBookCanon, prevBookName, prevBookChapter);
            }
        }

        private static KnowledgeGraphNodeId? GetNextChapter(string canon, string book, int chapter)
        {
            int thisBookLength = BOOK_CHAPTER_LENGTHS[book];
            if (chapter < thisBookLength)
            {
                return FeatureToNodeMapping.ScriptureChapter(canon, book, chapter + 1);
            }
            else
            {
                int thisBookIndex = Array.IndexOf(BOOKS_IN_ORDER, book);
                if (thisBookIndex <= 0)
                {
                    return null;
                }

                string nextBookName = BOOKS_IN_ORDER[thisBookIndex + 1];
                string nextBookCanon = BOOK_TO_CANON[nextBookName];
                return FeatureToNodeMapping.ScriptureChapter(nextBookCanon, nextBookName, 1);
            }
        }

        private static void ExtractChapterLevelFeatures(
            string canon,
            string book,
            int chapter,
            ILogger logger,
            List<TrainingFeature> trainingFeaturesOut)
        {
            // Relationship between this scripture book and the chapters in it
            trainingFeaturesOut.Add(new TrainingFeature(
                FeatureToNodeMapping.ScriptureBook(
                    canon,
                    book),
                FeatureToNodeMapping.ScriptureChapter(
                    canon,
                    book,
                    chapter),
                TrainingFeatureType.BookAssociation));

            // And the previous chapter, if applicable
            if (chapter > 1)
            {
                trainingFeaturesOut.Add(new TrainingFeature(
                    FeatureToNodeMapping.ScriptureChapter(
                        canon,
                        book,
                        chapter),
                    FeatureToNodeMapping.ScriptureChapter(
                        canon,
                        book,
                        chapter - 1),
                    TrainingFeatureType.BookAssociation));
            }
        }

        private static void ExtractFeaturesFromSingleVerse(
            StructuredVerse currentVerse,
            string rawText,
            List<SingleWordWithFootnotes> words,
            ILogger logger,
            List<TrainingFeature> trainingFeaturesOut)
        {
            // Node for this verse - we use it a lot
            KnowledgeGraphNodeId thisVerseNode = FeatureToNodeMapping.ScriptureVerse(
                currentVerse.Canon,
                currentVerse.Book,
                currentVerse.Chapter,
                currentVerse.Verse);

            // Common word and ngram level features associated with this verse entity
            EnglishWordFeatureExtractor.ExtractTrainingFeatures(rawText, trainingFeaturesOut, thisVerseNode);

            // Relationship between this verse and the previous one (if present)
            if (currentVerse.Verse > 1)
            {
                trainingFeaturesOut.Add(new TrainingFeature(
                    thisVerseNode,
                    FeatureToNodeMapping.ScriptureVerse(
                        currentVerse.Canon,
                        currentVerse.Book,
                        currentVerse.Chapter,
                        currentVerse.Verse - 1),
                    TrainingFeatureType.ParagraphAssociation));
            }

            // Relationship between this verse and the book it's in
            trainingFeaturesOut.Add(new TrainingFeature(
                thisVerseNode,
                FeatureToNodeMapping.ScriptureChapter(
                    currentVerse.Canon,
                    currentVerse.Book,
                    currentVerse.Chapter),
                TrainingFeatureType.BookAssociation));

            // Cross-references between this verse and other verses based on footnotes
            foreach (var word in words)
            {
                if (word.Footnote != null)
                {
                    foreach (var scriptureRef in word.Footnote.ScriptureReferences)
                    {
                       KnowledgeGraphNodeId refNodeId = LdsDotOrgCommonParsers.ConvertScriptureRefToNodeId(scriptureRef);
                        trainingFeaturesOut.Add(new TrainingFeature(
                            thisVerseNode,
                            refNodeId,
                            TrainingFeatureType.EntityReference));
                        foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(word.Word))
                        {
                            trainingFeaturesOut.Add(new TrainingFeature(
                                ngram,
                                refNodeId,
                                TrainingFeatureType.WordDesignation));
                        }
                    }
                }
            }
        }
    }
}
