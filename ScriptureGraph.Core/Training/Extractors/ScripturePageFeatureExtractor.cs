using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Graph;
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
                        if (string.Equals(scriptureRef.Canon, "tg", StringComparison.Ordinal))
                        {
                            // Topical guide topic
                            KnowledgeGraphNodeId refNodeId = FeatureToNodeMapping.TopicalGuideKeyword(scriptureRef.Book);
                            trainingFeaturesOut.Add(new TrainingFeature(
                                thisVerseNode,
                                refNodeId,
                                TrainingFeatureType.EntityReference));
                            trainingFeaturesOut.Add(new TrainingFeature(
                                FeatureToNodeMapping.Word(word.Word, LanguageCode.ENGLISH),
                                refNodeId,
                                TrainingFeatureType.WordDesignation));
                        }
                        else if (string.Equals(scriptureRef.Canon, "bd", StringComparison.Ordinal))
                        {
                            // Bible dictionary topic
                            KnowledgeGraphNodeId refNodeId = FeatureToNodeMapping.BibleDictionaryTopic(scriptureRef.Book);
                            trainingFeaturesOut.Add(new TrainingFeature(
                                thisVerseNode,
                                refNodeId,
                                TrainingFeatureType.EntityReference));
                            trainingFeaturesOut.Add(new TrainingFeature(
                                FeatureToNodeMapping.Word(word.Word, LanguageCode.ENGLISH),
                                refNodeId,
                                TrainingFeatureType.WordDesignation));
                        }
                        else if (string.Equals(scriptureRef.Canon, "gs", StringComparison.Ordinal))
                        {
                            // Bible dictionary topic
                            KnowledgeGraphNodeId refNodeId = FeatureToNodeMapping.GuideToScripturesTopic(scriptureRef.Book);
                            trainingFeaturesOut.Add(new TrainingFeature(
                                thisVerseNode,
                                refNodeId,
                                TrainingFeatureType.EntityReference));
                            trainingFeaturesOut.Add(new TrainingFeature(
                                FeatureToNodeMapping.Word(word.Word, LanguageCode.ENGLISH),
                                refNodeId,
                                TrainingFeatureType.WordDesignation));
                        }
                        else
                        {
                            KnowledgeGraphNodeId refNodeId;
                            if (scriptureRef.Chapter.HasValue &&
                                scriptureRef.Verse.HasValue)
                            {
                                // Regular scripture ref
                                refNodeId = FeatureToNodeMapping.ScriptureVerse(
                                    scriptureRef.Canon,
                                    scriptureRef.Book,
                                    scriptureRef.Chapter.Value,
                                    scriptureRef.Verse.Value);
                            }
                            else if (scriptureRef.Chapter.HasValue)
                            {
                                // Reference to an entire chapter
                                refNodeId = FeatureToNodeMapping.ScriptureChapter(
                                    scriptureRef.Canon,
                                    scriptureRef.Book,
                                    scriptureRef.Chapter.Value);
                            }
                            else
                            {
                                // Reference to an entire book
                                refNodeId = FeatureToNodeMapping.ScriptureBook(
                                    scriptureRef.Canon,
                                    scriptureRef.Book);
                            }

                            trainingFeaturesOut.Add(new TrainingFeature(
                                thisVerseNode,
                                refNodeId,
                                TrainingFeatureType.EntityReference));
                            trainingFeaturesOut.Add(new TrainingFeature(
                                FeatureToNodeMapping.Word(word.Word, LanguageCode.ENGLISH),
                                refNodeId,
                                TrainingFeatureType.WordDesignation));
                        }
                    }
                }
            }
        }
    }
}
