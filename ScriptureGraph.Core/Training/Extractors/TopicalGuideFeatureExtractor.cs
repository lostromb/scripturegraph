using Durandal.Common.Logger;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class TopicalGuideFeatureExtractor
    {
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/scriptures\\/tg\\/(.+?)(?:\\?|$)");

        private static readonly Regex TGEntryParser = new Regex("<p class=\"entry\".+?>([\\w\\W]+?)<\\/p>");

        private static readonly Regex ScriptureRefRemover = new Regex("<a class=\\\"scripture-ref\\\".+?>([\\w\\W]+?)<\\/a>");

        private static readonly Regex HtmlTagRemover = new Regex("<\\/?[a-z]+(?: [\\w\\W]+?)?>");

        public static void ExtractFeatures(string htmlPage, Uri pageUrl, ILogger logger, List<TrainingFeature> trainingFeaturesOut)
        {
            try
            {
                Match urlParse = UrlPathParser.Match(pageUrl.AbsolutePath);
                if (!urlParse.Success)
                {
                    logger.Log("Failed to parse URL", LogLevel.Err);
                    return;
                }

                string topic = urlParse.Groups[1].Value;
                KnowledgeGraphNodeId topicalGuideNode = FeatureToNodeMapping.TopicalGuideKeyword(topic);

                List<ScriptureReference> references = new List<ScriptureReference>();
                foreach (Match entryMatch in TGEntryParser.Matches(htmlPage))
                {
                    string rawText = entryMatch.Groups[1].Value;
                    string wordBreakerText = StringUtils.RegexRemove(ScriptureRefRemover, rawText);
                    wordBreakerText = StringUtils.RegexRemove(HtmlTagRemover, wordBreakerText);
                    List<KnowledgeGraphNodeId> ngrams = EnglishWordFeatureExtractor.ExtractNGrams(wordBreakerText).ToList();

                    foreach (KnowledgeGraphNodeId ngram in ngrams)
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            ngram,
                            topicalGuideNode,
                            ngram.Type == KnowledgeGraphNodeType.NGram ? TrainingFeatureType.NgramAssociation : TrainingFeatureType.WordAssociation));
                    }

                    references.Clear();
                    LdsDotOrgCommonParsers.ParseAllScriptureReferences(rawText, references, logger);
                    foreach (ScriptureReference scriptureRef in references)
                    {
                        KnowledgeGraphNodeId refNodeId;
                        if (string.Equals(scriptureRef.Canon, "tg", StringComparison.Ordinal))
                        {
                            // Topical guide topic
                            refNodeId = FeatureToNodeMapping.TopicalGuideKeyword(scriptureRef.Book);
                        }
                        else if (string.Equals(scriptureRef.Canon, "bd", StringComparison.Ordinal))
                        {
                            // Bible dictionary topic
                            refNodeId = FeatureToNodeMapping.BibleDictionaryTopic(scriptureRef.Book);
                        }
                        else if (string.Equals(scriptureRef.Canon, "gs", StringComparison.Ordinal))
                        {
                            // GS topic
                            refNodeId = FeatureToNodeMapping.GuideToScripturesTopic(scriptureRef.Book);
                        }
                        else
                        {
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
                        }

                        trainingFeaturesOut.Add(new TrainingFeature(
                            topicalGuideNode,
                            refNodeId,
                            TrainingFeatureType.EntityReference));

                        foreach (KnowledgeGraphNodeId ngram in ngrams)
                        {
                            trainingFeaturesOut.Add(new TrainingFeature(
                                ngram,
                                refNodeId,
                                ngram.Type == KnowledgeGraphNodeType.NGram ? TrainingFeatureType.NgramAssociation : TrainingFeatureType.WordAssociation));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }
    }
}
