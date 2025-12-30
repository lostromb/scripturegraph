using Durandal.Common.Compression.Zip;
using Durandal.Common.File;
using Durandal.Common.Instrumentation;
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
    /// Parser for Mormon Doctrine from epub format
    /// </summary>
    public static class BookExtractorMD
    {
        private static readonly string BOOK_ID = "md";
        private static readonly Regex CHAPTER_FILE_MATCHER = new Regex("^part\\d+.html$");
        private static readonly IReadOnlySet<KnowledgeGraphNodeId> EMPTY_NODE_SET = new HashSet<KnowledgeGraphNodeId>();

        public static void ExtractFeatures(IFileSystem fileSystem, VirtualPath bookPath, ILogger logger, Action<TrainingFeature> trainingFeaturesOut, IThreadPool threadPool)
        {
            try
            {
                foreach (ParsedTopic topic in ExtractTopics(fileSystem, bookPath, logger))
                {
                    ParsedTopic closure = topic;
                    try
                    {
                        threadPool.EnqueueUserWorkItem(() =>
                            ExtractFeaturesOnThread(closure, trainingFeaturesOut, logger));
                    }
                    catch (Exception e)
                    {
                        logger.Log(e);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        private static void ExtractFeaturesOnThread(ParsedTopic topic, Action<TrainingFeature> trainingFeaturesOut, ILogger logger)
        {
            // High-level features
            // Title of the topic -> topic
            foreach (var ngram in EnglishWordFeatureExtractor.ExtractNGrams(topic.Title))
            {
                trainingFeaturesOut(new TrainingFeature(
                    topic.DocumentEntityId,
                    ngram,
                    TrainingFeatureType.WordDesignation));
            }

            // Seealso references in this topic
            string topicId = topic.Title.ToLowerInvariant();
            KnowledgeGraphNodeId seeAlsoParagraphNode = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, topicId, "seealso");
            foreach (string seeAlsoTopic in topic.SeeAlso)
            {
                string seeAlsoTopicId = seeAlsoTopic.ToLowerInvariant();
                KnowledgeGraphNodeId seeAlsoNodeId = FeatureToNodeMapping.BookChapter(BOOK_ID, topicId);
                trainingFeaturesOut(new TrainingFeature(
                    topic.DocumentEntityId,
                    seeAlsoNodeId,
                    TrainingFeatureType.EntityReference));

                trainingFeaturesOut(new TrainingFeature(
                    seeAlsoParagraphNode,
                    seeAlsoNodeId,
                    TrainingFeatureType.EntityReference));
            }

            List<TrainingFeature> scratch = new List<TrainingFeature>();
            foreach (ParsedBodyParagraph para in topic.BodyParagraphs)
            {
                // Associate this paragraph with the entire topic
                trainingFeaturesOut(new TrainingFeature(
                    topic.DocumentEntityId,
                    para.ParaEntityId,
                    TrainingFeatureType.BookAssociation));

                // And with the previous paragraph
                if (para.ParagraphNum > 1)
                {
                    trainingFeaturesOut(new TrainingFeature(
                        para.ParaEntityId,
                        FeatureToNodeMapping.BookChapterParagraph(topic.DocumentEntityId, (para.ParagraphNum - 1).ToString()),
                        TrainingFeatureType.ParagraphAssociation));
                }

                // Scripture references in this paragraph
                foreach (KnowledgeGraphNodeId scriptureRef in para.References)
                {
                    trainingFeaturesOut(new TrainingFeature(
                        para.ParaEntityId,
                        scriptureRef,
                        TrainingFeatureType.EntityReference));
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

        public static void ExtractSearchIndexFeatures(
            IFileSystem fileSystem, VirtualPath bookPath, ILogger logger, Action<TrainingFeature> trainingFeatureHandler, EntityNameIndex nameIndex)
        {
            try
            {
                foreach (ParsedTopic topic in ExtractTopics(fileSystem, bookPath, logger))
                {
                    if (topic.BodyParagraphs.Count == 0)
                    {
                        continue;
                    }

                    nameIndex.EntityIdToPlainName[topic.DocumentEntityId] = topic.Title;
                    foreach (var ngram in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(topic.Title))
                    {
                        trainingFeatureHandler(new TrainingFeature(
                            topic.DocumentEntityId,
                            ngram,
                            TrainingFeatureType.WordDesignation));
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        public static IEnumerable<BookChapterDocument> ExtractDocuments(IFileSystem fileSystem, VirtualPath bookPath, ILogger logger)
        {
            foreach (ParsedTopic topic in ExtractTopics(fileSystem, bookPath, logger))
            {
                if (topic.BodyParagraphs.Count == 0)
                {
                    continue;
                }

                string topicId = topic.Title.ToLowerInvariant();
                topicId = topicId.Replace('\'', '_');
                topicId = topicId.Replace('\"', '_');
                topicId = topicId.Replace('(', '_');
                topicId = topicId.Replace(')', '_');
                topicId = FilePathSanitizer.SanitizeFileName(topicId);

                BookChapterDocument parsedChapter = new BookChapterDocument()
                {
                    BookId = BOOK_ID,
                    DocumentType = GospelDocumentType.GospelBookChapter,
                    Language = LanguageCode.ENGLISH,
                    Paragraphs = new List<GospelParagraph>(),
                    DocumentEntityId = topic.DocumentEntityId,
                    ChapterId = topicId,
                    ChapterName = topic.Title
                };

                parsedChapter.Paragraphs.Add(new GospelParagraph()
                {
                    ParagraphEntityId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, topicId, "title"),
                    Text = topic.Title,
                    Class = GospelParagraphClass.Header
                });

                parsedChapter.Paragraphs.Add(new GospelParagraph()
                {
                    ParagraphEntityId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, topicId, "seealso"),
                    Text = "See also: " + string.Join(", ", topic.SeeAlso),
                    Class = GospelParagraphClass.StudySummary
                });

                foreach (ParsedBodyParagraph para in topic.BodyParagraphs)
                {
                    parsedChapter.Paragraphs.Add(new GospelParagraph()
                    {
                        ParagraphEntityId = para.ParaEntityId,
                        Text = para.Text,
                        Class = GospelParagraphClass.Default
                    });
                }

                yield return parsedChapter;
            }
        }

        private static IEnumerable<ParsedTopic> ExtractTopics(IFileSystem fileSystem, VirtualPath bookPath, ILogger logger)
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

            Regex letterHeaderMatcher = new Regex("^ *\\([A-Z] .{1,4}$");
            Regex topicApproxMatcher = new Regex("^ *\\( *(.+) .{0,4}$");
            Regex pageNumberMatcher = new Regex("^ *\\d+ *$");
            Regex spaceCollapser = new Regex(" +");
            Regex seeAlsoSplitterMatcher = new Regex("^(.*?)(See +([A-Z][a-zA-Z, \\-\\'\\.\\(\\)]+))$");
            Regex seeAlsoMatcher = new Regex("^ *See +([A-Z][a-zA-Z, \\-\\'\\.\\(\\)]+)$");
            Regex seeAlsoExtendedMatcher = new Regex("^ *[A-Z, \\-\\'\\.\\(\\)]+ *$");
            Regex meaningfulWordMatcher = new Regex("[a-zA-Z]{3}");

            // Because of the way this book is laid out, everything is just a stream of raw paragraphs,
            // which we have to tease out after the fact into topics & breaks. Very tedious.
            List<string> rawParagraphs = new List<string>(100000);
            bool started = false;
            foreach (VirtualPath file in fileSystem.ListFiles(epubContentDirectory).OrderBy(s => s.Name))
            {
                Match fileNameMatch = CHAPTER_FILE_MATCHER.Match(file.Name);
                if (fileNameMatch.Success)
                {
                    using (Stream fileStream = fileSystem.OpenStream(file, FileOpenMode.Open, FileAccessMode.Read))
                    {
                        logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "Parsing epub file \"{0}\"", file.FullName);

                        HtmlDocument html = new HtmlDocument();
                        html.Load(fileStream);
                        HtmlNodeNavigator navigator = html.CreateNavigator() as HtmlNodeNavigator;
                        XPathNodeIterator iter = navigator.Select("/html/body/div/p");
                        while (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
                        {
                            string innerText = WebUtility.HtmlDecode(currentNav.CurrentNode.InnerText);
                            // Skip letter headers
                            if (letterHeaderMatcher.IsMatch(innerText))
                            {
                                started = true;
                                continue;
                            }

                            if (!started)
                            {
                                continue;
                            }

                            if (pageNumberMatcher.IsMatch(innerText))
                            {
                                continue;
                            }

                            // If it's an inline "See ATONEMENT...." block right after a topic, split that
                            // into a separate line for easier processing later
                            Match match = seeAlsoSplitterMatcher.Match(innerText);
                            if (match.Success)
                            {
                                if (match.Groups[1].Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                                {
                                    rawParagraphs.Add(match.Groups[1].Value);
                                }

                                rawParagraphs.Add(match.Groups[2].Value);
                            }
                            else
                            {
                                rawParagraphs.Add(innerText);
                            }
                        }
                    }
                }
            }

            // Now split the paragraph stream into topics
            List<string>? mostRecentTopic = null;
            List<string> allTopics = new List<string>();
            Dictionary<string, List<string>> topics = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (string paragraph in rawParagraphs)
            {
                Match match = topicApproxMatcher.Match(paragraph);
                if (match.Success && !topics.ContainsKey(match.Groups[1].Value) && !paragraph.Contains("Discourses of Wilford Woodruff"))
                {
                    mostRecentTopic = new List<string>();
                    string topicString = match.Groups[1].Value.Trim().TrimEnd(')').TrimEnd();
                    topics.Add(topicString, mostRecentTopic);
                    allTopics.Add(topicString);
                }
                else if (mostRecentTopic != null)
                {
                    mostRecentTopic.Add(StringUtils.RegexReplace(spaceCollapser, paragraph, " ").Trim());
                }
            }

            // Remove all lines that are just repetitions of a nearby topic
            const int searchRadius = 5;
            for (int center = 0; center < allTopics.Count; center++)
            {
                string thisTopic = allTopics[center];
                for (int around = Math.Max(0, center - searchRadius); around < Math.Min(allTopics.Count, center + searchRadius); around++)
                {
                    string otherTopic = allTopics[around];
                    topics[otherTopic].RemoveAll(s => s.Equals(thisTopic, StringComparison.OrdinalIgnoreCase));
                }
            }

            // Now we can get some structure
            HashSet<KnowledgeGraphNodeId> scriptures = new HashSet<KnowledgeGraphNodeId>();
            foreach (string topic in allTopics)
            {
                string topicId = topic.ToLowerInvariant();
                KnowledgeGraphNodeId nodeId = FeatureToNodeMapping.BookChapter(BOOK_ID, topicId);

                List<ParsedBodyParagraph> parsedParagraphs = new List<ParsedBodyParagraph>();
                List<string> seeAlso = new List<string>();
                bool lastParaWasSeeAlso = false;
                int paraNum = 1;

                //logger.Log($"      TOPIC \"{topic}\"");
                foreach (string para in topics[topic])
                {
                    Match match = seeAlsoMatcher.Match(para);
                    if (match.Success)
                    {
                        foreach (string see in match.Groups[1].Value.Split(','))
                        {
                            string trimmed = see.Trim().Trim('.');
                            if (!string.IsNullOrWhiteSpace(trimmed))
                            {
                                seeAlso.Add(trimmed);
                            }
                        }
                        lastParaWasSeeAlso = true;
                        continue;
                    }

                    if (lastParaWasSeeAlso)
                    {
                        match = seeAlsoExtendedMatcher.Match(para);
                        if (match.Success)
                        {
                            seeAlso.AddRange(para.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                            lastParaWasSeeAlso = true;
                            continue;
                        }
                    }

                    if (!meaningfulWordMatcher.Match(para).Success) // It's some OCR error, line doesn't contain a single word of at least 3 letters
                    {
                        continue;
                    }

                    //if (para.Length > 100)
                    //{
                    //    logger.Log(para.Substring(0, 100));
                    //}
                    //else
                    //{
                    //    logger.Log(para);
                    //}

                    scriptures.Clear();
                    foreach (ScriptureReference scripture in ScriptureMetadataEnglish.ParseAllReferences(para))
                    {
                        KnowledgeGraphNodeId scriptureNodeId = scripture.ToNodeId();
                        if (!scriptures.Contains(scriptureNodeId))
                        {
                            //logger.Log($"Refers to \"{scripture}\"");
                            scriptures.Add(scriptureNodeId);
                        }
                    }

                    parsedParagraphs.Add(new ParsedBodyParagraph()
                    {
                        ParaEntityId = FeatureToNodeMapping.BookChapterParagraph(BOOK_ID, topicId, paraNum.ToString()),
                        References = scriptures.Count == 0 ? EMPTY_NODE_SET : new HashSet<KnowledgeGraphNodeId>(scriptures),
                        Text = para,
                        ParagraphNum = paraNum,
                    });

                    lastParaWasSeeAlso = false;
                    paraNum++;
                }

                //logger.Log("See also: " + string.Join(',', seeAlso));
                logger.Log("Parsed topic " + topic);

                int textLength = parsedParagraphs.Sum(s => s.Text.Length);
                if (textLength < 30)
                {
                    if (textLength > 0)
                    {
                        //logger.Log("This is very short; text will be trimmed", LogLevel.Wrn);
                    }

                    if (seeAlso.Count != 0)
                    {
                        yield return new ParsedTopic()
                        {
                            DocumentEntityId = nodeId,
                            Title = topic,
                            BodyParagraphs = new List<ParsedBodyParagraph>(),
                            SeeAlso = seeAlso
                        };
                    }
                }
                else
                {
                    yield return new ParsedTopic()
                    {
                        DocumentEntityId = nodeId,
                        Title = topic,
                        BodyParagraphs = parsedParagraphs,
                        SeeAlso = seeAlso
                    };
                }
            }
        }

        public record struct ParsedTopic
        {
            public KnowledgeGraphNodeId DocumentEntityId;
            public string Title;
            public List<string> SeeAlso;
            public List<ParsedBodyParagraph> BodyParagraphs;
        }

        public record struct ParsedBodyParagraph
        {
            public KnowledgeGraphNodeId ParaEntityId;
            public string Text;
            public IReadOnlySet<KnowledgeGraphNodeId> References;
            public int ParagraphNum;
        }
    }
}
