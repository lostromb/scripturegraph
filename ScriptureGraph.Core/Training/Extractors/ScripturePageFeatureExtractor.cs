using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using Durandal.Common.Parsers;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas.Documents;
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
                htmlPage = LdsDotOrgCommonParsers.RemoveNbsp(htmlPage);
                string canon = urlParse.Groups[1].Value;
                string book = urlParse.Groups[2].Value;
                int chapter = int.Parse(urlParse.Groups[3].Value);

                List<StructuredVerse> verses = LdsDotOrgCommonParsers.ParseVerses(canon, book, chapter, htmlPage);
                Dictionary<string, StructuredFootnote> footnotes = LdsDotOrgCommonParsers.ParseFootnotesFromPage(htmlPage, logger);

                // Now restructure each verse into a series of words with correlated footnotes
                foreach (StructuredVerse verse in verses)
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
                htmlPage = LdsDotOrgCommonParsers.RemoveNbsp(htmlPage);

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
                    DocumentEntityId = FeatureToNodeMapping.ScriptureChapter(book, chapter),
                    Prev = ScriptureMetadata.GetPrevChapter(book, chapter),
                    Next = ScriptureMetadata.GetNextChapter(book, chapter)
                };

                List<StructuredVerse> verses = LdsDotOrgCommonParsers.ParseVerses(canon, book, chapter, htmlPage);
                
                foreach (var verse in verses)
                {
                    // Is this a numerical verse?
                    int verseNum;
                    if (verse.ParagraphId.StartsWith('p') &&
                        int.TryParse(verse.ParagraphId.AsSpan(1), out verseNum))
                    {
                        returnVal.Paragraphs.Add(new GospelParagraph()
                        {
                            ParagraphEntityId = FeatureToNodeMapping.ScriptureVerse(book, chapter, verseNum),
                            Text = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, verse.Text),
                            Class = GospelParagraphClass.Verse
                        });
                    }
                    else
                    {
                        returnVal.Paragraphs.Add(new GospelParagraph()
                        {
                            ParagraphEntityId = FeatureToNodeMapping.ScriptureSupplementalParagraph(book, chapter, verse.ParagraphId),
                            Text = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, verse.Text),
                            Class = GospelParagraphClass.Verse
                        });
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
                    book),
                FeatureToNodeMapping.ScriptureChapter(
                    book,
                    chapter),
                TrainingFeatureType.BookAssociation));

            // And the previous chapter, if applicable
            if (chapter > 1)
            {
                trainingFeaturesOut.Add(new TrainingFeature(
                    FeatureToNodeMapping.ScriptureChapter(
                        book,
                        chapter),
                    FeatureToNodeMapping.ScriptureChapter(
                        book,
                        chapter - 1),
                    TrainingFeatureType.BookAssociation));
            }
        }

        private static void ExtractFeaturesFromSingleVerse(
            StructuredVerse currentParagraph,
            string rawText,
            List<SingleWordWithFootnotes> words,
            ILogger logger,
            List<TrainingFeature> trainingFeaturesOut)
        {
            // Node for this verse - we use it a lot
            KnowledgeGraphNodeId thisVerseNode;

            // Is this paragraph an actual numerical verse?
            int verseNum;
            if (currentParagraph.ParagraphId.StartsWith('p') &&
                int.TryParse(currentParagraph.ParagraphId.AsSpan(1), out verseNum))
            {
                thisVerseNode = FeatureToNodeMapping.ScriptureVerse(
                    currentParagraph.Book,
                    currentParagraph.Chapter,
                    verseNum);

                // Common word and ngram level features associated with this verse entity
                EnglishWordFeatureExtractor.ExtractTrainingFeatures(rawText, trainingFeaturesOut, thisVerseNode);

                // Relationship between this verse and the previous one (if present)
                if (verseNum > 1)
                {
                    trainingFeaturesOut.Add(new TrainingFeature(
                        thisVerseNode,
                        FeatureToNodeMapping.ScriptureVerse(
                            currentParagraph.Book,
                            currentParagraph.Chapter,
                            verseNum - 1),
                        TrainingFeatureType.ParagraphAssociation));
                }
            }
            else
            {
                thisVerseNode = FeatureToNodeMapping.ScriptureSupplementalParagraph(
                    currentParagraph.Book,
                    currentParagraph.Chapter,
                    currentParagraph.ParagraphId);

                // Common word and ngram level features associated with this verse entity
                EnglishWordFeatureExtractor.ExtractTrainingFeatures(rawText, trainingFeaturesOut, thisVerseNode);
            }

            // Relationship between this verse and the book it's in
            trainingFeaturesOut.Add(new TrainingFeature(
                thisVerseNode,
                FeatureToNodeMapping.ScriptureChapter(
                    currentParagraph.Book,
                    currentParagraph.Chapter),
                TrainingFeatureType.BookAssociation));

            // Cross-references between this verse and other verses based on footnotes
            foreach (var word in words)
            {
                if (word.Footnote != null)
                {
                    foreach (var scriptureRef in word.Footnote.ScriptureReferences)
                    {
                       KnowledgeGraphNodeId refNodeId = scriptureRef.ToNodeId();
                        trainingFeaturesOut.Add(new TrainingFeature(
                            thisVerseNode,
                            refNodeId,
                            scriptureRef.LowEmphasis ? TrainingFeatureType.ScriptureReferenceWithoutEmphasis : TrainingFeatureType.ScriptureReference));
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
