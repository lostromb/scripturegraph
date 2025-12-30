using Durandal.Common.Compression.Zip;
using Durandal.Common.File;
using Durandal.Common.Instrumentation;
using Durandal.Common.IO;
using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using Durandal.Common.Tasks;
using Durandal.Common.Utils;
using HtmlAgilityPack;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Schemas.Documents;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.XPath;

namespace ScriptureGraph.Core.Training.Extractors
{
    /// <summary>
    /// Parser for the Messiah series from epub format
    /// </summary>
    public static class BookExtractorMessiah
    {
        // The book ID convention is:
        // "messiah1" -> Promised Messiah
        // "messiah2" -> Mortal Messiah (many chapters split over multiple volumes; here they are considered one single book)
        // "messiah3" -> Millennial Messiah

        private static readonly Regex CHAPTER_FILE_MATCHER = new Regex("^part(\\d+)\\.xhtml$");
        private static readonly Regex CHAPTER_NUM_EXTRACTOR = new Regex("Chapter\\s+(\\d+)", RegexOptions.IgnoreCase);

        // <\s*span\s+class=\"class14\"\s*>([\w\W]+?)<\/\s*span\s*>
        private static readonly Regex ITALIC_CLASS_REPLACER = new Regex("<\\s*span\\s+class=\\\"class14\\\"\\s*>([\\w\\W]+?)<\\/\\s*span\\s*>");
        // <br[^>]+\/?>
        private static readonly Regex BR_NORMALIZER = new Regex("<br[^>]+\\/?>");
        // <\s*span\s+id=\"(.+?)\"
        private static readonly Regex FOOTNOTE_ID_EXTRACTOR = new Regex("<\\s*span\\s+id=\\\"(.+?)\\\"");
        private static readonly List<string> EMPTY_STRING_LIST = new List<string>();

        public static void ExtractFeatures(IFileSystem fileSystem, VirtualPath bookPath, ILogger logger, Action<TrainingFeature> trainingFeaturesOut, IThreadPool threadPool)
        {
            try
            {
                ParseEpubAndProcess(fileSystem, bookPath, logger, (ParsedChapter chapter, ILogger logger) =>
                {
                    ParsedChapter closure = chapter;
                    threadPool.EnqueueUserWorkItem(() =>
                    {
                        try
                        {
                            ExtractFeaturesOnThread(closure, trainingFeaturesOut, logger);
                        }
                        catch (Exception e)
                        {
                            logger.Log(e);
                        }
                    });
                });
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        private static void ExtractFeaturesOnThread(ParsedChapter chapter, Action<TrainingFeature> trainingFeaturesOut, ILogger logger)
        {
            // High-level features
            // Title of the chapter -> Chapter
            foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(chapter.Title))
            {
                trainingFeaturesOut(new TrainingFeature(
                    chapter.DocumentEntityId,
                    ngram,
                    TrainingFeatureType.WordDesignation));
            }

            List<TrainingFeature> scratch = new List<TrainingFeature>();
            HashSet<KnowledgeGraphNodeId> dedupSet = new HashSet<KnowledgeGraphNodeId>();
            foreach (ParsedParagraph para in chapter.Paragraphs)
            {
                dedupSet.Clear();

                // Associate this paragraph with the entire chapter
                trainingFeaturesOut(new TrainingFeature(
                    chapter.DocumentEntityId,
                    para.ParaEntityId,
                    TrainingFeatureType.BookAssociation));

                // And with the previous paragraph
                if (para.ParagraphNum > 1)
                {
                    trainingFeaturesOut(new TrainingFeature(
                        para.ParaEntityId,
                        FeatureToNodeMapping.BookChapterParagraph(chapter.DocumentEntityId, (para.ParagraphNum - 1).ToString()),
                        TrainingFeatureType.ParagraphAssociation));
                }

                // Paragraph footnotes references -> paragraph
                foreach (string paragraphFootnoteAnchor in para.FootnoteRefs)
                {
                    if (chapter.Footnotes.TryGetValue(paragraphFootnoteAnchor, out ParsedFootnote footnote))
                    {
                        foreach (var referenceFromFootnote in footnote.References)
                        {
                            if (!dedupSet.Contains(referenceFromFootnote.Node))
                            {
                                dedupSet.Add(referenceFromFootnote.Node);
                                trainingFeaturesOut(new TrainingFeature(
                                    para.ParaEntityId,
                                    referenceFromFootnote.Node,
                                    referenceFromFootnote.LowEmphasis ? TrainingFeatureType.ScriptureReferenceWithoutEmphasis : TrainingFeatureType.ScriptureReference));
                            }
                        }
                    }
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

                    // Parse all scripture references (in plaintext) and turn them into entity links
                    foreach (OmniParserOutput parsedScripture in OmniParser.ParseHtml(thisSentenceWordBreakerText, logger, LanguageCode.ENGLISH))
                    {
                        if (!dedupSet.Contains(parsedScripture.Node))
                        {
                            dedupSet.Add(parsedScripture.Node);
                            // Entity reference between this paragraph and the scripture ref
                            trainingFeaturesOut(new TrainingFeature(
                            para.ParaEntityId,
                            parsedScripture.Node,
                            TrainingFeatureType.EntityReference));
                        }

                        // And association between all words spoken in this sentence and the scripture ref
                        foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(thisSentenceWordBreakerText))
                        {
                            trainingFeaturesOut(new TrainingFeature(
                                ngram,
                                parsedScripture.Node,
                                TrainingFeatureType.WordAssociation));
                        }
                    }
                }
            }
        }

