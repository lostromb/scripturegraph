using Durandal.Common.Logger;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using System.Net;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class TripleIndexFeatureExtractor
    {
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/scriptures\\/triple-index\\/(.+?)(?:\\?|$)");

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
    }
}
