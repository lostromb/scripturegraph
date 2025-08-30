using Durandal.Common.Logger;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class GuideToScripturesFeatureExtractor
    {
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/scriptures\\/gs\\/(.+?)(?:\\?|$)");

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
                KnowledgeGraphNodeId topicalGuideNode = FeatureToNodeMapping.GuideToScripturesTopic(topic);

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
    }
}
