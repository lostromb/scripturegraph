using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using Durandal.Common.Parsers;
using Durandal.Common.Utils;
using HtmlAgilityPack;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.XPath;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class ScripturePageFeatureExtractorNew
    {
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/scriptures\\/(.+?)\\/(.+?)\\/(\\d+)");

        public static DocumentParseModel? ParseInternal(string htmlPage, Uri pageUrl, ILogger logger)
        {
            try
            {
                Match urlParse = UrlPathParser.Match(pageUrl.AbsolutePath);
                if (!urlParse.Success)
                {
                    logger.Log("Failed to parse URL", LogLevel.Err);
                    return null;
                }

                string canon = urlParse.Groups[1].Value;
                string book = urlParse.Groups[2].Value;
                int chapter = int.Parse(urlParse.Groups[3].Value);

                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(htmlPage);

                DocumentParseModel returnVal = new DocumentParseModel()
                {
                    DocumentEntityId = FeatureToNodeMapping.ScriptureChapter(book, chapter)
                };

                XPathNodeIterator iter;
                HtmlNodeNavigator navigator = html.CreateNavigator() as HtmlNodeNavigator;

                // Parse footnotes
                Dictionary<string, ISet<KnowledgeGraphNodeId>> footnoteReferences = new Dictionary<string, ISet<KnowledgeGraphNodeId>>();

                HashSet<ScriptureReference> scriptureRefs = new HashSet<ScriptureReference>();
                navigator.MoveToRoot();
                iter = navigator.Select("//footer[@class=\'study-notes\']");
                while (iter.MoveNext())
                {
                    XPathNodeIterator iter2 = iter.Current.Select(".//p[@id]");
                    while (iter2.MoveNext())
                    {
                        HtmlNodeNavigator currentNav = iter2.Current as HtmlNodeNavigator;
                        if (currentNav == null)
                        {
                            continue;
                        }

                        string footnotePId = currentNav.GetAttribute("id", string.Empty);
                        if (footnotePId.Length < 3)
                        {
                            logger.Log("Invalid footnote id " + footnotePId, LogLevel.Wrn);
                            continue;
                        }

                        footnotePId = footnotePId.Substring(0, footnotePId.Length - 3); // trim the _p1 from the end
                        Console.WriteLine($"ID {footnotePId}");

                        // Extract all scripture references from links and text
                        scriptureRefs.Clear();
                        LdsDotOrgCommonParsers.ParseAllScriptureReferences(WebUtility.HtmlDecode(currentNav.CurrentNode.InnerHtml), scriptureRefs, logger);
                        HashSet<KnowledgeGraphNodeId> refNodeIds = new HashSet<KnowledgeGraphNodeId>();
                        foreach (ScriptureReference scriptureRef in scriptureRefs)
                        {
                            if (scriptureRef.Chapter.HasValue && scriptureRef.Verse.HasValue)
                            {
                                refNodeIds.Add(FeatureToNodeMapping.ScriptureVerse(scriptureRef.Book, scriptureRef.Chapter.Value, scriptureRef.Verse.Value));
                            }
                            else if (scriptureRef.Chapter.HasValue && !string.IsNullOrEmpty(scriptureRef.Paragraph))
                            {
                                refNodeIds.Add(FeatureToNodeMapping.ScriptureSupplementalParagraph(scriptureRef.Book, scriptureRef.Chapter.Value, scriptureRef.Paragraph));
                            }
                            else if (scriptureRef.Chapter.HasValue)
                            {
                                refNodeIds.Add(FeatureToNodeMapping.ScriptureChapter(scriptureRef.Book, scriptureRef.Chapter.Value));
                            }
                            else
                            {
                                refNodeIds.Add(FeatureToNodeMapping.ScriptureBook(scriptureRef.Book));
                            }
                        }

                        footnoteReferences[footnotePId] = refNodeIds;
                        foreach (var node in refNodeIds)
                        {
                            Console.WriteLine(node);
                        }
                    }
                }

                // Parse headers
                navigator.MoveToRoot();
                iter = navigator.Select("//*[@id=\"main\"]/div/header/p");
                while (iter.MoveNext())
                {
                    HtmlNodeNavigator currentNav = iter.Current as HtmlNodeNavigator;
                    if (currentNav == null)
                    {
                        continue;
                    }

                    string paraIdRaw = currentNav.GetAttribute("id", string.Empty);
                    string paraClassRaw = currentNav.GetAttribute("class", string.Empty);
                    Console.WriteLine($"ID {paraIdRaw} CLASS {paraClassRaw}");
                    Console.WriteLine(currentNav.CurrentNode.InnerHtml.Trim());

                    if (string.Equals(paraClassRaw, "title-number", StringComparison.OrdinalIgnoreCase))
                    {
                        returnVal.Titles.Add(new Paragraph()
                        {
                            ParaEntityId = FeatureToNodeMapping.ScriptureSupplementalParagraph(book, chapter, paraIdRaw),
                            Class = paraClassRaw,
                            Id = paraIdRaw,
                            Text = LdsDotOrgCommonParsers.StripAllButBoldAndItalics(currentNav.CurrentNode.InnerHtml).Trim()
                        });
                    }
                    else if (string.Equals(paraClassRaw, "study-summary", StringComparison.OrdinalIgnoreCase))
                    {
                        // TODO parse scriptures and links
                        returnVal.StudySummaries.Add(new Paragraph()
                        {
                            ParaEntityId = FeatureToNodeMapping.ScriptureSupplementalParagraph(book, chapter, paraIdRaw),
                            Class = paraClassRaw,
                            Id = paraIdRaw,
                            Text = LdsDotOrgCommonParsers.StripAllButBoldAndItalics(currentNav.CurrentNode.InnerHtml).Trim()
                        });
                    }
                    else if (string.Equals(paraClassRaw, "study-intro", StringComparison.OrdinalIgnoreCase))
                    {
                        // TODO parse scriptures and links
                        returnVal.StudySummaries.Add(new Paragraph()
                        {
                            ParaEntityId = FeatureToNodeMapping.ScriptureSupplementalParagraph(book, chapter, paraIdRaw),
                            Class = paraClassRaw,
                            Id = paraIdRaw,
                            Text = LdsDotOrgCommonParsers.StripAllButBoldAndItalics(currentNav.CurrentNode.InnerHtml).Trim()
                        });
                    }
                }

                // Parse verses

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
            public readonly List<Paragraph> Titles = new List<Paragraph>();
            public readonly List<Paragraph> StudySummaries = new List<Paragraph>();
            public readonly List<Paragraph> Verses = new List<Paragraph>();
            public readonly Dictionary<string, string> Footnotes = new Dictionary<string, string>();
        }

        public class Paragraph
        {
            public required KnowledgeGraphNodeId ParaEntityId;
            public required string Id;
            public required string Class;
            public required string Text;
            public readonly List<FootnoteReference> References = new List<FootnoteReference>();

            public override string ToString()
            {
                return Text;
            }
        }

        public class FootnoteReference
        {
            public required KnowledgeGraphNodeId ReferenceNodeId;
            public Range? ReferenceSpan;
        }
    }
}
