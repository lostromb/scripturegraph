using Durandal.Common.Logger;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using System.Net;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class TripleIndexFeatureExtractor
    {
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/scriptures\\/triple-index\\/(.+?)(?:\\?|$)");

        private static readonly Regex PrintableTitleParser = new Regex("<h1.*?>(.+?)<\\/h1>");

        private static readonly Regex BracketRemover = new Regex("\\s*([\\[\\(]).+?([\\]\\)])");

        private static readonly Regex SuperscriptNumberRemover = new Regex("\\d+");
        private static readonly Regex EmDashRemover = new Regex("—.+");

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
                string topic = urlParse.Groups[1].Value;
                KnowledgeGraphNodeId tripleIndexNode = FeatureToNodeMapping.GuideToScripturesTopic(topic);

                List<ScriptureReference> references = new List<ScriptureReference>();
                Match titleMatch = LdsDotOrgCommonParsers.IndexTitleParser.Match(htmlPage);
                if (titleMatch.Success)
                {
                    // There's a "see also" heading, just parse references and ignore text
                    LdsDotOrgCommonParsers.ParseAllScriptureReferences(titleMatch.Groups[1].Value, references, logger);
                    foreach (ScriptureReference scriptureRef in references)
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            tripleIndexNode,
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
                            tripleIndexNode,
                            ngram.Type == KnowledgeGraphNodeType.NGram ? TrainingFeatureType.NgramAssociation : TrainingFeatureType.WordAssociation));
                    }

                    references.Clear();
                    LdsDotOrgCommonParsers.ParseAllScriptureReferences(rawText, references, logger);
                    foreach (ScriptureReference scriptureRef in references)
                    {
                        KnowledgeGraphNodeId refNodeId = LdsDotOrgCommonParsers.ConvertScriptureRefToNodeId(scriptureRef);

                        trainingFeaturesOut.Add(new TrainingFeature(
                            tripleIndexNode,
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

        public static void ExtractSearchIndexFeatures(string htmlPage, Uri pageUrl, ILogger logger, List<TrainingFeature> trainingFeaturesOut, EntityNameIndex nameIndex)
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

                KnowledgeGraphNodeId thisNode = FeatureToNodeMapping.TripleIndexTopic(topicId);

                string prettyTopicString = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, titleParse.Groups[1].Value);
                prettyTopicString = StringUtils.RegexRemove(SuperscriptNumberRemover, prettyTopicString); // remove all numbers - this is to handle things like "aaron2"

                // remove the clarifier after the title (if any), such as "brother of Moses"
                string topicStringWithoutClarification = StringUtils.RegexRemove(EmDashRemover, prettyTopicString);

                // Include the clarifier in the full title (for search retrieval) only if it's not terribly long. see "Large Plates of Nephi" for an example of this
                nameIndex.Mapping[thisNode] = prettyTopicString.Length > 40 ? topicStringWithoutClarification : prettyTopicString;

                prettyTopicString = StringUtils.RegexRemove(BracketRemover, prettyTopicString); // remove "[verb]", "[noun]" etc from titles
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(prettyTopicString))
                {
                    trainingFeaturesOut.Add(new TrainingFeature(
                        thisNode,
                        ngram,
                        TrainingFeatureType.WordDesignation));
                }

                // This comma inversion loop is different from normal since we already extracted non-inversion features in the loop above,
                // so this only runs if there is a possible inversion in just the short article title, such as "Ammon, Children of"
                while (EnglishWordFeatureExtractor.PerformCommaInversion(ref topicStringWithoutClarification))
                {
                    // Extract ngrams from the topic title and associate it with the topic
                    foreach (var ngram in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(topicStringWithoutClarification))
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            thisNode,
                            ngram,
                            TrainingFeatureType.WordDesignation));
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
