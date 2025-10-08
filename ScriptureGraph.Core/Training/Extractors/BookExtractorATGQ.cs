using Durandal.Common.Compression.Zip;
using Durandal.Common.File;
using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
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
    /// <summary>
    /// Parser for Answers to Gospel Questions from epub format
    /// </summary>
    public static class BookExtractorATGQ
    {
        private static readonly string BOOK_ID = "atgq";
        private static readonly Regex CHAPTER_FILE_MATCHER = new Regex("^sec_(\\d+)_(\\d+)\\.xml$");
        private static readonly IReadOnlySet<string> EMPTY_STRING_SET = new HashSet<string>();

        public static void ExtractFeatures(IFileSystem fileSystem, VirtualPath bookPath, ILogger logger, List<TrainingFeature> trainingFeaturesOut)
        {
            try
            {
                ParseEpubAndProcess(fileSystem, bookPath, logger, (ParsedChapter chapter, ILogger logger) =>
                {
                    // High-level features
                    // Title of the question -> Question
                    foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(chapter.Title))
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            chapter.DocumentEntityId,
                            ngram,
                            TrainingFeatureType.WordDesignation));
                    }

                    foreach (ParsedBodyParagraph para in chapter.BodyParagraphs)
                    {
                        // Associate this paragraph with the entire question
                        trainingFeaturesOut.Add(new TrainingFeature(
                            chapter.DocumentEntityId,
                            para.ParaEntityId,
                            TrainingFeatureType.BookAssociation));

                        // And with the previous paragraph
                        if (para.ParagraphNum > 1)
                        {
                            trainingFeaturesOut.Add(new TrainingFeature(
                                para.ParaEntityId,
                                FeatureToNodeMapping.BookChapterParagraph(chapter.DocumentEntityId, (para.ParagraphNum - 1).ToString()),
                                TrainingFeatureType.ParagraphAssociation));
                        }

                        // Break sentences within the paragraph (this is mainly to control ngram propagation so we don't have associations
                        // doing 9x permutations between every single word in the paragraph)
                        List<string> sentences = EnglishWordFeatureExtractor.BreakSentence(para.Text).ToList();

                        foreach (string sentence in sentences)
                        {
                            string thisSentenceWordBreakerText = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, sentence);

                            // Common word and ngram level features associated with this paragraph entity
                            EnglishWordFeatureExtractor.ExtractTrainingFeatures(thisSentenceWordBreakerText, trainingFeaturesOut, para.ParaEntityId);

                            // Parse all scripture references (in plaintext) and turn them into entity links
                            foreach (ScriptureReference scriptureRef in ScriptureMetadata.ParseAllReferences(thisSentenceWordBreakerText, LanguageCode.ENGLISH))
                            {
                                KnowledgeGraphNodeId refNodeId = scriptureRef.ToNodeId();
                                // Entity reference between this talk paragraph and the scripture ref
                                trainingFeaturesOut.Add(new TrainingFeature(
                                    para.ParaEntityId,
                                    refNodeId,
                                    TrainingFeatureType.EntityReference));

                                // And association between all words spoken in this sentence and the scripture ref
                                foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(thisSentenceWordBreakerText))
                                {
                                    trainingFeaturesOut.Add(new TrainingFeature(
                                        ngram,
                                        refNodeId,
                                        TrainingFeatureType.WordAssociation));
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        public static void ExtractSearchIndexFeatures(
            IFileSystem fileSystem, VirtualPath bookPath, ILogger logger, Action<TrainingFeature> trainingFeatureHandler, EntityNameIndex nameIndex)
        {
            try
            {
                ParseEpubAndProcess(fileSystem, bookPath, logger, (ParsedChapter chapter, ILogger logger) =>
                {
                    nameIndex.Mapping[chapter.DocumentEntityId] = chapter.Title;
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
                    BookId = BOOK_ID,
                    DocumentType = GospelDocumentType.GospelBookChapter,
                    Language = LanguageCode.ENGLISH,
                    Paragraphs = new List<GospelParagraph>(),
                    DocumentEntityId = chapter.DocumentEntityId,
                    ChapterId = chapter.ChapterId,
                    ChapterName = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, chapter.Title),
                };

                parsedChapter.Paragraphs.Insert(0, new GospelParagraph()
                {
                    ParagraphEntityId = chapter.TitleParaEntityId,
                    Text = chapter.Title,
                    Class = GospelParagraphClass.Header
                });

                foreach (ParsedBodyParagraph para in chapter.BodyParagraphs)
                {
                    GospelParagraphClass paraClassTyped = GospelParagraphClass.Default;
                    if (para.ClassSet.Contains("Sub1"))
                    {
                        paraClassTyped = GospelParagraphClass.SubHeader;
                    }
                    else if (para.ClassSet.Contains("Quote") || para.ClassSet.Contains("Poem"))
                    {
                        paraClassTyped = GospelParagraphClass.Quotation;
                    }
                    else if (para.ClassSet.Contains("Scripture"))
                    {
                        paraClassTyped = GospelParagraphClass.Verse;
                    }

                    parsedChapter.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = para.ParaEntityId,
                        Text = para.Text,
                        Class = paraClassTyped
                    });
                }

                returnVal.Add(parsedChapter);
            });

            return returnVal;
        }

        private static void ParseEpubAndProcess(IFileSystem fileSystem, VirtualPath bookPath, ILogger logger, Action<ParsedChapter, ILogger> parseHandler)
        {
            if (!fileSystem.Exists(bookPath))
            {
                throw new FileNotFoundException($"File not found: {bookPath}");
            }

            VirtualPath extractionDirectory = bookPath.Container.Combine(bookPath.NameWithoutExtension);
            VirtualPath epubContentDirectory = extractionDirectory.Combine("OPS");
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
                        logger.LogFormat(LogLevel.Vrb, DataPrivacyClassification.SystemMetadata, "Parsing file \"{0}\"", file.FullName);
                        int volumeNum = int.Parse(fileNameMatch.Groups[1].Value);
                        int chapterNum = int.Parse(fileNameMatch.Groups[2].Value);
                        ParsedChapter? chap = ParseSingleChapter(fileStream, volumeNum, chapterNum, logger);

                        if (chap != null)
                        {
                            parseHandler(chap.Value, logger);
                        }
                    }
                }
            }
        }

        private static ParsedChapter? ParseSingleChapter(
            Stream htmlStream,
            int volume,
            int chapter,
            ILogger logger)
        {
            try
            {
                HtmlDocument html = new HtmlDocument();
                html.Load(htmlStream);

                string bookChapterString = $"{volume}.{chapter}";

                bool hasChapterNum = false;
                bool hasChapterTitle = false;

                // First, see if this is actually a chapter
                XPathNodeIterator iter = html.CreateNavigator().Select("/html/body/p");
                while (iter.MoveNext())
                {
                    if (iter.Current == null)
                    {
                        continue;
                    }

                    string paraClassRaw = iter.Current.GetAttribute("class", string.Empty);
                    IReadOnlySet<string> paraClassSet = paraClassRaw == null ? EMPTY_STRING_SET : paraClassRaw.Split(" ").ToHashSet(StringComparer.OrdinalIgnoreCase);

                    if (paraClassSet.Contains("Chapter"))
                    {
                        hasChapterTitle = true;
                    }
                    else if (paraClassSet.Contains("ChapNo"))
                    {
                        hasChapterNum = true;
                    }
                }

                if (!hasChapterNum || !hasChapterTitle)
                {
                    logger.Log("This page does not appear to be a chapter", LogLevel.Wrn);
                    return null;
                }

                KnowledgeGraphNodeId documentEntityId = FeatureToNodeMapping.BookChapter(BOOK_ID, bookChapterString);
                KnowledgeGraphNodeId? titleParaEntityId = null;
                string? title = null;
                List<ParsedBodyParagraph> bodyParagraphs = new List<ParsedBodyParagraph>();

                int paragraph = 1;
                iter = html.CreateNavigator().Select("/html/body/p");
                while (iter.MoveNext())
                {
                    if (iter.Current == null)
                    {
                        continue;
                    }

                    string paraClassRaw = iter.Current.GetAttribute("class", string.Empty);
                    IReadOnlySet<string> paraClassSet = paraClassRaw == null ? EMPTY_STRING_SET : paraClassRaw.Split(" ").ToHashSet(StringComparer.OrdinalIgnoreCase);
                    string paraContent = iter.Current.InnerXml.Trim();

                    if (paraClassSet.Contains("Chapter"))
                    {
                        string paragraphContent = paraContent;
                        paragraphContent = LdsDotOrgCommonParsers.ReplaceBrWithNewlines(paragraphContent);
                        paragraphContent = LdsDotOrgCommonParsers.StripAllButBoldAndItalics(paragraphContent);
                        paragraphContent = LdsDotOrgCommonParsers.RemoveNbsp(paragraphContent);
                        paragraphContent = WebUtility.HtmlDecode(paragraphContent);
                        paragraphContent = WebUtility.HtmlDecode(paragraphContent);
                        logger.LogFormat(LogLevel.Vrb, DataPrivacyClassification.SystemMetadata, "Document title set to \"{0}\"", paragraphContent);
                        titleParaEntityId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, bookChapterString, "title");
                        title = paragraphContent;
                    }
                    else if (paraClassSet.Contains("ChapNo"))
                    {
                        logger.LogFormat(LogLevel.Vrb, DataPrivacyClassification.SystemMetadata, "Chapter number is \"{0}\"", paraContent);
                        hasChapterNum = true;
                    }
                    else
                    {
                        string paragraphContent = paraContent;
                        paragraphContent = LdsDotOrgCommonParsers.ReplaceBrWithNewlines(paragraphContent);
                        paragraphContent = LdsDotOrgCommonParsers.StripAllButBoldAndItalics(paragraphContent);
                        paragraphContent = LdsDotOrgCommonParsers.RemoveNbsp(paragraphContent);
                        paragraphContent = WebUtility.HtmlDecode(paragraphContent);
                        paragraphContent = WebUtility.HtmlDecode(paragraphContent);
                        if (string.IsNullOrWhiteSpace(paragraphContent))
                        {
                            continue;
                        }

                        //logger.Log($"<class=\"{paraClassRaw}\">{paragraphContent}");
                        bodyParagraphs.Add(new ParsedBodyParagraph()
                        {
                            ParaEntityId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, bookChapterString, paragraph.ToString()),
                            Text = paragraphContent,
                            ParagraphNum = paragraph,
                            ClassSet = paraClassSet.Count == 0 ? EMPTY_STRING_SET : new HashSet<string>(paraClassSet, StringComparer.OrdinalIgnoreCase)
                        });

                        paragraph++;
                    }
                }

                if (title is null || titleParaEntityId is null)
                {
                    logger.Log("No title found", LogLevel.Wrn);
                    return null;
                }

                return new ParsedChapter()
                {
                    Title = title,
                    DocumentEntityId = documentEntityId,
                    TitleParaEntityId = titleParaEntityId.Value,
                    BodyParagraphs = bodyParagraphs,
                    ChapterId = bookChapterString,
                };
            }
            catch (Exception e)
            {
                logger.Log(e);
                return null;
            }
        }

        private record struct ParsedChapter
        {
            public string ChapterId;
            public KnowledgeGraphNodeId DocumentEntityId;
            public KnowledgeGraphNodeId TitleParaEntityId;
            public string Title;
            public List<ParsedBodyParagraph> BodyParagraphs;
        }

        private record struct ParsedBodyParagraph
        {
            public KnowledgeGraphNodeId ParaEntityId;
            public string Text;
            public int ParagraphNum;
            public IReadOnlySet<string> ClassSet;
        }
    }
}
