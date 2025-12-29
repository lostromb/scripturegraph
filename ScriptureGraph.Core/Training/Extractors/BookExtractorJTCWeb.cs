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
    public class BookExtractorJTCWeb
    {
        private static readonly string BOOK_ID = "jtc";
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/manual\\/jesus-the-christ\\/chapter-(\\d+)");

        public static void ExtractFeatures(string htmlPage, Uri pageUrl, ILogger logger, Action<TrainingFeature> trainingFeaturesOut)
        {
            try
            {
                
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
                foreach (var ngram in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(parseResult.ChapterName))
                {
                    trainingFeaturesOut(new TrainingFeature(
                        parseResult.DocumentEntityId,
                        ngram,
                        TrainingFeatureType.WordDesignation));
                }

                nameIndex.EntityIdToPlainName[parseResult.DocumentEntityId] = parseResult.ChapterName;
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        public static BookChapterDocument? ParseDocument(string htmlPage, Uri pageUrl, ILogger logger)
        {
            try
            {
                ParseInternal(htmlPage, pageUrl, logger);
                return null;
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
                Match urlParse = UrlPathParser.Match(pageUrl.AbsolutePath);
                if (!urlParse.Success)
                {
                    logger.Log("Failed to parse URL", LogLevel.Err);
                    return null;
                }

                int chapter = int.Parse(urlParse.Groups[1].Value);

                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(htmlPage);

                string chapterName = LdsDotOrgCommonParsers.ParseAndFormatHtmlFragmentNew(GetSingleInnerHtml(html, "//h1[@id=\"title1\"]"), logger).TextWithInlineFormatTags;
                DocumentParseModel returnVal = new DocumentParseModel()
                {
                    DocumentEntityId = FeatureToNodeMapping.BookChapter(BOOK_ID, chapter.ToString()),
                    ChapterName = chapterName,
                    ChapterNum = chapter
                };

                XPathNodeIterator iter;
                HtmlNodeNavigator navigator = html.CreateNavigator() as HtmlNodeNavigator;

                // Parse footnotes
                Dictionary<string, ISet<KnowledgeGraphNodeId>> footnoteReferences = new Dictionary<string, ISet<KnowledgeGraphNodeId>>();

                HashSet<ScriptureReference> scriptureRefs = new HashSet<ScriptureReference>();
                navigator.MoveToRoot();
                iter = navigator.Select("//footer[@class=\"notes\"]//p[@id]");
                while (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
                {
                    string footnotePId = currentNav.GetAttribute("id", string.Empty);
                    if (footnotePId.Length < 3)
                    {
                        logger.Log("Invalid footnote id " + footnotePId, LogLevel.Wrn);
                        continue;
                    }

                    footnotePId = footnotePId.Substring(0, footnotePId.Length - 3); // trim the _p1 from the end
                    Console.WriteLine($"ID {footnotePId}");

                    string content = currentNav.CurrentNode.InnerHtml;
                    content = WebUtility.HtmlDecode(content);

                    // Extract all scripture references from links and text
                    scriptureRefs.Clear();
                    LdsDotOrgCommonParsers.ParseAllScriptureReferences(content, scriptureRefs, logger);
                    HashSet<KnowledgeGraphNodeId> refNodeIds = new HashSet<KnowledgeGraphNodeId>();
                    foreach (ScriptureReference scriptureRef in scriptureRefs)
                    {
                        KnowledgeGraphNodeId refId = scriptureRef.ToNodeId();
                        if (!refNodeIds.Contains(refId))
                        {
                            refNodeIds.Add(refId);
                        }
                    }

                    footnoteReferences[footnotePId] = refNodeIds;
                    foreach (var node in refNodeIds)
                    {
                        Console.WriteLine(node);
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

        private static string GetSingleInnerHtml(HtmlDocument html, string xpath)
        {
            HtmlNodeNavigator navigator = html.CreateNavigator() as HtmlNodeNavigator;
            XPathNodeIterator iter = navigator.Select(xpath);
            if (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
            {
                return currentNav.CurrentNode.InnerHtml;
            }

            return string.Empty;
        }

        private class DocumentParseModel
        {
            public required string ChapterName;
            public required int ChapterNum;
            public required KnowledgeGraphNodeId DocumentEntityId;
            public readonly List<Paragraph> BodyParagraphs = new List<Paragraph>();
            public readonly List<Paragraph> FootnoteParagraphs = new List<Paragraph>();
        }

        private class Paragraph
        {
            public required KnowledgeGraphNodeId ParaEntityId;
            public required string Text;
            public required List<FootnoteReference> References;

            public override string ToString()
            {
                return Text;
            }
        }

        private class FootnoteReference
        {
            public required KnowledgeGraphNodeId TargetNodeId;
            public ScriptureReference? ScriptureRef;
            public IntRange? ReferenceSpan;
        }
    }
}