        public static void ExtractSearchIndexFeatures(
            IFileSystem fileSystem, VirtualPath bookPath, ILogger logger, Action<TrainingFeature> trainingFeatureHandler, EntityNameIndex nameIndex)
        {
            try
            {
                ParseEpubAndProcess(fileSystem, bookPath, logger, (ParsedChapter chapter, ILogger logger) =>
                {
                    nameIndex.EntityIdToPlainName[chapter.DocumentEntityId] = chapter.Title;
                    foreach (var ngram in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(chapter.Title))
                    {
                        trainingFeatureHandler(new TrainingFeature(
                            chapter.DocumentEntityId,
                            ngram,
                            TrainingFeatureType.WordDesignation));
                    }
                });
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        public static IEnumerable<BookChapterDocument> ExtractDocuments(IFileSystem fileSystem, VirtualPath bookPath, ILogger logger)
        {
            List<BookChapterDocument> returnVal = new List<BookChapterDocument>();
            ParseEpubAndProcess(fileSystem, bookPath, logger, (ParsedChapter chapter, ILogger logger) =>
            {
                BookChapterDocument parsedChapter = new BookChapterDocument()
                {
                    BookId = BookId(chapter.Volume),
                    DocumentType = GospelDocumentType.GospelBookChapter,
                    Language = LanguageCode.ENGLISH,
                    Paragraphs = new List<GospelParagraph>(),
                    DocumentEntityId = chapter.DocumentEntityId,
                    ChapterId = chapter.Chapter.ToString(),
                    ChapterName = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, chapter.Title),
                };

                foreach (ParsedParagraph para in chapter.Paragraphs)
                {
                    string text = para.Text;
                    GospelParagraphClass paraClassTyped = GospelParagraphClass.Default;
                    if (string.Equals("class16", para.Class, StringComparison.OrdinalIgnoreCase))
                    {
                        paraClassTyped = GospelParagraphClass.Header;
                    }
                    else if (
                        string.Equals("class17", para.Class, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals("class18", para.Class, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals("class26", para.Class, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals("class41", para.Class, StringComparison.OrdinalIgnoreCase))
                    {
                        paraClassTyped = GospelParagraphClass.SubHeader;
                    }
                    else if (
                        string.Equals("class23", para.Class, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals("class32", para.Class, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals("class34", para.Class, StringComparison.OrdinalIgnoreCase))
                    {
                        paraClassTyped = GospelParagraphClass.Quotation;
                    }
                    else if (string.Equals("class36", para.Class, StringComparison.OrdinalIgnoreCase))
                    {
                        text = $"<i>{text}</i>";
                        paraClassTyped = GospelParagraphClass.SubHeader;
                    }

                    parsedChapter.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = para.ParaEntityId,
                        Text = text,
                        Class = paraClassTyped
                    });
                }

                if (chapter.Chapter > 1)
                {
                    parsedChapter.Prev = FeatureToNodeMapping.BookChapter(parsedChapter.BookId, (chapter.Chapter - 1).ToString());
                }
                if (chapter.Chapter < CHAPTER_LENGTHS[chapter.Volume - 1])
                {
                    parsedChapter.Next = FeatureToNodeMapping.BookChapter(parsedChapter.BookId, (chapter.Chapter + 1).ToString());
                }

                returnVal.Add(parsedChapter);
            });

            return returnVal;
        }

        private static string BookId(int volume)
        {
            return $"messiah{volume}";
        }

        private static int HtmlFileNumberToVolume(int fileNumber)
        {
            if (fileNumber < 4)
            {
                // Intro and TOC
                return -1;
            }
            if (fileNumber < 39)
            {
                // Promised Messiah
                return 1;
            }
            //else if (fileNumber < 78)
            //{
            //    // Mortal Messiah v1
            //    return 2;
            //}
            //else if (fileNumber < 110)
            //{
            //    // Mortal Messiah v2
            //    return 3;
            //}
            //else if (fileNumber < 151)
            //{
            //    // Mortal Messiah v3
            //    return 4;
            //}
            else if (fileNumber < 186)
            {
                // Mortal Messiah (all volumes)
                return 2;
            }
            else if (fileNumber < 246)
            {
                // Millenial Messiah
                return 3;
            }
            else
            {
                // End page
                return -1;
            }
        }

        private static readonly int[] CHAPTER_LENGTHS = new int[3] { 32, 122, 56 };

        private static void ParseEpubAndProcess(IFileSystem fileSystem, VirtualPath bookPath, ILogger logger, Action<ParsedChapter, ILogger> parseHandler)
        {
            if (!fileSystem.Exists(bookPath))
            {
                throw new FileNotFoundException($"File not found: {bookPath}");
            }

            VirtualPath extractionDirectory = bookPath.Container.Combine(bookPath.NameWithoutExtension);
            VirtualPath epubContentDirectory = extractionDirectory.Combine("OEBPS");
            if (!fileSystem.Exists(extractionDirectory))
            {
                fileSystem.CreateDirectory(extractionDirectory);
                using (ZipFile epubArchive = ZipFile.Read(bookPath, fileSystem, logger))
                {
                    epubArchive.ExtractAll(extractionDirectory);
                }
            }

            foreach (VirtualPath file in fileSystem.ListFiles(epubContentDirectory))
            {
                Match fileNameMatch = CHAPTER_FILE_MATCHER.Match(file.Name);
                if (fileNameMatch.Success)
                {
                    using (Stream fileStream = fileSystem.OpenStream(file, FileOpenMode.Open, FileAccessMode.Read))
                    {
                        logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "Parsing epub file \"{0}\"", file.FullName);
                        int documentNumber = int.Parse(fileNameMatch.Groups[1].Value);
                        int volume = HtmlFileNumberToVolume(documentNumber);
                        if (volume > 0)
                        {
                            ParsedChapter? chap = ParseSingleChapter(fileStream, volume, logger);

                            if (chap != null)
                            {
                                parseHandler(chap.Value, logger);
                            }
                        }
                    }
                }
            }
        }

        private static ParsedChapter? ParseSingleChapter(
            Stream htmlStream,
            int volume,
            ILogger logger)
        {
            try
            {
                string rawHtml;
                using (PooledStringBuilder pooledSb = StringBuilderPool.Rent())
                {
                    using (StringBuilderTextWriter textWriter = new StringBuilderTextWriter(pooledSb.Builder))
                    using (TextReader reader = new StreamReader(htmlStream, StringUtils.UTF8_WITHOUT_BOM))
                    using (PooledBuffer<char> scratchChar = BufferPool<char>.Rent())
                    {
                        while (true)
                        {
                            int charsRead = reader.Read(scratchChar.AsSpan);
                            if (charsRead <= 0)
                            {
                                break;
                            }

                            textWriter.Write(scratchChar.AsSpan.Slice(0, charsRead));
                        }
                    }

                    rawHtml = pooledSb.Builder.ToString();
                }

                rawHtml = MoreStringUtils.RegexGroupReplace(ITALIC_CLASS_REPLACER, rawHtml, (groups) => $"<i>{groups[1].Value}</i>");
                rawHtml = StringUtils.RegexReplace(BR_NORMALIZER, rawHtml, "<br>");
                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(rawHtml);

                // First, see if this is actually a chapter
                string chapterHeader = GetSingleInnerHtml(html, "/html/body/div[@class=\"class16\"]");
                string chapterSubheader = GetSingleInnerHtml(html, "/html/body/div[@class=\"class17\"]");
                if (string.IsNullOrEmpty(chapterSubheader))
                {
                    chapterSubheader = GetSingleInnerHtml(html, "/html/body/div[@class=\"class41\"]");
                }

                Match chapterNumMatch = CHAPTER_NUM_EXTRACTOR.Match(chapterHeader);

                if (string.IsNullOrWhiteSpace(chapterHeader) ||
                    string.IsNullOrWhiteSpace(chapterSubheader) ||
                    !chapterNumMatch.Success)
                {
                    logger.Log("This page does not appear to be a chapter", LogLevel.Wrn);
                    return null;
                }

                chapterHeader = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, chapterHeader).Trim();
                chapterSubheader = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, chapterSubheader).Trim();

                int chapterNum = int.Parse(chapterNumMatch.Groups[1].Value);
                string bookId = BookId(volume);
                string bookChapterString = chapterNum.ToString();
                KnowledgeGraphNodeId documentEntityId = FeatureToNodeMapping.BookChapter(bookId, bookChapterString);
                List<ParsedParagraph> paragraphs = new List<ParsedParagraph>();
                Dictionary<string, ParsedFootnote> footnotes = new Dictionary<string, ParsedFootnote>();

                int paragraph = 1;
                XPathNodeIterator iter;
                
                // Parse paragraphs
                HtmlNodeNavigator navigator = html.CreateNavigator() as HtmlNodeNavigator;
                iter = navigator.Select("/html/body/div");
                int footnoteRef = 1;
                while (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
                {
                    string paraClassRaw = iter.Current.GetAttribute("class", string.Empty);
                    string paraContent = currentNav.CurrentNode.InnerHtml.Trim();

                    string paragraphContent = paraContent;
                    paragraphContent = LdsDotOrgCommonParsers.ReplaceBrWithNewlines(paragraphContent);
                    paragraphContent = LdsDotOrgCommonParsers.RemoveNbsp(paragraphContent);
                    paragraphContent = WebUtility.HtmlDecode(paragraphContent);
                    var paragraphParseModel = LdsDotOrgCommonParsers.ParseAndFormatHtmlFragmentNew(paragraphContent, logger, insertLineBreaks: true);

                    if (string.IsNullOrWhiteSpace(paragraphParseModel.TextWithInlineFormatTags))
                    {
                        continue;
                    }

                    List<string> footnoteRefs = EMPTY_STRING_LIST;
                    if (string.Equals(paraClassRaw, "class8", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(paraClassRaw, "class30", StringComparison.OrdinalIgnoreCase))
                    {
                        // It's a footnote. Extract its ID.
                        string anchorId = StringUtils.RegexRip(FOOTNOTE_ID_EXTRACTOR, paragraphContent, 1);

                        if (string.IsNullOrEmpty(anchorId))
                        {
                            logger.Log("Footnote paragraph without an ID", LogLevel.Wrn);
                        }
                        else
                        {
                            // Extract footnote scripture references, if any
                            List<OmniParserOutput> referencesWithinThisFootnote = OmniParser.ParseHtml(paragraphContent, logger, LanguageCode.ENGLISH).ToList();

                            footnotes[anchorId] = new ParsedFootnote()
                            {
                                AnchorId = anchorId,
                                Text = paragraphParseModel.TextWithInlineFormatTags,
                                References = referencesWithinThisFootnote,
                            };
                        }
                    }
                    else
                    {
                        foreach (var link in paragraphParseModel.Links)
                        {
                            if (!link.Item2.Contains('#'))
                            {
                                continue;
                            }

                            if (footnoteRefs.Count == 0)
                            {
                                footnoteRefs = new List<string>();
                            }

                            footnoteRefs.Add(link.Item2.Substring(link.Item2.IndexOf('#') + 1));
                        }

                        // Re-add the text for footnotes, being very careful about indexes
                        int footnoteId = footnoteRef + paragraphParseModel.Links.Count;
                        paragraphParseModel.Links.Reverse();
                        using (PooledStringBuilder pooledSb = StringBuilderPool.Rent())
                        {
                            foreach (var link in paragraphParseModel.Links)
                            {
                                pooledSb.Builder.Clear();
                                pooledSb.Builder.Append(paragraphParseModel.TextWithInlineFormatTags.Substring(0, link.Item1.Start));
                                pooledSb.Builder.Append("[Footnote ");
                                pooledSb.Builder.Append(paragraphParseModel.TextWithInlineFormatTags.Substring(link.Item1.Start, link.Item1.Length));
                                pooledSb.Builder.Append("]");
                                pooledSb.Builder.Append(paragraphParseModel.TextWithInlineFormatTags.Substring(link.Item1.Start + link.Item1.Length));
                                paragraphParseModel.TextWithInlineFormatTags = pooledSb.Builder.ToString();
                                footnoteId--;
                                footnoteRef++;
                            }
                        }
                    }

                    //logger.Log($"<class=\"{paraClassRaw}\">{paragraphContent}");
                    paragraphs.Add(new ParsedParagraph()
                    {
                        ParaEntityId = FeatureToNodeMapping.BookChapterParagraph(bookId, bookChapterString, paragraph.ToString()),
                        Text = paragraphParseModel.TextWithInlineFormatTags,
                        ParagraphNum = paragraph,
                        Class = paraClassRaw,
                        FootnoteRefs = footnoteRefs,
                    });

                    paragraph++;
                }

                return new ParsedChapter()
                {
                    Title = chapterSubheader,
                    DocumentEntityId = documentEntityId,
                    Paragraphs = paragraphs,
                    Footnotes = footnotes,
                    Volume = volume,
                    Chapter = chapterNum
                };
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

        private record struct ParsedChapter
        {
            public int Volume;
            public int Chapter;
            public string Title;
            public KnowledgeGraphNodeId DocumentEntityId;
            public List<ParsedParagraph> Paragraphs;
            public Dictionary<string, ParsedFootnote> Footnotes;
        }

        private record struct ParsedParagraph
        {
            public KnowledgeGraphNodeId ParaEntityId;
            public string Text;
            public int ParagraphNum;
            public string Class;
            public List<string> FootnoteRefs;
        }

        private record struct ParsedFootnote
        {
            public string AnchorId;
            public string Text;
            public List<OmniParserOutput> References;
        }
    }
}
