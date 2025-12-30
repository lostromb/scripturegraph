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
                DocumentParseModel? internalParse = ParseInternal(htmlPage, pageUrl, logger);
                if (internalParse == null)
                {
                    return null;
                }

                BookChapterDocument returnVal = new BookChapterDocument()
                {
                    BookId = BOOK_ID,
                    ChapterId = internalParse.ChapterNum.ToString(),
                    ChapterName = $"Chapter {internalParse.ChapterNum}: {internalParse.ChapterName}",
                    DocumentEntityId = internalParse.DocumentEntityId,
                    DocumentType = GospelDocumentType.GospelBookChapter,
                    Language = LanguageCode.ENGLISH,
                    Paragraphs = new List<GospelParagraph>(),
                };

                if (internalParse.ChapterNum > 1)
                {
                    returnVal.Prev = FeatureToNodeMapping.BookChapter(BOOK_ID, (internalParse.ChapterNum - 1).ToString());
                }
                if (internalParse.ChapterNum < 42)
                {
                    returnVal.Next = FeatureToNodeMapping.BookChapter(BOOK_ID, (internalParse.ChapterNum + 1).ToString());
                }

                returnVal.Paragraphs.Add(new GospelParagraph()
                {
                    ParagraphEntityId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, internalParse.ChapterNum.ToString(), "head1"),
                    Text = $"Chapter {internalParse.ChapterNum}",
                    Class = GospelParagraphClass.SubHeader,
                });

                returnVal.Paragraphs.Add(new GospelParagraph()
                {
                    ParagraphEntityId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, internalParse.ChapterNum.ToString(), "head2"),
                    Text = internalParse.ChapterName,
                    Class = GospelParagraphClass.Header,
                });

                foreach (var para in internalParse.BodyParagraphs)
                {
                    returnVal.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = para.ParaEntityId,
                        Text = para.Text,
                        Class = para.ParaClass,
                        Regions = para.Subregions.Select(s => new GospelParagraphSubregion()
                            { 
                                Range = s.Range,
                                RegionEntityId = s.EntityId!.Value
                            }).ToList()
                    });
                }

                if (internalParse.NoteBlocks.Count > 0)
                {
                    returnVal.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, internalParse.ChapterNum.ToString(), "head3"),
                        Text = $"Notes to Chapter {internalParse.ChapterNum}",
                        Class = GospelParagraphClass.SubHeader,
                    });

                    foreach (var note in internalParse.NoteBlocks)
                    {
                        returnVal.Paragraphs.Add(new GospelParagraph()
                        {
                            ParagraphEntityId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, internalParse.ChapterNum.ToString(), $"note{note.NoteNum}"),
                            Text = $"Note {note.NoteNum}",
                            Class = GospelParagraphClass.SubHeader,
                        });

                        foreach (var notePara in note.Paragraphs)
                        {
                            returnVal.Paragraphs.Add(new GospelParagraph()
                            {
                                ParagraphEntityId = notePara.ParaEntityId,
                                Text = notePara.ParaContent,
                                Class = GospelParagraphClass.Default,
                                Regions = notePara.Subregions.Select(s => new GospelParagraphSubregion()
                                    {
                                        Range = s.Range,
                                        RegionEntityId = s.EntityId!.Value
                                    }).ToList()
                            });
                        }
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
                

                HtmlNodeNavigator navigator = html.CreateNavigator() as HtmlNodeNavigator;
                Dictionary<string, Footnote> footnoteReferences = ParseFootnotes(navigator, chapter, pageUrl.AbsolutePath, logger);
                List<Note> notes = ParseNotes(navigator, chapter, logger);
                List<Paragraph> paragraphs = ParseParagraphs(navigator, chapter, pageUrl.AbsolutePath, footnoteReferences, notes, logger);

                DocumentParseModel returnVal = new DocumentParseModel()
                {
                    DocumentEntityId = FeatureToNodeMapping.BookChapter(BOOK_ID, chapter.ToString()),
                    ChapterName = chapterName,
                    ChapterNum = chapter,
                    BodyParagraphs = paragraphs,
                    NoteBlocks = notes
                };

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

        private static List<Note> ParseNotes(HtmlNodeNavigator navigator, int chapter, ILogger logger)
        {
            List<Note> returnVal = new List<Note>();
            navigator.MoveToRoot();
            int noteNum = 1;
            XPathNodeIterator sectionIter = navigator.Select("//*[@id=\"main\"]/div[@class=\"body\"]/div[@class=\"body-block\"]/section");
            while (sectionIter.MoveNext() && sectionIter.Current is HtmlNodeNavigator currentNavSection)
            {
                string sectionTitle = LdsDotOrgCommonParsers.RemoveLinksAndAnchorText(GetSingleInnerHtml(currentNavSection, "header/h2"));

                // Skip non-notes
                if (!sectionTitle.StartsWith("Notes to Chapter"))
                {
                    continue;
                }

                XPathNodeIterator noteIter = currentNavSection.Select("ol/li");
                while (noteIter.MoveNext() && noteIter.Current is HtmlNodeNavigator currentNavNote)
                {
                    Note note = new Note()
                    {
                        NoteNum = noteNum++,
                        Paragraphs = new List<NoteParagraph>()
                    };

                    returnVal.Add(note);
                    XPathNodeIterator paraIter = currentNavNote.Select("p[@id]");
                    while (paraIter.MoveNext() && paraIter.Current is HtmlNodeNavigator currentNavPara)
                    {
                        string notePId = currentNavPara.GetAttribute("id", string.Empty);
                        KnowledgeGraphNodeId thisParaId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, chapter.ToString(), notePId);
                        string noteParaContent = WebUtility.HtmlDecode(currentNavPara.CurrentNode.InnerHtml);
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


                        note.Paragraphs.Add(new NoteParagraph()
                        {
                            ParaId = notePId,
                            ParaContent = htmlFragmentParse.TextWithInlineFormatTags,
                            EntityRefs = nodeReferenceInThisNoteParagraph,
                            ParaEntityId = thisParaId,
                            Subregions = EnglishWordFeatureExtractor.BreakSentences(htmlFragmentParse.TextWithInlineFormatTags, thisParaId)
                                .Where(s => s.EntityId.HasValue).ToList()
                        });
                    }
                }
            }

            return returnVal;
        }

        private static List<Paragraph> ParseParagraphs(
            HtmlNodeNavigator navigator,
            int chapter,
            string currentUrl,
            IDictionary<string, Footnote> footnotes,
            List<Note> notes,
            ILogger logger)
        {
            List<Paragraph> returnVal = new List<Paragraph>();
            Regex matcherForThisChapterFootnoteRefs = new Regex(Regex.Escape(currentUrl) + ".*?#(note\\d+)");
            navigator.MoveToRoot();
            int sectionNum = 1;
            XPathNodeIterator sectionIter = navigator.Select("//*[@id=\"main\"]/div[@class=\"body\"]/div[@class=\"body-block\"]/section");
            while (sectionIter.MoveNext() && sectionIter.Current is HtmlNodeNavigator currentNavSection)
            {
                string sectionTitle = LdsDotOrgCommonParsers.RemoveLinksAndAnchorText(GetSingleInnerHtml(currentNavSection, "header/h2"));

                // Is this a notes section? Skip it
                if (sectionTitle.StartsWith("Notes to Chapter"))
                {
                    continue;
                }

                // Emit header for this section, if present
                if (!string.IsNullOrEmpty(sectionTitle))
                {
                    returnVal.Add(new Paragraph()
                    {
                        ParaEntityId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, chapter.ToString(), $"subhead{sectionNum++}"),
                        Text = sectionTitle,
                        ParaClass = GospelParagraphClass.SubHeader,
                        Footnotes = new List<FootnoteReference>(),
                        Subregions = new List<Substring>(),
                    });
                }

                XPathNodeIterator paraIter = currentNavSection.Select("p[@id]");
                while (paraIter.MoveNext() && paraIter.Current is HtmlNodeNavigator currentNavPara)
                {
                    string paraId = currentNavPara.GetAttribute("id", string.Empty);
                    KnowledgeGraphNodeId thisParaId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, chapter.ToString(), paraId);
                    string noteParaContent = WebUtility.HtmlDecode(currentNavPara.CurrentNode.InnerHtml);
                    var htmlFragmentParse = LdsDotOrgCommonParsers.ParseAndFormatHtmlFragmentNew(noteParaContent, logger);
                    List<FootnoteReference> footnoteRefsInThisParagraph = new List<FootnoteReference>();
                    foreach (var link in htmlFragmentParse.Links.OrderByDescending(s => s.Item1.End))
                    {
                        int linkCharsDelta = 0 - link.Item1.Length;
                        Match footnoteMatch = matcherForThisChapterFootnoteRefs.Match(link.Item2);
                        if (footnoteMatch.Success && footnotes.TryGetValue(footnoteMatch.Groups[1].Value, out Footnote? footnote))
                        {
                            Console.WriteLine(footnote.NoteId);
                            FootnoteReference thisFootnoteRef = new FootnoteReference()
                            {
                                Footnote = footnote,
                                ReferenceSpan = link.Item1,
                            };

                            Note? referencedNote = null;
                            foreach (var noteRef in footnote.NoteRefs)
                            {
                                foreach (var note in notes)
                                {
                                    foreach (var para in note.Paragraphs)
                                    {
                                        if (string.Equals(para.ParaId, noteRef, StringComparison.OrdinalIgnoreCase))
                                        {
                                            referencedNote = note;
                                            break;
                                        }
                                    }
                                }
                            }

                            // Remove footnote superscripts from the paragraph text, unless they refer to notes at the bottom,
                            // in which case replace them with an inline "[Note 1]"
                            if (referencedNote != null)
                            {
                                string replaceText = string.Format(" [Note {0}]", referencedNote.NoteNum);
                                linkCharsDelta += replaceText.Length;
                                thisFootnoteRef.ReferenceSpan = new IntRange(
                                    link.Item1.Start,
                                    link.Item1.Start + replaceText.Length);
                                htmlFragmentParse.TextWithInlineFormatTags = string.Format("{0}{1}{2}",
                                    htmlFragmentParse.TextWithInlineFormatTags.Substring(0, link.Item1.Start),
                                    replaceText,
                                    htmlFragmentParse.TextWithInlineFormatTags.Substring(link.Item1.End));
                            }
                            else
                            {
                                thisFootnoteRef.ReferenceSpan = new IntRange(
                                    link.Item1.Start,
                                    link.Item1.Start); // zero-length footnote ref since we're deleting its text
                                htmlFragmentParse.TextWithInlineFormatTags = htmlFragmentParse.TextWithInlineFormatTags.Remove(link.Item1.Start, link.Item1.Length);
                            }

                            // Whenever we alter the content of the paragraph, we need to adjust the positions
                            // of each footnote link. Since we're iterating links in reverse order, every existing link
                            // is guaranteed to be later than the point being edited, which simplifies things.
                            foreach (FootnoteReference existingRef in footnoteRefsInThisParagraph)
                            {
                                existingRef.ReferenceSpan = new IntRange(
                                    existingRef.ReferenceSpan.Start + linkCharsDelta,
                                    existingRef.ReferenceSpan.End + linkCharsDelta);
                            }

                            footnoteRefsInThisParagraph.Add(thisFootnoteRef);
                        }
                        else
                        {
                            htmlFragmentParse.TextWithInlineFormatTags = htmlFragmentParse.TextWithInlineFormatTags.Remove(link.Item1.Start, link.Item1.Length);
                            foreach (FootnoteReference existingRef in footnoteRefsInThisParagraph)
                            {
                                existingRef.ReferenceSpan = new IntRange(
                                    existingRef.ReferenceSpan.Start - link.Item1.Length,
                                    existingRef.ReferenceSpan.End - link.Item1.Length);
                            }
                        }
                    }

                    Console.WriteLine(htmlFragmentParse.TextWithInlineFormatTags);

                    returnVal.Add(new Paragraph()
                    {
                        ParaEntityId = thisParaId,
                        Text = htmlFragmentParse.TextWithInlineFormatTags,
                        ParaClass = GospelParagraphClass.Default,
                        Footnotes = footnoteRefsInThisParagraph.OrderBy(s => s.ReferenceSpan.Start).ToList(),
                        Subregions = EnglishWordFeatureExtractor.BreakSentences(htmlFragmentParse.TextWithInlineFormatTags, thisParaId)
                            .Where(s => s.EntityId.HasValue).ToList()
                    });
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

        private static string GetSingleInnerHtml(HtmlNodeNavigator navigator, string xpath)
        {
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
            public required List<Paragraph> BodyParagraphs;
            public required List<Note> NoteBlocks;
        }

        private class Paragraph
        {
            public required KnowledgeGraphNodeId ParaEntityId;
            public required GospelParagraphClass ParaClass;
            public required string Text;
            public required List<Substring> Subregions;
            public required List<FootnoteReference> Footnotes;

            public override string ToString()
            {
                return Text;
            }
        }

        private class Footnote
        {
            public required string NoteId;
            public required string NoteContent;
            public required ISet<KnowledgeGraphNodeId> EntityRefs;
            public required ISet<string> NoteRefs;
        }

        private class Note
        {
            public required int NoteNum;
            public required IList<NoteParagraph> Paragraphs;
        }

        private class NoteParagraph
        {
            public required string ParaId;
            public required KnowledgeGraphNodeId ParaEntityId;
            public required string ParaContent;
            public required ISet<KnowledgeGraphNodeId> EntityRefs;
            public required List<Substring> Subregions;
        }

        private class FootnoteReference
        {
            public required Footnote Footnote;
            public IntRange ReferenceSpan;
        }
    }
}
