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

                Dictionary<string, Footnote> footnoteReferences = ParseFootnotes(navigator, chapter, pageUrl.AbsolutePath, logger);
                Dictionary<string, Note> notes = ParseNotes(navigator, chapter, logger);

                // Body paragraphs

                return returnVal;
            }
            catch (Exception e)
            {
                logger.Log(e);
                return null;
            }
        }

        private static Dictionary<string, Footnote> ParseFootnotes(HtmlNodeNavigator navigator, int chapter, string currentUrl, ILogger logger)
        {
            Dictionary<string, Footnote> footnoteReferences = new Dictionary<string, Footnote>();
            navigator.MoveToRoot();
            XPathNodeIterator iter = navigator.Select("//footer[@class=\"notes\"]//p[@id]");
            Regex matcherForThisChapterFooters = new Regex(Regex.Escape(currentUrl) + ".*?#(p\\d+)[\"$]");
            while (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
            {
                string footnotePId = currentNav.GetAttribute("id", string.Empty);
                if (footnotePId.Length < 3)
                {
                    logger.Log("Invalid footnote id " + footnotePId, LogLevel.Wrn);
                    continue;
                }

                if (footnotePId.Contains('_'))
                {
                    footnotePId = footnotePId.Substring(0, footnotePId.LastIndexOf('_')); // trim the _p1 from the end
                }

                Console.WriteLine($"ID {footnotePId}");

                string content = currentNav.CurrentNode.InnerHtml;
                content = WebUtility.HtmlDecode(content);

                // Extract all scripture references from links and text in footnotes
                HashSet<KnowledgeGraphNodeId> refNodeIds = new HashSet<KnowledgeGraphNodeId>();
                HashSet<string> noteRefs = new HashSet<string>();
                foreach (OmniParserOutput parseOutput in OmniParser.ParseHtml(content, logger, LanguageCode.ENGLISH))
                {
                    KnowledgeGraphNodeId refId = parseOutput.Node;
                    if (!refNodeIds.Contains(refId))
                    {
                        refNodeIds.Add(refId);
                    }
                }

                // And also look for references to "Note 1", etc.
                // We replace this with an in-line text link in addition to the entity relationship
                foreach (Match thisChapterFragmentLink in matcherForThisChapterFooters.Matches(content))
                {
                    string noteParagraphId = thisChapterFragmentLink.Groups[1].Value;
                    if (!noteRefs.Contains(noteParagraphId))
                    {
                        Console.WriteLine($"Reference to fragment {noteParagraphId}");
                        noteRefs.Add(noteParagraphId);
                    }

                    KnowledgeGraphNodeId refId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, chapter.ToString(), noteParagraphId);
                    if (!refNodeIds.Contains(refId))
                    {
                        refNodeIds.Add(refId);
                    }
                }

                footnoteReferences[footnotePId] = new Footnote()
                {
                    NoteId = footnotePId,
                    NoteContent = content,
                    EntityRefs = refNodeIds,
                    NoteRefs = noteRefs
                };

                foreach (var node in refNodeIds)
                {
                    Console.WriteLine(node);
                }
            }

            return footnoteReferences;
        }

        private static Dictionary<string, Note> ParseNotes(HtmlNodeNavigator navigator, int chapter, ILogger logger)
        {
            Dictionary<string, Note> returnVal = new Dictionary<string, Note>();
            navigator.MoveToRoot();
            XPathNodeIterator iter = navigator.Select("//*[@id=\"main\"]/div[@class=\"body\"]/div[@class=\"body-block\"]/section[2]/ol/li");
            while (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
            {
                XPathNodeIterator iter2 = currentNav.Select("p[@id]");
                while (iter2.MoveNext() && iter2.Current is HtmlNodeNavigator currentNav2)
                {
                    string notePId = currentNav2.GetAttribute("id", string.Empty);
                    KnowledgeGraphNodeId thisParaId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, chapter.ToString(), notePId);
                    string noteParaContent = WebUtility.HtmlDecode(currentNav2.CurrentNode.InnerHtml);
                    var htmlFragmentParse = LdsDotOrgCommonParsers.ParseAndFormatHtmlFragmentNew(noteParaContent, logger);
                    HashSet<KnowledgeGraphNodeId> nodeReferenceInThisNoteParagraph = new HashSet<KnowledgeGraphNodeId>();
                    foreach (var link in htmlFragmentParse.Links)
                    {
                        foreach (var omniParseOutput in OmniParser.ParseHtml(link.Item2, logger, LanguageCode.ENGLISH))
                        {
                            if (!nodeReferenceInThisNoteParagraph.Contains(omniParseOutput.Node))
                            {
                                nodeReferenceInThisNoteParagraph.Add(omniParseOutput.Node);
                            }
                        }
                    }

                    returnVal[notePId] = new Note()
                    {
                        NoteParaId = notePId,
                        NoteContent = htmlFragmentParse.TextWithInlineFormatTags,
                        EntityRefs = nodeReferenceInThisNoteParagraph
                    };
                }
            }

            return returnVal;
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

        private class Footnote
        {
            public string NoteId;
            public string NoteContent;
            public ISet<KnowledgeGraphNodeId> EntityRefs;
            public ISet<string> NoteRefs;
        }

        private class Note
        {
            public string NoteParaId;
            public string NoteContent;
            public ISet<KnowledgeGraphNodeId> EntityRefs;
        }

        private class FootnoteReference
        {
            public required KnowledgeGraphNodeId TargetNodeId;
            public ScriptureReference? ScriptureRef;
            public IntRange? ReferenceSpan;
        }
    }
}
