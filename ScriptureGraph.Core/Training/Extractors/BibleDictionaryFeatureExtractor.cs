using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using Durandal.Common.Parsers;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using System.Net;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class BibleDictionaryFeatureExtractor
    {
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/scriptures\\/bd\\/(.+?)(?:\\?|$)");

        private static readonly Regex ParagraphParser = new Regex("<p[^>]+?id=\\\"p\\d+\\\".*?>([\\w\\W]+?)<\\/p>");

        private static readonly Regex PunctuationParser = new Regex("[\\.\\?\\!]+");

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
                string topic = urlParse.Groups[1].Value;
                KnowledgeGraphNodeId dictionaryNode = FeatureToNodeMapping.BibleDictionaryTopic(topic);

                // Bible dictionary does not use "see also" headers as far as I know, so just parse paragraphs of text and scripture references.

                List<ScriptureReference> references = new List<ScriptureReference>();
                int paragraph = 0;
                foreach (Match entryMatch in ParagraphParser.Matches(htmlPage))
                {
                    paragraph++;
                    KnowledgeGraphNodeId thisParagraph = FeatureToNodeMapping.BibleDictionaryParagraph(topic, paragraph);

                    // Associate this paragraph with the entire article
                    trainingFeaturesOut.Add(new TrainingFeature(
                        thisParagraph,
                        dictionaryNode,
                        TrainingFeatureType.BookAssociation));

                    // And with the previous paragraph
                    if (paragraph > 1)
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            thisParagraph,
                            FeatureToNodeMapping.BibleDictionaryParagraph(topic, paragraph - 1),
                            TrainingFeatureType.ParagraphAssociation));
                    }

                    // Replace inline scripture references with ones that won't mess up the word breaker
                    string rawParagraph = LdsDotOrgCommonParsers.RemovePageBreakTags(entryMatch.Groups[1].Value);
                    string linkAlteredParagraph = RemoveScriptureRefAnchorTexts(rawParagraph);
                    foreach (string sentence in EnglishWordFeatureExtractor.BreakSentence(linkAlteredParagraph))
                    {
                        string wordBreakerText = StringUtils.RegexRemove(LdsDotOrgCommonParsers.ScriptureRefReplacer, sentence);
                        wordBreakerText = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, wordBreakerText);

                        // for parsing the document later
                        string sanitizedText = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, sentence);
                        List<KnowledgeGraphNodeId> ngrams = EnglishWordFeatureExtractor.ExtractNGrams(wordBreakerText).ToList();

                        // Associate ngrams in the sentence with the paragraph entity
                        foreach (KnowledgeGraphNodeId ngram in ngrams)
                        {
                            trainingFeaturesOut.Add(new TrainingFeature(
                                ngram,
                                thisParagraph,
                                ngram.Type == KnowledgeGraphNodeType.NGram ? TrainingFeatureType.NgramAssociation : TrainingFeatureType.WordAssociation));
                        }

                        references.Clear();
                        LdsDotOrgCommonParsers.ParseAllScriptureReferences(sentence, references, logger);
                        foreach (ScriptureReference scriptureRef in references)
                        {
                            KnowledgeGraphNodeId refNodeId = LdsDotOrgCommonParsers.ConvertScriptureRefToNodeId(scriptureRef);

                            // Associate all scripture references in this sentence to the paragraph entity
                            trainingFeaturesOut.Add(new TrainingFeature(
                                thisParagraph,
                                refNodeId,
                                TrainingFeatureType.EntityReference));

                            foreach (KnowledgeGraphNodeId ngram in ngrams)
                            {
                                // And associate the words within this sentence with that scripture reference as well
                                trainingFeaturesOut.Add(new TrainingFeature(
                                    ngram,
                                    refNodeId,
                                    ngram.Type == KnowledgeGraphNodeType.NGram ? TrainingFeatureType.NgramAssociation : TrainingFeatureType.WordAssociation));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        public static BibleDictionaryDocument? ParseDocument(string htmlPage, Uri pageUrl, ILogger logger)
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
                string topicId = urlParse.Groups[1].Value;

                Match titleParse = PrintableTitleParser.Match(htmlPage);
                if (!titleParse.Success)
                {
                    logger.Log("Failed to parse article title", LogLevel.Err);
                    return null;
                }

                string prettyTopicString = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, titleParse.Groups[1].Value);

                // Parse the actual correct topic from the page
                KnowledgeGraphNodeId dictEntryNodeId = FeatureToNodeMapping.BibleDictionaryTopic(topicId);

                BibleDictionaryDocument returnVal = new BibleDictionaryDocument()
                {
                    Title = prettyTopicString,
                    TopicId = topicId,
                    DocumentType = GospelDocumentType.BibleDictionaryEntry,
                    Language = LanguageCode.ENGLISH,
                    Paragraphs = new List<GospelParagraph>(),
                    DocumentEntityId = dictEntryNodeId,
                };

                int paragraph = 0;
                foreach (Match entryMatch in ParagraphParser.Matches(htmlPage))
                {
                    paragraph++;
                    string rawParagraph = entryMatch.Groups[1].Value;
                    returnVal.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = FeatureToNodeMapping.BibleDictionaryParagraph(topicId, paragraph),
                        Text = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, rawParagraph)
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

        /// <summary>
        /// Given input text with anchor tags that link to scriptures, keep the link in place while replacing the anchor text with whatever replacement you want.
        /// This can be used to preserve links while altering the underlying text.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static string RemoveScriptureRefAnchorTexts(string input)
        {
            using (PooledStringBuilder pooledSb = StringBuilderPool.Rent())
            {
                int startIndex = 0;
                Match match;
                while (startIndex < input.Length)
                {
                    match = LdsDotOrgCommonParsers.ScriptureRefReplacer.Match(input, startIndex);
                    if (match.Success)
                    {
                        if (match.Index > startIndex)
                        {
                            pooledSb.Builder.Append(input, startIndex, match.Index - startIndex);
                        }

                        pooledSb.Builder.Append(match.Groups[1].Value);
                        string anchorText = StringUtils.RegexRemove(PunctuationParser, match.Groups[2].Value);
                        pooledSb.Builder.Append(anchorText);
                        pooledSb.Builder.Append("<\\/a>");
                        startIndex = match.Index + match.Length;
                    }
                    else
                    {
                        pooledSb.Builder.Append(input, startIndex, input.Length - startIndex);
                        startIndex = input.Length;
                    }
                }

                return pooledSb.Builder.ToString();
            }
        }
    }
}
