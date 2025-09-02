using Durandal.Common.Logger;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using System.Net;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class GuideToScripturesFeatureExtractor
    {
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/scriptures\\/gs\\/(.+?)(?:\\?|$)");

        private static readonly Regex PrintableTitleParser = new Regex("<h1.*?>(.+?)<\\/h1>");

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

                htmlPage = WebUtility.HtmlDecode(htmlPage);
                htmlPage = LdsDotOrgCommonParsers.RemoveNbsp(htmlPage);
                string topicId = urlParse.Groups[1].Value;
                KnowledgeGraphNodeId topicalGuideNode = FeatureToNodeMapping.GuideToScripturesTopic(topicId);

                if (string.Equals(topicId, "introduction", StringComparison.Ordinal))
                {
                    return;
                }

                List<ScriptureReference> references = new List<ScriptureReference>();
                Match titleMatch = LdsDotOrgCommonParsers.IndexTitleParser.Match(htmlPage);
                if (titleMatch.Success)
                {
                    // There's a "see also" heading, just parse references and ignore text
                    LdsDotOrgCommonParsers.ParseAllScriptureReferences(titleMatch.Groups[1].Value, references, logger);
                    foreach (ScriptureReference scriptureRef in references)
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            topicalGuideNode,
                            LdsDotOrgCommonParsers.ConvertScriptureRefToNodeId(scriptureRef),
                            TrainingFeatureType.EntityReference));
                    }
                }
                
                foreach (Match entryMatch in LdsDotOrgCommonParsers.IndexEntryParser.Matches(htmlPage))
                {
                    string rawText = entryMatch.Groups[1].Value;
                    string wordBreakerText = StringUtils.RegexRemove(LdsDotOrgCommonParsers.ScriptureRefReplacer, rawText);
                    wordBreakerText = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, wordBreakerText);
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
                        KnowledgeGraphNodeId refNodeId = LdsDotOrgCommonParsers.ConvertScriptureRefToNodeId(scriptureRef);

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

        public static void ExtractSearchIndexFeatures(string htmlPage, Uri pageUrl, ILogger logger, List<TrainingFeature> trainingFeaturesOut)
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
                string topicId = urlParse.Groups[1].Value;

                Match titleParse = PrintableTitleParser.Match(htmlPage);
                if (!titleParse.Success)
                {
                    logger.Log("Failed to parse article title", LogLevel.Err);
                    return;
                }

                if (string.Equals(topicId, "introduction", StringComparison.Ordinal))
                {
                    return;
                }

                string prettyTopicString = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, titleParse.Groups[1].Value);

                KnowledgeGraphNodeId thisNode = FeatureToNodeMapping.GuideToScripturesTopic(topicId);

                do
                {
                    // Extract ngrams from the topic title and associate it with the topic
                    foreach (var ngram in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(prettyTopicString))
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            thisNode,
                            ngram,
                            TrainingFeatureType.WordDesignation));
                    }

                    // Also see if comma inversion changes the title. If so, loop and extract those features as well
                } while (EnglishWordFeatureExtractor.PerformCommaInversion(ref prettyTopicString));
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }
    }
}
