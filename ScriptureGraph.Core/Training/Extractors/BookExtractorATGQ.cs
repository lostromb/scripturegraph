using Durandal.Common.Compression.Zip;
using Durandal.Common.File;
using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using Durandal.Common.Utils;
using HtmlAgilityPack;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
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
                ParseEpubAndProcess(fileSystem, bookPath, logger, (Stream fileStream, int volumeNum, int chapterNum, ILogger logger) =>
                {
                    string bookChapterString = $"{volumeNum}.{chapterNum}";
                    KnowledgeGraphNodeId overallDocumentId = FeatureToNodeMapping.BookChapter(BOOK_ID, bookChapterString);
                    ParseSingleChapter(fileStream, volumeNum, chapterNum, logger,
                        (KnowledgeGraphNodeId documentId) =>
                        {
                            overallDocumentId = documentId;
                        },
                        (KnowledgeGraphNodeId paraId, string title) =>
                        {
                            // High-level features
                            // Title of the question -> Question
                            foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(title))
                            {
                                trainingFeaturesOut.Add(new TrainingFeature(
                                    overallDocumentId,
                                    ngram,
                                    TrainingFeatureType.WordDesignation));
                            }
                        },
                        (KnowledgeGraphNodeId paraId, string body, int paragraphNum, IReadOnlySet<string> paraClassSet) =>
                        {
                            // Associate this paragraph with the entire question
                            trainingFeaturesOut.Add(new TrainingFeature(
                                overallDocumentId,
                                paraId,
                                TrainingFeatureType.BookAssociation));

                            // And with the previous paragraph
                            if (paragraphNum > 1)
                            {
                                trainingFeaturesOut.Add(new TrainingFeature(
                                    paraId,
                                    FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, bookChapterString, (paragraphNum - 1).ToString()),
                                    TrainingFeatureType.ParagraphAssociation));
                            }

                            // Break sentences within the paragraph (this is mainly to control ngram propagation so we don't have associations
                            // doing 9x permutations between every single word in the paragraph)
                            List<string> sentences = EnglishWordFeatureExtractor.BreakSentence(body).ToList();

                            foreach (string sentence in sentences)
                            {
                                string thisSentenceWordBreakerText = StringUtils.RegexRemove(LdsDotOrgCommonParsers.HtmlTagRemover, sentence);

                                // Common word and ngram level features associated with this paragraph entity
                                EnglishWordFeatureExtractor.ExtractTrainingFeatures(thisSentenceWordBreakerText, trainingFeaturesOut, paraId);

                                // Parse all scripture references (in plaintext) and turn them into entity links
                                foreach (ScriptureReference scriptureRef in ScriptureMetadata.ParseAllReferences(thisSentenceWordBreakerText))
                                {
                                    KnowledgeGraphNodeId refNodeId = LdsDotOrgCommonParsers.ConvertScriptureRefToNodeId(scriptureRef);
                                    // Entity reference between this talk paragraph and the scripture ref
                                    trainingFeaturesOut.Add(new TrainingFeature(
                                        paraId,
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
                        });
                });
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        public static void ExtractSearchIndexFeatures(IFileSystem fileSystem, VirtualPath bookPath, ILogger logger, List<TrainingFeature> trainingFeaturesOut, EntityNameIndex nameIndex)
        {
            try
            {
                ParseEpubAndProcess(fileSystem, bookPath, logger, (Stream fileStream, int volumeNum, int chapterNum, ILogger logger) =>
                {
                    KnowledgeGraphNodeId overallDocumentId = FeatureToNodeMapping.BookChapter(BOOK_ID, $"{volumeNum}.{chapterNum}");
                    ParseSingleChapter(fileStream, volumeNum, chapterNum, logger,
                        (KnowledgeGraphNodeId documentId) =>
                        {
                            overallDocumentId = documentId;
                        },
                        (KnowledgeGraphNodeId paraId, string title) =>
                        {
                            foreach (var ngram in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(title))
                            {
                                trainingFeaturesOut.Add(new TrainingFeature(
                                    overallDocumentId,
                                    ngram,
                                    TrainingFeatureType.WordDesignation));
                            }
                        },
                        (KnowledgeGraphNodeId paraId, string body, int paragraphNum, IReadOnlySet<string> paraClassSet) => { });
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
            ParseEpubAndProcess(fileSystem, bookPath, logger, (Stream fileStream, int volumeNum, int chapterNum, ILogger logger) =>
            {
                BookChapterDocument parsedChapter = new BookChapterDocument()
                {
                    BookId = BOOK_ID,
                    DocumentType = GospelDocumentType.GospelBookChapter,
                    Language = LanguageCode.ENGLISH,
                    Paragraphs = new List<GospelParagraph>(),
                    DocumentEntityId = new KnowledgeGraphNodeId(),
                };

                bool success = ParseSingleChapter(fileStream, volumeNum, chapterNum, logger,
                    (KnowledgeGraphNodeId documentId) =>
                    {
                        parsedChapter.DocumentEntityId = documentId;
                    },
                    (KnowledgeGraphNodeId paraId, string title) =>
                    {
                        parsedChapter.Paragraphs.Insert(0, new GospelParagraph()
                        {
                            ParagraphEntityId = paraId,
                            Text = title,
                            Class = GospelParagraphClass.Header
                        });
                    },
                    (KnowledgeGraphNodeId paraId, string body, int paragraphNum, IReadOnlySet<string> paraClassSet) =>
                    {
                        GospelParagraphClass paraClassTyped = GospelParagraphClass.Default;
                        if (paraClassSet.Contains("Sub1"))
                        {
                            paraClassTyped = GospelParagraphClass.SubHeader;
                        }
                        else if (paraClassSet.Contains("Quote") || paraClassSet.Contains("Poem"))
                        {
                            paraClassTyped = GospelParagraphClass.Quotation;
                        }
                        else if (paraClassSet.Contains("Scripture"))
                        {
                            paraClassTyped = GospelParagraphClass.Verse;
                        }

                        parsedChapter.Paragraphs.Add(new GospelParagraph()
                        {
                            ParagraphEntityId = paraId,
                            Text = body,
                            Class = paraClassTyped
                        });
                    });


                if (success)
                {
                    returnVal.Add(parsedChapter);
                }
            });

            return returnVal;
        }


        private static void ParseEpubAndProcess(IFileSystem fileSystem, VirtualPath bookPath, ILogger logger, Action<Stream, int, int, ILogger> parseHandler)
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
                        parseHandler(fileStream, volumeNum, chapterNum, logger);
                    }
                }
            }
        }

        private static bool ParseSingleChapter(
            Stream htmlStream,
            int volume,
            int chapter,
            ILogger logger,
            Action<KnowledgeGraphNodeId> entireDocumentDelegate,
            Action<KnowledgeGraphNodeId, string> titleDelegate,
            Action<KnowledgeGraphNodeId, string, int, IReadOnlySet<string>> bodyParagraphDelegate)
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
                    return false;
                }

                entireDocumentDelegate(FeatureToNodeMapping.BookChapter(BOOK_ID, bookChapterString));

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
                        titleDelegate(FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, bookChapterString, "title"), paragraphContent);
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
                        bodyParagraphDelegate(
                            FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, bookChapterString, paragraph.ToString()),
                            paragraphContent,
                            paragraph,
                            paraClassSet);

                        paragraph++;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                logger.Log(e);
                return false;
            }
        }
    }
}
