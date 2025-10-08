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
    public class ScripturePageFeatureExtractorNew
    {
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/scriptures\\/(.+?)\\/(.+?)\\/(\\d+)");

        public static ScriptureChapterDocument? ParseDocument(string htmlPage, Uri pageUrl, ILogger logger)
        {
            try
            {
                DocumentParseModel? parseResult = ParseInternal(htmlPage, pageUrl, logger);
                if (parseResult == null)
                {
                    return null;
                }

                ScriptureChapterDocument returnVal = new ScriptureChapterDocument()
                {
                    DocumentType = GospelDocumentType.ScriptureChapter,
                    Canon = parseResult.Canon,
                    Book = parseResult.Book,
                    Chapter = parseResult.Chapter,
                    Language = LanguageCode.ENGLISH,
                    Paragraphs = new List<GospelParagraph>(),
                    DocumentEntityId = FeatureToNodeMapping.ScriptureChapter(parseResult.Book, parseResult.Chapter),
                    Prev = ScriptureMetadata.GetPrevChapter(parseResult.Book, parseResult.Chapter),
                    Next = ScriptureMetadata.GetNextChapter(parseResult.Book, parseResult.Chapter)
                };

                foreach (var paragraph in parseResult.Titles)
                {
                    returnVal.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = paragraph.ParaEntityId,
                        Class = StandardizeClass(paragraph.Class, paragraph.Text),
                        Text = paragraph.Text
                    });
                }

                foreach (var paragraph in parseResult.StudySummaries)
                {
                    returnVal.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = paragraph.ParaEntityId,
                        Class = StandardizeClass(paragraph.Class, paragraph.Text),
                        Text = paragraph.Text
                    });
                }

                foreach (var paragraph in parseResult.Verses)
                {
                    returnVal.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = paragraph.ParaEntityId,
                        Class = GospelParagraphClass.Verse,
                        Text = paragraph.Text
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

        private static readonly IReadOnlyDictionary<string, GospelParagraphClass> CLASS_MAPPING = new Dictionary<string, GospelParagraphClass>(StringComparer.OrdinalIgnoreCase)
        {
            {"title-number", GospelParagraphClass.SubHeader },
            {"study-intro", GospelParagraphClass.StudySummary },
            {"study-summary", GospelParagraphClass.StudySummary },
        };

        private static GospelParagraphClass StandardizeClass(string rawClass, string debugText)
        {

            GospelParagraphClass returnVal;
            if (CLASS_MAPPING.TryGetValue(rawClass, out returnVal))
            {
                return returnVal;
            }

            Console.WriteLine("Found a new class " + rawClass + " " + debugText);
            return GospelParagraphClass.Default;
        }

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
                    Canon = canon,
                    Book = book,
                    Chapter = chapter,
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
                        //Console.WriteLine($"ID {footnotePId}");
                        

                        string content = currentNav.CurrentNode.InnerHtml;
                        content = WebUtility.HtmlDecode(content);

                        // Extract all scripture references from links and text
                        scriptureRefs.Clear();
                        LdsDotOrgCommonParsers.ParseAllScriptureReferences(content, scriptureRefs, logger);
                        HashSet<KnowledgeGraphNodeId> refNodeIds = new HashSet<KnowledgeGraphNodeId>();
                        foreach (ScriptureReference scriptureRef in scriptureRefs)
                        {
                            refNodeIds.Add(scriptureRef.ToNodeId());
                        }

                        footnoteReferences[footnotePId] = refNodeIds;
                        //foreach (var node in refNodeIds)
                        //{
                        //    Console.WriteLine(node);
                        //}
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
                    //Console.WriteLine($"ID {paraIdRaw} CLASS {paraClassRaw}");
                    //Console.WriteLine(currentNav.CurrentNode.InnerHtml.Trim());

                    string content = currentNav.CurrentNode.InnerHtml;
                    content = LdsDotOrgCommonParsers.RemoveNbsp(content);
                    content = content.Trim();

                    var parsedHtml = LdsDotOrgCommonParsers.ParseAndFormatHtmlFragmentNew(content, logger);
                    List<FootnoteReference> footnoteRefs = new List<FootnoteReference>();
                    foreach (var inlineRef in parsedHtml.Links)
                    {
                        foreach (ScriptureReference scriptureRef in LdsDotOrgCommonParsers.ParseAllScriptureReferences(inlineRef.Item2, logger))
                        {
                            // Ignore references within the same book and chapter (e.g. hyperlinks found in long study summaries, see d&C 76 for example)
                            if (!string.Equals(book, scriptureRef.Book, StringComparison.OrdinalIgnoreCase) &&
                                chapter != scriptureRef.Chapter.GetValueOrDefault(-1))
                            {
                                //Console.WriteLine($"Links to {inlineRef.Item2}");
                                footnoteRefs.Add(new FootnoteReference()
                                {
                                    TargetNodeId = scriptureRef.ToNodeId(),
                                    ScriptureRef = scriptureRef,
                                    ReferenceSpan = inlineRef.Item1
                                });
                            }
                        }
                    }

                    Paragraph para = new Paragraph()
                    {
                        ParaEntityId = FeatureToNodeMapping.ScriptureSupplementalParagraph(book, chapter, paraIdRaw),
                        Class = paraClassRaw,
                        Id = paraIdRaw,
                        Text = parsedHtml.TextWithInlineFormatTags,
                        References = footnoteRefs
                    };

                    if (string.Equals(paraClassRaw, "title-number", StringComparison.OrdinalIgnoreCase))
                    {
                        para.Text = para.Text.Trim();
                        returnVal.Titles.Add(para);
                    }
                    else if (string.Equals(paraClassRaw, "study-summary", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(paraClassRaw, "study-intro", StringComparison.OrdinalIgnoreCase))
                    {
                        returnVal.StudySummaries.Add(para);
                    }

                    //Console.WriteLine(para.Text);
                }

                // Parse verses
                navigator.MoveToRoot();
                iter = navigator.Select("//p[@class=\"verse\"]");
                while (iter.MoveNext())
                {
                    HtmlNodeNavigator currentNav = iter.Current as HtmlNodeNavigator;
                    if (currentNav == null)
                    {
                        continue;
                    }

                    string paraIdRaw = currentNav.GetAttribute("id", string.Empty);
                    string paraClassRaw = currentNav.GetAttribute("class", string.Empty);
                    //Console.WriteLine($"ID {paraIdRaw} CLASS {paraClassRaw}");
                    //Console.WriteLine(currentNav.CurrentNode.InnerHtml.Trim());

                    int verseNum = int.Parse(paraIdRaw.TrimStart('p'));

                    string content = currentNav.CurrentNode.InnerHtml;
                    content = LdsDotOrgCommonParsers.RemoveVerseNumberSpans(content);
                    content = LdsDotOrgCommonParsers.RemoveNbsp(content);
                    content = content.Trim();
                    var parsedHtml = LdsDotOrgCommonParsers.ParseAndFormatHtmlFragmentNew(content, logger);
                    List<FootnoteReference> footnoteRefs = new List<FootnoteReference>();
                    foreach (var inlineRef in parsedHtml.Links)
                    {
                        Uri uri = new Uri("scripture://" + inlineRef.Item2, UriKind.Absolute);
                        string footnote = uri.Fragment.TrimStart('#');
                        ISet<KnowledgeGraphNodeId>? footnoteTargets;
                        if (footnote.StartsWith("note"))
                        {
                            if (footnoteReferences.TryGetValue(footnote, out footnoteTargets))
                            {
                                //Console.WriteLine($"Has footnote {footnote}");
                                foreach (KnowledgeGraphNodeId nodeId in footnoteTargets)
                                {
                                    footnoteRefs.Add(new FootnoteReference()
                                    {
                                        TargetNodeId = nodeId,
                                        ScriptureRef = null,
                                        ReferenceSpan = inlineRef.Item1
                                    });
                                }
                            }
                            else
                            {
                                logger.Log($"Verse {verseNum} has reference to unknown footnote {footnote}, ignoring...", LogLevel.Wrn);
                            }
                        }
                        else
                        {
                            // This could be an href that links directly to another scripture verse
                            // example D&C 76:15
                            foreach (ScriptureReference scriptureRef in LdsDotOrgCommonParsers.ParseAllScriptureReferences(inlineRef.Item2, logger))
                            {
                                // Ignore references within the same book and chapter (e.g. hyperlinks found in long study summaries, see d&C 76 for example)
                                if (!string.Equals(book, scriptureRef.Book, StringComparison.OrdinalIgnoreCase) &&
                                    chapter != scriptureRef.Chapter.GetValueOrDefault(-1))
                                {
                                    //Console.WriteLine($"Links to {scriptureRef}");
                                    footnoteRefs.Add(new FootnoteReference()
                                    {
                                        TargetNodeId = scriptureRef.ToNodeId(),
                                        ScriptureRef = scriptureRef,
                                        ReferenceSpan = inlineRef.Item1
                                    });
                                }
                            }
                        }
                    }

                    //Console.WriteLine(parsedHtml.TextWithInlineFormatTags);
                    Paragraph para = new Paragraph()
                    {
                        ParaEntityId = FeatureToNodeMapping.ScriptureVerse(book, chapter, verseNum),
                        Class = paraClassRaw,
                        Id = paraIdRaw,
                        Text = parsedHtml.TextWithInlineFormatTags,
                        References = footnoteRefs
                    };

                    returnVal.Verses.Add(para);
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
            public required string Canon;
            public required string Book;
            public required int Chapter;
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
            public required List<FootnoteReference> References;

            public override string ToString()
            {
                return Text;
            }
        }

        public class FootnoteReference
        {
            public required KnowledgeGraphNodeId TargetNodeId;
            public ScriptureReference? ScriptureRef;
            public IntRange? ReferenceSpan;
        }
    }
}
