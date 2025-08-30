using Durandal.Common.Logger;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class BibleDictionaryFeatureExtractor
    {
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/scriptures\\/bd\\/(.+?)(?:\\?|$)");

        private static readonly Regex ParagraphParser = new Regex("<p[^>]+?id=\\\"p\\d+\\\".*?>([\\w\\W]+?)<\\/p>");

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
                KnowledgeGraphNodeId dictionaryNode = FeatureToNodeMapping.BibleDictionaryTopic(topic);

                List<ScriptureReference> references = new List<ScriptureReference>();
                foreach (Match entryMatch in ParagraphParser.Matches(htmlPage))
                {
                    string rawText = entryMatch.Groups[1].Value;
                    string wordBreakerText = StringUtils.RegexRemove(ScriptureRefRemover, rawText);
                    wordBreakerText = StringUtils.RegexRemove(HtmlTagRemover, wordBreakerText);
                    
                    // for parsing the document later
                    string sanitizedText = StringUtils.RegexRemove(HtmlTagRemover, rawText);
                    List<KnowledgeGraphNodeId> ngrams = EnglishWordFeatureExtractor.ExtractNGrams(wordBreakerText).ToList();

                    foreach (KnowledgeGraphNodeId ngram in ngrams)
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            ngram,
                            dictionaryNode,
                            ngram.Type == KnowledgeGraphNodeType.NGram ? TrainingFeatureType.NgramAssociation : TrainingFeatureType.WordAssociation));
                    }

                    references.Clear();
                    LdsDotOrgCommonParsers.ParseAllScriptureReferences(rawText, references, logger);
                    foreach (ScriptureReference scriptureRef in references)
                    {
                        KnowledgeGraphNodeId refNodeId = LdsDotOrgCommonParsers.ConvertScriptureRefToNodeId(scriptureRef);

                        trainingFeaturesOut.Add(new TrainingFeature(
                            dictionaryNode,
                            refNodeId,
                            TrainingFeatureType.EntityReference));

                        // don't associate the entire paragraph's ngrams with the scripture references, that's way too many
                        //foreach (KnowledgeGraphNodeId ngram in ngrams)
                        //{
                        //    trainingFeaturesOut.Add(new TrainingFeature(
                        //        ngram,
                        //        refNodeId,
                        //        ngram.Type == KnowledgeGraphNodeType.NGram ? TrainingFeatureType.NgramAssociation : TrainingFeatureType.WordAssociation));
                        //}
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
