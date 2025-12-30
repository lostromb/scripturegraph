using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using Durandal.Common.Parsers;
using Durandal.Common.Utils;
using HtmlAgilityPack;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Schemas.Documents;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.XPath;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class ProclamationsFeatureExtractor
    {
        public static string PROC_ID_LIVING_CHRIST = "lc";
        public static string PROC_ID_THE_FAMILY = "fam";

        public static void ExtractFeatures(string htmlPage, Uri pageUrl, ILogger logger, Action<TrainingFeature> trainingFeaturesOut)
        {
            try
            {
                DocumentParseModel? parseResult = ParseInternal(htmlPage, pageUrl, logger);
                if (parseResult == null)
                {
                    //logger.Log($"Null parse result: {pageUrl}", LogLevel.Err);
                    return;
                }

                // High-level features
                // Title of the document -> Document
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(parseResult.DocumentTitle))
                {
                    trainingFeaturesOut(new TrainingFeature(
                        parseResult.DocumentEntityId,
                        ngram,
                        TrainingFeatureType.WordDesignation));
                }

                List<TrainingFeature> scratch = new List<TrainingFeature>();
                Paragraph? previousPara = null;
                foreach (Paragraph para in parseResult.Paragraphs)
                {
                    // Associate this paragraph with the entire document
                    trainingFeaturesOut(new TrainingFeature(
                        parseResult.DocumentEntityId,
                        para.ParaEntityId,
                        TrainingFeatureType.BookAssociation));

                    // And with the previous paragraph
                    if (previousPara != null)
                    {
                        trainingFeaturesOut(new TrainingFeature(
                            para.ParaEntityId,
                            previousPara.ParaEntityId,
                            TrainingFeatureType.ParagraphAssociation));
                    }

                    previousPara = para;

                    foreach (OmniParserOutput reference in para.References)
                    {
                        trainingFeaturesOut(new TrainingFeature(
                            para.ParaEntityId,
                            reference.Node,
                            reference.LowEmphasis ? TrainingFeatureType.ScriptureReferenceWithoutEmphasis : TrainingFeatureType.ScriptureReference));
                    }

                    // TODO: Proper sentence entity handling
                    List<Substring> sentences = EnglishWordFeatureExtractor.BreakSentences(para.Text).ToList();

                    foreach (Substring sentence in sentences)
                    {
                        string thisSentenceWordBreakerText = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, sentence.Text);

                        // Common word and ngram level features associated with this paragraph entity
                        scratch.Clear();
                        EnglishWordFeatureExtractor.ExtractTrainingFeatures(thisSentenceWordBreakerText, scratch, para.ParaEntityId);

                        foreach (var f in scratch)
                        {
                            trainingFeaturesOut(f);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        public static void ExtractSearchIndexFeatures(string htmlPage, Uri pageUrl, ILogger logger, Action<TrainingFeature> trainingFeaturesOut, EntityNameIndex nameIndex)
        {
            try
            {
                DocumentParseModel? parseResult = ParseInternal(htmlPage, pageUrl, logger);
                if (parseResult == null)
                {
                    //logger.Log($"Null parse result: {pageUrl}", LogLevel.Err);
                    return;
                }

                // Extract ngrams from the document title and associate it with the document
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(parseResult.DocumentTitle))
                {
                    trainingFeaturesOut(new TrainingFeature(
                        parseResult.DocumentEntityId,
                        ngram,
                        TrainingFeatureType.WordDesignation));
                }

                nameIndex.EntityIdToPlainName[parseResult.DocumentEntityId] = parseResult.DocumentTitle;
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        public static ProclamationDocument? ParseDocument(string htmlPage, Uri pageUrl, ILogger logger)
        {
            try
            {
                DocumentParseModel? parseResult = ParseInternal(htmlPage, pageUrl, logger);
                if (parseResult == null)
                {
                    //logger.Log($"Null parse result: {pageUrl}", LogLevel.Err);
                    return null;
                }

                ProclamationDocument returnVal = new ProclamationDocument()
                {
                    DocumentType = GospelDocumentType.Proclamation,
                    Title = parseResult.DocumentTitle,
                    Language = LanguageCode.ENGLISH,
                    Paragraphs = new List<GospelParagraph>(),
                    DocumentEntityId = parseResult.DocumentEntityId,
                    ProclamationId = parseResult.ProcId,
                };

                foreach (var paragraph in parseResult.Titles)
                {
                    returnVal.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = paragraph.ParaEntityId,
                        Class = paragraph.Class,
                        Text = paragraph.Text.Trim()
                    });
                }

                foreach (var paragraph in parseResult.Paragraphs)
                {
                    returnVal.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = paragraph.ParaEntityId,
                        Class = paragraph.Class,
                        Text = paragraph.Text.Trim()
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

        private static DocumentParseModel? ParseInternal(string htmlPage, Uri pageUrl, ILogger logger)
        {
            try
            {
                string documentId;
                string documentTitle;
                if (pageUrl.AbsolutePath.Contains("the-living-christ-the-testimony-of-the-apostles"))
                {
                    documentId = PROC_ID_LIVING_CHRIST;
                    documentTitle = "The Living Christ: The Testimony of the Apostles";
                }
                else if (pageUrl.AbsolutePath.Contains("the-family-a-proclamation-to-the-world"))
                {
                    documentId = PROC_ID_THE_FAMILY;
                    documentTitle = "The Family: A Proclamation to the World";
                }
                else
                {
                    return null;
                }

                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(htmlPage);

                DocumentParseModel returnVal = new DocumentParseModel()
                {
                    DocumentEntityId = FeatureToNodeMapping.Proclamation(documentId),
                    DocumentTitle = documentTitle,
                    ProcId = documentId,
                };

                XPathNodeIterator iter;
                HtmlNodeNavigator navigator = html.CreateNavigator() as HtmlNodeNavigator;

                // Parse headers
                navigator.MoveToRoot();
                iter = navigator.Select("//*[@id=\"main\"]/div[@class=\"body\"]/header//h1 | //*[@id=\"main\"]/div[@class=\"body\"]/header//p");
                while (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
                {
                    string paraIdRaw = currentNav.GetAttribute("id", string.Empty) ?? string.Empty;
                    string paraClassRaw = currentNav.GetAttribute("class", string.Empty) ?? string.Empty;

                    string content = currentNav.CurrentNode.InnerHtml;
                    content = LdsDotOrgCommonParsers.RemoveNbsp(content);
                    content = content.Trim();

                    var parsedHtml = LdsDotOrgCommonParsers.ParseAndFormatHtmlFragmentNew(content, logger);
                    GospelParagraphClass structuredClass = GospelParagraphClass.Default;
                    if (paraIdRaw.StartsWith("title", StringComparison.Ordinal))
                    {
                        structuredClass = GospelParagraphClass.Header;
                    }
                    else if (paraIdRaw.StartsWith("subtitle", StringComparison.Ordinal))
                    {
                        structuredClass = GospelParagraphClass.SubHeader;
                    }
                    else if (paraIdRaw.StartsWith("author", StringComparison.Ordinal))
                    {
                        structuredClass = GospelParagraphClass.SubHeader;
                    }

                    Paragraph para = new Paragraph()
                    {
                        ParaEntityId = FeatureToNodeMapping.ProclamationParagraph(documentId, paraIdRaw),
                        Class = structuredClass,
                        Text = parsedHtml.TextWithInlineFormatTags,
                        References = new List<OmniParserOutput>(),
                    };

                    returnVal.Titles.Add(para);
                }

                // Parse paragraphs
                navigator.MoveToRoot();
                iter = navigator.Select("//*[@id=\"main\"]/div[@class=\"body\"]/div[@class=\"body-block\"]//p[@id]");
                while (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
                {
                    string paraIdRaw = currentNav.GetAttribute("id", string.Empty) ?? string.Empty;
                    string content = currentNav.CurrentNode.InnerHtml;
                    content = LdsDotOrgCommonParsers.RemoveNbsp(content);
                    content = content.Trim();
                    var parsedHtml = LdsDotOrgCommonParsers.ParseAndFormatHtmlFragmentNew(content, logger);

                    string text = parsedHtml.TextWithInlineFormatTags;

                    if (string.Equals(documentId, PROC_ID_THE_FAMILY) && paraIdRaw.Equals("p10"))
                    {
                        text = $"<i>{text}</i>";
                    }

                    Paragraph para = new Paragraph()
                    {
                        ParaEntityId = FeatureToNodeMapping.ProclamationParagraph(documentId, paraIdRaw),
                        Text = text,
                        Class = GospelParagraphClass.Default,
                        References = OmniParser.ParseHtml(content, logger, LanguageCode.ENGLISH).ToList()
                    };

                    returnVal.Paragraphs.Add(para);
                }

                return returnVal;
            }
            catch (Exception e)
            {
                logger.Log(e);
                return null;
            }
        }

        public class DocumentParseModel
        {
            public required KnowledgeGraphNodeId DocumentEntityId;
            public required string DocumentTitle;
            public required string ProcId;
            public readonly List<Paragraph> Titles = new List<Paragraph>();
            public readonly List<Paragraph> Paragraphs = new List<Paragraph>();
        }

        public class Paragraph
        {
            public required KnowledgeGraphNodeId ParaEntityId;
            public required GospelParagraphClass Class;
            public required List<OmniParserOutput> References;
            public required string Text;

            public override string ToString()
            {
                return Text;
            }
        }
    }
}
