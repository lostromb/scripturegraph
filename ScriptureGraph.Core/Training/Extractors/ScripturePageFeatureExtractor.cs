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
                    Prev = ScriptureMetadata.GetPrevChapter(canon, book, chapter),
                    Next = ScriptureMetadata.GetNextChapter(canon, book, chapter)
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
                return null;
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
