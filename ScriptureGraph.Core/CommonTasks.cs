using Durandal.Common.File;
using Durandal.Common.Instrumentation;
using Durandal.Common.Logger;
using Durandal.Common.Net.Http;
using Durandal.Common.Tasks;
using Durandal.Common.Time;
using Durandal.Common.Utils.NativePlatform;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Schemas.Documents;
using ScriptureGraph.Core.Training;
using ScriptureGraph.Core.Training.Extractors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScriptureGraph.Core
{
    public static class CommonTasks
    {
        /// <summary>
        /// Builds a master entity graph over all data
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="pageCache"></param>
        /// <returns></returns>
        public static async Task BuildUniversalGraph(ILogger logger, TrainingKnowledgeGraph startGraph, WebPageCache pageCache, IFileSystem epubFileSystem)
        {
            using (FixedCapacityThreadPool threadPool = new FixedCapacityThreadPool(
                new TaskThreadPool(),
                NullLogger.Singleton,
                NullMetricCollector.Singleton,
                DimensionSet.Empty,
                "TrainingThreads",
                maxCapacity: Environment.ProcessorCount,
                overschedulingBehavior: ThreadPoolOverschedulingBehavior.QuadraticThrottle,
                overschedulingParam: TimeSpan.FromMilliseconds(5)))
            {
                WebCrawler crawler = new WebCrawler(new PortableHttpClientFactory(), pageCache);
                DocumentProcessorForFeatureExtraction processor = new DocumentProcessorForFeatureExtraction(startGraph, threadPool);
                //await CrawlStandardWorks(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                //await CrawlReferenceMaterials(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                //await CrawlGeneralConference(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                //await CrawlByuSpeeches(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                //await CrawlHymns(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                await CrawlProclamations(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                logger.Log("Processing documents from local sources");
                //BookExtractorATGQ.ExtractFeatures(
                //    epubFileSystem,
                //    new VirtualPath(@"Answers to Gospel Questions, Vo - Joseph Fielding Smith.epub"),
                //    logger, startGraph.Train, threadPool);

                //BookExtractorMD.ExtractFeatures(
                //    epubFileSystem,
                //    new VirtualPath(@"Mormon Doctrine (2nd Ed.) - Bruce R. McConkie.epub"),
                //    logger, startGraph.Train, threadPool);

                //BookExtractorMessiah.ExtractFeatures(
                //    epubFileSystem,
                //    new VirtualPath(@"The Messiah Series_ Promised Me - Bruce R. McConkie.epub"),
                //    logger, startGraph.Train, threadPool);

                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromHours(2)))
                {
                    try
                    {
                        CancellationToken cancelToken = cts.Token;
                        while (!cts.Token.IsCancellationRequested && threadPool.TotalWorkItems > 0)
                        {
                            logger.Log("Winding down training threads " + threadPool.TotalWorkItems);
                            await threadPool.WaitForCurrentTasksToFinish(cts.Token, DefaultRealTimeProvider.Singleton);
                        }
                    }
                    catch (OperationCanceledException) { }
                }
            }
        }

        /// <summary>
        /// Builds a search index over conference talks and BD topics
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="pageCache"></param>
        /// <returns></returns>
        public static async Task<Tuple<TrainingKnowledgeGraph, EntityNameIndex>> BuildSearchIndex(ILogger logger, WebPageCache pageCache, IFileSystem epubFileSystem)
        {
            using (FixedCapacityThreadPool threadPool = new FixedCapacityThreadPool(
                new TaskThreadPool(),
                NullLogger.Singleton,
                NullMetricCollector.Singleton,
                DimensionSet.Empty,
                "TrainingThreads",
                overschedulingBehavior: ThreadPoolOverschedulingBehavior.BlockUntilThreadsAvailable))
            {
                WebCrawler crawler = new WebCrawler(new PortableHttpClientFactory(), pageCache);
                TrainingKnowledgeGraph entitySearchGraph = new TrainingKnowledgeGraph();
                EntityNameIndex nameIndex = new EntityNameIndex();
                DocumentProcessorForSearchIndex processor = new DocumentProcessorForSearchIndex(entitySearchGraph, nameIndex, threadPool);
                await CrawlReferenceMaterials(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                await CrawlGeneralConference(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                await CrawlByuSpeeches(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                await CrawlHymns(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                await CrawlProclamations(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                logger.Log("Processing documents from local sources");
                BookExtractorATGQ.ExtractSearchIndexFeatures(
                    epubFileSystem, new VirtualPath(@"Answers to Gospel Questions, Vo - Joseph Fielding Smith.epub"), logger, entitySearchGraph.Train, nameIndex);
                BookExtractorMD.ExtractSearchIndexFeatures(
                    epubFileSystem, new VirtualPath(@"Mormon Doctrine (2nd Ed.) - Bruce R. McConkie.epub"), logger, entitySearchGraph.Train, nameIndex);
                BookExtractorMessiah.ExtractSearchIndexFeatures(
                    epubFileSystem, new VirtualPath(@"The Messiah Series_ Promised Me - Bruce R. McConkie.epub"), logger, entitySearchGraph.Train, nameIndex);

                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
                {
                    try
                    {
                        CancellationToken cancelToken = cts.Token;
                        while (!cts.Token.IsCancellationRequested && threadPool.TotalWorkItems > 0)
                        {
                            logger.Log("Winding down training threads " + threadPool.TotalWorkItems);
                            await threadPool.WaitForCurrentTasksToFinish(cts.Token, DefaultRealTimeProvider.Singleton);
                        }
                    }
                    catch (OperationCanceledException) { }
                }

                return new Tuple<TrainingKnowledgeGraph, EntityNameIndex>(entitySearchGraph, nameIndex);
            }
        }

        /// <summary>
        /// Extracts structured documents from web pages to be loaded into a reader
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="pageCache"></param>
        /// <returns></returns>
        public static async Task ParseDocuments(ILogger logger, WebPageCache pageCache, IFileSystem documentFileSystem, IFileSystem epubFileSystem)
        {
            using (FixedCapacityThreadPool threadPool = new FixedCapacityThreadPool(
                new TaskThreadPool(),
                NullLogger.Singleton,
                NullMetricCollector.Singleton,
                DimensionSet.Empty,
                "TrainingThreads",
                overschedulingBehavior: ThreadPoolOverschedulingBehavior.BlockUntilThreadsAvailable))
            {
                WebCrawler crawler = new WebCrawler(new PortableHttpClientFactory(), pageCache);
                DocumentProcessorForDocumentParsing processor = new DocumentProcessorForDocumentParsing(documentFileSystem, threadPool);
                logger.Log("Processing documents from webcrawler sources");
                await CrawlStandardWorks(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                await CrawlBibleDictionary(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                await CrawlGeneralConference(crawler, processor.ProcessFromWebCrawler, logger);
                await CrawlByuSpeeches(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                await CrawlHymns(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                await CrawlProclamations(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
                logger.Log("Processing documents from local sources");
                Book_ATGQ_ExtractDocuments(documentFileSystem, epubFileSystem, new VirtualPath(@"Answers to Gospel Questions, Vo - Joseph Fielding Smith.epub"), logger);
                Book_MD_ExtractDocuments(documentFileSystem, epubFileSystem, new VirtualPath(@"Mormon Doctrine (2nd Ed.) - Bruce R. McConkie.epub"), logger);
                Book_Messiah_ExtractDocuments(documentFileSystem, epubFileSystem, new VirtualPath(@"The Messiah Series_ Promised Me - Bruce R. McConkie.epub"), logger);

                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
                {
                    try
                    {
                        CancellationToken cancelToken = cts.Token;
                        while (!cts.Token.IsCancellationRequested && threadPool.TotalWorkItems > 0)
                        {
                            logger.Log("Winding down training threads " + threadPool.TotalWorkItems);
                            await threadPool.WaitForCurrentTasksToFinish(cts.Token, DefaultRealTimeProvider.Singleton);
                        }
                    }
                    catch (OperationCanceledException) { }
                }
            }
        }

        /// <summary>
        /// Web page processor which extracts all entity features from pages to build a universal index
        /// </summary>
        private class DocumentProcessorForFeatureExtraction
        {
            private static readonly Regex ScriptureChapterUrlMatcher = new Regex("\\/study\\/scriptures\\/(?:bofm|ot|nt|dc-testament|pgp)\\/.+?\\/\\d+");
            private static readonly Regex ReferenceUrlMatcher = new Regex("\\/study\\/scriptures\\/(tg|bd|gs|triple-index)\\/.+?(?:\\?|$)");
            private static readonly Regex ConferenceTalkUrlMatcher = new Regex("\\/study\\/general-conference\\/\\d+\\/\\d+\\/.+?(?:\\?|$)");
            private static readonly Regex ByuSpeechUrlMatcher = new Regex("\\/talks\\/.+?\\/.+?(?:/?|$)");
            private static readonly Regex ByuSpeakerUrlMatcher = new Regex("\\/speakers\\/.+?(?:/?|$)");
            private static readonly Regex HymnUrlMatcher = new Regex("\\/media\\/music\\/songs\\/.+?(?:/?|$)");
            private readonly TrainingKnowledgeGraph _trainingGraph;
            private readonly IThreadPool _trainingThreadPool;

            public DocumentProcessorForFeatureExtraction(TrainingKnowledgeGraph graph, IThreadPool threadPool)
            {
                _trainingGraph = graph;
                _trainingThreadPool = threadPool;
            }

            public Task<bool> ProcessFromWebCrawlerThreaded(WebCrawler.CrawledPage page, ILogger logger)
            {
                _trainingThreadPool.EnqueueUserAsyncWorkItem(async () =>
                {
                    await ProcessFromWebCrawler(page, logger);
                });

                return Task.FromResult<bool>(true);
            }

            public Task<bool> ProcessFromWebCrawler(WebCrawler.CrawledPage page, ILogger logger)
            {
                try
                {
                    List<TrainingFeature> features = new List<TrainingFeature>(50000);
                    if (ScriptureChapterUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        logger.Log($"Featurizing scripture page {page.Url.AbsolutePath}");
                        ScripturePageFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, features);
                    }
                    else if (ReferenceUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        Match match = ReferenceUrlMatcher.Match(page.Url.AbsolutePath);
                        if (string.Equals(match.Groups[1].Value, "tg", StringComparison.Ordinal))
                        {
                            logger.Log($"Featurizing TG page {page.Url.AbsolutePath}");
                            TopicalGuideFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, features);
                        }
                        else if (string.Equals(match.Groups[1].Value, "bd", StringComparison.Ordinal))
                        {
                            logger.Log($"Featurizing BD page {page.Url.AbsolutePath}");
                            BibleDictionaryFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, features);
                        }
                        else if (string.Equals(match.Groups[1].Value, "gs", StringComparison.Ordinal))
                        {
                            logger.Log($"Featurizing GS page {page.Url.AbsolutePath}");
                            GuideToScripturesFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, features);
                        }
                        else if (string.Equals(match.Groups[1].Value, "triple-index", StringComparison.Ordinal))
                        {
                            logger.Log($"Featurizing index page {page.Url.AbsolutePath}");
                            TripleIndexFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, features);
                        }
                        else
                        {
                            logger.Log($"Unknown page type {page.Url.AbsolutePath}", LogLevel.Wrn);
                        }
                    }
                    else if (ConferenceTalkUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        logger.Log($"Featurizing conference talk {page.Url.AbsolutePath}");
                        ConferenceTalkFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, features);
                    }
                    else if (ByuSpeakerUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                    }
                    else if (ByuSpeechUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        logger.Log($"Featurizing BYU speech {page.Url.AbsolutePath}");
                        ByuSpeechFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, _trainingGraph.Train);
                    }
                    else if (HymnUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        logger.Log($"Featurizing hymn {page.Url.AbsolutePath}");
                        HymnsFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, _trainingGraph.Train);
                    }
                    else if (page.Url.AbsolutePath.Contains("the-living-christ-the-testimony-of-the-apostles") ||
                        page.Url.AbsolutePath.Contains("the-family-a-proclamation-to-the-world"))
                    {
                        logger.Log($"Featurizing proclamation {page.Url.AbsolutePath}");
                        ProclamationsFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, _trainingGraph.Train);
                    }
                    else
                    {
                        logger.Log($"Unknown page type {page.Url.AbsolutePath}", LogLevel.Wrn);
                    }

                    foreach (var feature in features)
                    {
                        _trainingGraph.Train(feature);
                    }
                }
                catch (Exception e)
                {
                    logger.Log(e);
                }

                return Task.FromResult<bool>(true);
            }
        }

        /// <summary>
        /// Web page processor which extracts search features from pages to build a search index
        /// </summary>
        private class DocumentProcessorForSearchIndex
        {
            private static readonly Regex ScriptureChapterUrlMatcher = new Regex("\\/study\\/scriptures\\/(?:bofm|ot|nt|dc-testament|pgp)\\/.+?\\/\\d+");
            private static readonly Regex ReferenceUrlMatcher = new Regex("\\/study\\/scriptures\\/(tg|bd|gs|triple-index)\\/.+?(?:\\?|$)");
            private static readonly Regex ConferenceTalkUrlMatcher = new Regex("\\/study\\/general-conference\\/\\d+\\/\\d+\\/.+?(?:\\?|$)");
            private static readonly Regex ByuSpeechUrlMatcher = new Regex("\\/talks\\/.+?\\/.+?(?:/?|$)");
            private static readonly Regex ByuSpeakerUrlMatcher = new Regex("\\/speakers\\/.+?(?:/?|$)");
            private static readonly Regex HymnUrlMatcher = new Regex("\\/media\\/music\\/songs\\/.+?(?:/?|$)");
            private readonly TrainingKnowledgeGraph _trainingGraph;
            private readonly EntityNameIndex _nameIndex;
            private readonly IThreadPool _trainingThreadPool;

            public DocumentProcessorForSearchIndex(TrainingKnowledgeGraph graph, EntityNameIndex nameIndex, IThreadPool threadPool)
            {
                _trainingGraph = graph;
                _nameIndex = nameIndex;
                _trainingThreadPool = threadPool;
            }

            public Task<bool> ProcessFromWebCrawlerThreaded(WebCrawler.CrawledPage page, ILogger logger)
            {
                _trainingThreadPool.EnqueueUserAsyncWorkItem(async () =>
                {
                    await ProcessFromWebCrawler(page, logger);
                });

                return Task.FromResult<bool>(true);
            }

            public Task<bool> ProcessFromWebCrawler(WebCrawler.CrawledPage page, ILogger logger)
            {
                try
                {
                    List<TrainingFeature> features = new List<TrainingFeature>();

                    if (ScriptureChapterUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                    }
                    else if (ReferenceUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        Match match = ReferenceUrlMatcher.Match(page.Url.AbsolutePath);
                        if (string.Equals(match.Groups[1].Value, "bd", StringComparison.Ordinal))
                        {
                            logger.Log($"Building search index from BD page {page.Url.AbsolutePath}");
                            BibleDictionaryFeatureExtractor.ExtractSearchIndexFeatures(page.Html, page.Url, logger, features, _nameIndex);
                        }
                        else if (string.Equals(match.Groups[1].Value, "gs", StringComparison.Ordinal))
                        {
                            logger.Log($"Building search index from GS page {page.Url.AbsolutePath}");
                            GuideToScripturesFeatureExtractor.ExtractSearchIndexFeatures(page.Html, page.Url, logger, features, _nameIndex);
                        }
                        else if (string.Equals(match.Groups[1].Value, "tg", StringComparison.Ordinal))
                        {
                            logger.Log($"Building search index from TG page {page.Url.AbsolutePath}");
                            TopicalGuideFeatureExtractor.ExtractSearchIndexFeatures(page.Html, page.Url, logger, features, _nameIndex);
                        }
                        else if (string.Equals(match.Groups[1].Value, "triple-index", StringComparison.Ordinal))
                        {
                            logger.Log($"Building search index from triple index page {page.Url.AbsolutePath}");
                            TripleIndexFeatureExtractor.ExtractSearchIndexFeatures(page.Html, page.Url, logger, features, _nameIndex);
                        }
                        else
                        {
                            logger.Log($"Unknown page type {page.Url.AbsolutePath}", LogLevel.Wrn);
                        }
                    }
                    else if (ConferenceTalkUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        logger.Log($"Building search index from conference talk {page.Url.AbsolutePath}");
                        ConferenceTalkFeatureExtractor.ExtractSearchIndexFeatures(page.Html, page.Url, logger, _trainingGraph.Train, _nameIndex);
                    }
                    else if (ByuSpeakerUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                    }
                    else if (ByuSpeechUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        logger.Log($"Building search index from BYU speech {page.Url.AbsolutePath}");
                        ByuSpeechFeatureExtractor.ExtractSearchIndexFeatures(page.Html, page.Url, logger, _trainingGraph.Train, _nameIndex);
                    }
                    else if (HymnUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        logger.Log($"Building search index from hymn {page.Url.AbsolutePath}");
                        HymnsFeatureExtractor.ExtractSearchIndexFeatures(page.Html, page.Url, logger, _trainingGraph.Train, _nameIndex);
                    }
                    else if (page.Url.AbsolutePath.Contains("the-living-christ-the-testimony-of-the-apostles") ||
                        page.Url.AbsolutePath.Contains("the-family-a-proclamation-to-the-world"))
                    {
                        logger.Log($"Building search index from proclamation {page.Url.AbsolutePath}");
                        ProclamationsFeatureExtractor.ExtractSearchIndexFeatures(page.Html, page.Url, logger, _trainingGraph.Train, _nameIndex);
                    }
                    else
                    {
                        logger.Log($"Unknown page type {page.Url.AbsolutePath}", LogLevel.Wrn);
                    }

                    foreach (var feature in features)
                    {
                        _trainingGraph.Train(feature);
                    }
                }
                catch (Exception e)
                {
                    logger.Log(e);
                }

                return Task.FromResult<bool>(true);
            }
        }

        /// <summary>
        /// Web page processor which extracts all entity features from pages to build a universal index
        /// </summary>
        private class DocumentProcessorForDocumentParsing
        {
            private static readonly Regex ScriptureChapterUrlMatcher = new Regex("\\/study\\/scriptures\\/(?:bofm|ot|nt|dc-testament|pgp)\\/.+?\\/\\d+");
            private static readonly Regex ReferenceUrlMatcher = new Regex("\\/study\\/scriptures\\/(tg|bd|gs|triple-index)\\/.+?(?:\\?|$)");
            private static readonly Regex ConferenceTalkUrlMatcher = new Regex("\\/study\\/general-conference\\/\\d+\\/\\d+\\/.+?(?:\\?|$)");
            private static readonly Regex ByuSpeechUrlMatcher = new Regex("\\/talks\\/.+?\\/.+?(?:/?|$)");
            private static readonly Regex ByuSpeakerUrlMatcher = new Regex("\\/speakers\\/.+?(?:/?|$)");
            private static readonly Regex HymnUrlMatcher = new Regex("\\/media\\/music\\/songs\\/.+?(?:/?|$)");
            private readonly IThreadPool _trainingThreadPool;
            private readonly IFileSystem _documentCacheFileSystem;

            public DocumentProcessorForDocumentParsing(IFileSystem documentCacheFileSystem, IThreadPool threadPool)
            {
                _documentCacheFileSystem = documentCacheFileSystem;
                _trainingThreadPool = new FixedCapacityThreadPool(
                    threadPool,
                    NullLogger.Singleton,
                    NullMetricCollector.Singleton,
                    DimensionSet.Empty,
                    "TrainingThreads");
            }

            public Task<bool> ProcessFromWebCrawlerThreaded(WebCrawler.CrawledPage page, ILogger logger)
            {
                _trainingThreadPool.EnqueueUserAsyncWorkItem(async () =>
                {
                    await ProcessFromWebCrawler(page, logger);
                });

                return Task.FromResult<bool>(true);
            }

            public Task<bool> ProcessFromWebCrawler(WebCrawler.CrawledPage page, ILogger logger)
            {
                try
                {
                    VirtualPath fileDestination = VirtualPath.Root;
                    GospelDocument? parsedDoc = null;
                    if (ScriptureChapterUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        logger.Log($"Parsing scripture page {page.Url.AbsolutePath}");
                        ScriptureChapterDocument? structuredDoc = ScripturePageFeatureExtractor.ParseDocument(page.Html, page.Url, logger);
                        parsedDoc = structuredDoc;
                        if (structuredDoc == null)
                        {
                            logger.Log($"Did not parse a page from {page.Url.AbsolutePath}", LogLevel.Err);
                        }
                        else
                        {
                            fileDestination = new VirtualPath($"{structuredDoc.Language.ToBcp47Alpha3String()}\\{structuredDoc.Canon}\\{structuredDoc.Book}-{structuredDoc.Chapter}.json.br");
                        }
                    }
                    else if (ReferenceUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        Match match = ReferenceUrlMatcher.Match(page.Url.AbsolutePath);
                        if (string.Equals(match.Groups[1].Value, "bd", StringComparison.Ordinal))
                        {
                            logger.Log($"Parsing BD page {page.Url.AbsolutePath}");
                            BibleDictionaryDocument? structuredDoc = BibleDictionaryFeatureExtractor.ParseDocument(page.Html, page.Url, logger);
                            parsedDoc = structuredDoc;
                            if (structuredDoc == null)
                            {
                                logger.Log($"Did not parse a page from {page.Url.AbsolutePath}", LogLevel.Err);
                            }
                            else
                            {
                                fileDestination = new VirtualPath($"{structuredDoc.Language.ToBcp47Alpha3String()}\\bd\\{FilePathSanitizer.SanitizeFileName(structuredDoc.TopicId)}.json.br");
                            }
                        }
                        else if (string.Equals(match.Groups[1].Value, "tg", StringComparison.Ordinal) ||
                                 string.Equals(match.Groups[1].Value, "gs", StringComparison.Ordinal) ||
                                 string.Equals(match.Groups[1].Value, "triple-index", StringComparison.Ordinal))
                        {
                        }
                        else
                        {
                            logger.Log($"Unknown page type {page.Url.AbsolutePath}", LogLevel.Wrn);
                        }
                    }
                    else if (ConferenceTalkUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        logger.Log($"Parsing conference talk {page.Url.AbsolutePath}");
                        ConferenceTalkDocument? structuredDoc = ConferenceTalkFeatureExtractor.ParseDocument(page.Html, page.Url, logger);
                        parsedDoc = structuredDoc;
                        if (structuredDoc == null)
                        {
                            //logger.Log($"Did not parse a page from {page.Url.AbsolutePath}", LogLevel.Err);
                        }
                        else
                        {
                            fileDestination = new VirtualPath($"{structuredDoc.Language.ToBcp47Alpha3String()}\\general-conference\\{structuredDoc.Conference}\\{structuredDoc.TalkId}.json.br");
                        }
                    }
                    else if (ByuSpeakerUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                    }
                    else if (ByuSpeechUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        logger.Log($"Parsing BYU speech talk {page.Url.AbsolutePath}");
                        ByuSpeechDocument? structuredDoc = ByuSpeechFeatureExtractor.ParseDocument(page.Html, page.Url, logger);
                        parsedDoc = structuredDoc;
                        if (structuredDoc == null)
                        {
                            //logger.Log($"Did not parse a page from {page.Url.AbsolutePath}", LogLevel.Err);
                        }
                        else
                        {
                            fileDestination = new VirtualPath($"{structuredDoc.Language.ToBcp47Alpha3String()}\\byu\\{FilePathSanitizer.SanitizeFileName(structuredDoc.TalkId)}.json.br");
                        }
                    }
                    else if (HymnUrlMatcher.Match(page.Url.AbsolutePath).Success)
                    {
                        logger.Log($"Parsing hymn {page.Url.AbsolutePath}");
                        HymnDocument? structuredDoc = HymnsFeatureExtractor.ParseDocument(page.Html, page.Url, logger);
                        parsedDoc = structuredDoc;
                        if (structuredDoc == null)
                        {
                            //logger.Log($"Did not parse a page from {page.Url.AbsolutePath}", LogLevel.Err);
                        }
                        else
                        {
                            fileDestination = new VirtualPath($"{structuredDoc.Language.ToBcp47Alpha3String()}\\hymns\\{FilePathSanitizer.SanitizeFileName(structuredDoc.SongId)}.json.br");
                        }
                    }
                    else if (page.Url.AbsolutePath.Contains("the-living-christ-the-testimony-of-the-apostles") ||
                        page.Url.AbsolutePath.Contains("the-family-a-proclamation-to-the-world"))
                    {
                        logger.Log($"Parsing proclamation {page.Url.AbsolutePath}");
                        ProclamationDocument? structuredDoc = ProclamationsFeatureExtractor.ParseDocument(page.Html, page.Url, logger);
                        parsedDoc = structuredDoc;
                        if (structuredDoc == null)
                        {
                            //logger.Log($"Did not parse a page from {page.Url.AbsolutePath}", LogLevel.Err);
                        }
                        else
                        {
                            fileDestination = new VirtualPath($"{structuredDoc.Language.ToBcp47Alpha3String()}\\proc\\{FilePathSanitizer.SanitizeFileName(structuredDoc.ProclamationId)}.json.br");
                        }
                    }
                    else
                    {
                        logger.Log($"Unknown page type {page.Url.AbsolutePath}", LogLevel.Wrn);
                    }

                    if (parsedDoc != null)
                    {
                        _documentCacheFileSystem.CreateDirectory(fileDestination.Container);

                        using (Stream fileOut = _documentCacheFileSystem.OpenStream(fileDestination, FileOpenMode.Create, FileAccessMode.Write))
                        using (BrotliStream brotliStream = new BrotliStream(fileOut, CompressionLevel.SmallestSize))
                        {
                            GospelDocument.SerializePolymorphic(brotliStream, parsedDoc);
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Log(e);
                }

                return Task.FromResult<bool>(true);
            }
        }

        private static async Task CrawlGeneralConference(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            HashSet<Regex> allowedUrls =
            [
                new Regex("^https://www.churchofjesuschrist.org/study/general-conference\\?lang=eng$"), // overall conference index
                new Regex("^https://www.churchofjesuschrist.org/study/general-conference/\\d+\\?lang=eng$"), // decade index pages
                new Regex("^https://www.churchofjesuschrist.org/study/general-conference/\\d+/\\d+\\?lang=eng$"), // conference index page
                new Regex("^https://www.churchofjesuschrist.org/study/general-conference/\\d+/\\d+/.+?\\?lang=eng$"), // specific talks
            ];

            //HashSet<Regex> allowedUrls =
            //[
            //    new Regex("^https://www.churchofjesuschrist.org/study/general-conference\\?lang=eng$"), // overall conference index
            //    new Regex("^https://www.churchofjesuschrist.org/study/general-conference/2025/\\d+\\?lang=eng$"), // conference index page
            //    new Regex("^https://www.churchofjesuschrist.org/study/general-conference/2025/\\d+/.+?\\?lang=eng$"), // specific talks
            //];

            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/general-conference?lang=eng"),
                //new Uri("https://www.churchofjesuschrist.org/study/general-conference/2002/04/we-look-to-christ?lang=eng"),
                pageAction,
                logger.Clone("WebCrawler-GC"),
                allowedUrls);
        }

        private static async Task CrawlBookOfMormon(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            HashSet<Regex> allowedUrls =
            [
                new Regex("^https://www.churchofjesuschrist.org/study/scriptures/bofm/.+?/\\d+\\?lang=eng$")
            ];

            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/bofm?lang=eng"),
                pageAction,
                logger.Clone("WebCrawler-BoM"),
                allowedUrls);
        }

        private static async Task CrawlOldTestament(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            HashSet<Regex> allowedUrls =
            [
                new Regex("^https://www.churchofjesuschrist.org/study/scriptures/ot/.+?/\\d+\\?lang=eng$")
            ];

            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/ot?lang=eng"),
                pageAction,
                logger.Clone("WebCrawler-OT"),
                allowedUrls);
        }

        private static async Task CrawlNewTestament(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            HashSet<Regex> allowedUrls =
            [
                new Regex("^https://www.churchofjesuschrist.org/study/scriptures/nt/.+?/\\d+\\?lang=eng$")
                //new Regex("^https://www.churchofjesuschrist.org/study/scriptures/nt/matt/1\\?lang=eng$")
            ];

            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/nt?lang=eng"),
                //new Uri("https://www.churchofjesuschrist.org/study/scriptures/nt/matt/1?lang=eng"),
                pageAction,
                logger.Clone("WebCrawler-NT"),
                allowedUrls);
        }

        private static async Task CrawlDC(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            HashSet<Regex> allowedUrls =
            [
                new Regex("^https://www.churchofjesuschrist.org/study/scriptures/dc-testament/.+?/\\d+\\?lang=eng$")
            ];

            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/dc-testament?lang=eng"),
                pageAction,
                logger.Clone("WebCrawler-DC"),
                allowedUrls);
        }

        private static async Task CrawlPGP(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            HashSet<Regex> allowedUrls =
            [
                new Regex("^https://www.churchofjesuschrist.org/study/scriptures/pgp/.+?/\\d+\\?lang=eng$")
            ];

            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/pgp?lang=eng"),
                pageAction,
                logger.Clone("WebCrawler-PGP"),
                allowedUrls);
        }

        public static async Task CrawlStandardWorks(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            await CrawlBookOfMormon(crawler, pageAction, logger);
            await CrawlOldTestament(crawler, pageAction, logger);
            await CrawlNewTestament(crawler, pageAction, logger);
            await CrawlDC(crawler, pageAction, logger);
            await CrawlPGP(crawler, pageAction, logger);
        }

        private static async Task CrawlBibleDictionary(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            HashSet<Regex> allowedUrls =
            [
                new Regex("^https://www.churchofjesuschrist.org/study/scriptures/bd/.+?\\?lang=eng$")
            ];

            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/bd?lang=eng"),
                pageAction,
                logger.Clone("WebCrawler-BD"),
                allowedUrls);
        }

        private static async Task CrawlTopicalGuide(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            HashSet<Regex> allowedUrls =
            [
                new Regex("^https://www.churchofjesuschrist.org/study/scriptures/tg/.+?\\?lang=eng$")
            ];

            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/tg?lang=eng"),
                pageAction,
                logger.Clone("WebCrawler-TG"),
                allowedUrls);
        }

        private static async Task CrawlTripleIndex(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            HashSet<Regex> allowedUrls =
            [
                new Regex("^https://www.churchofjesuschrist.org/study/scriptures/triple-index/.+?\\?lang=eng$")
            ];

            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/triple-index?lang=eng"),
                pageAction,
                logger.Clone("WebCrawler-TI"),
                allowedUrls);
        }

        private static async Task CrawlGuideToScriptures(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            HashSet<Regex> allowedUrls =
            [
                new Regex("^https://www.churchofjesuschrist.org/study/scriptures/gs/.+?\\?lang=eng$")
            ];

            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/gs?lang=eng"),
                pageAction,
                logger.Clone("WebCrawler-GS"),
                allowedUrls);
        }

        private static async Task CrawlReferenceMaterials(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            await CrawlTopicalGuide(crawler, pageAction, logger);
            await CrawlTripleIndex(crawler, pageAction, logger);
            await CrawlGuideToScriptures(crawler, pageAction, logger);
            await CrawlBibleDictionary(crawler, pageAction, logger);
        }

        private static async Task CrawlByuSpeeches(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            HashSet<Regex> allowedUrls =
            [
                new Regex("^https://speeches.byu.edu/speakers/?$"),
                new Regex("^https://speeches.byu.edu/speakers/[^\\/]+/?$"),
                new Regex("^https://speeches.byu.edu/talks/[^\\/]+/[^\\/]+/?$"),
            ];

            await crawler.Crawl(
                new Uri("https://speeches.byu.edu/speakers/"),
                //new Uri("https://speeches.byu.edu/speakers/jeffrey-r-holland/"),
                pageAction,
                logger.Clone("WebCrawler-BYU"),
                allowedUrls);
        }

        private static async Task CrawlHymns(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            logger = logger.Clone("WebCrawler-Hymns");
            foreach (Uri songPage in HymnsFeatureExtractor.GetAllSongUris())
            {
                WebCrawler.CrawledPage? page = await crawler.DirectDownload(songPage, logger);
                if (page != null)
                {
                    try
                    {
                        await pageAction(page, logger);
                    }
                    catch (Exception e)
                    {
                        logger.Log(e);
                    }
                }
            }
        }

        private static async Task CrawlProclamations(WebCrawler crawler, Func<WebCrawler.CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            logger = logger.Clone("WebCrawler-Proclamations");
            foreach (Uri procPage in new Uri[]
            {
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/the-living-christ-the-testimony-of-the-apostles/the-living-christ-the-testimony-of-the-apostles?lang=eng"),
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/the-family-a-proclamation-to-the-world/the-family-a-proclamation-to-the-world?lang=eng"),
            })
            {
                WebCrawler.CrawledPage? page = await crawler.DirectDownload(procPage, logger);
                if (page != null)
                {
                    try
                    {
                        await pageAction(page, logger);
                    }
                    catch (Exception e)
                    {
                        logger.Log(e);
                    }
                }
            }
        }

        private static void Book_ATGQ_ExtractDocuments(
            IFileSystem documentCacheFileSystem,
            IFileSystem epubFileSystem,
            VirtualPath epubPath,
            ILogger logger)
        {
            foreach (BookChapterDocument bookChapter in BookExtractorATGQ.ExtractDocuments(epubFileSystem, epubPath, logger))
            {
                VirtualPath fileDestination = new VirtualPath($"{bookChapter.Language.ToBcp47Alpha3String()}\\{bookChapter.BookId}\\{bookChapter.ChapterId}.json.br");
                if (!documentCacheFileSystem.Exists(fileDestination.Container))
                {
                    documentCacheFileSystem.CreateDirectory(fileDestination.Container);
                }

                using (Stream fileOut = documentCacheFileSystem.OpenStream(fileDestination, FileOpenMode.Create, FileAccessMode.Write))
                using (BrotliStream brotliStream = new BrotliStream(fileOut, CompressionLevel.SmallestSize))
                {
                    GospelDocument.SerializePolymorphic(brotliStream, bookChapter);
                }
            }
        }

        private static void Book_MD_ExtractDocuments(
            IFileSystem documentCacheFileSystem,
            IFileSystem epubFileSystem,
            VirtualPath epubPath,
            ILogger logger)
        {
            foreach (BookChapterDocument bookChapter in BookExtractorMD.ExtractDocuments(epubFileSystem, epubPath, logger))
            {
                VirtualPath fileDestination = new VirtualPath($"{bookChapter.Language.ToBcp47Alpha3String()}\\{bookChapter.BookId}\\{bookChapter.ChapterId}.json.br");
                if (!documentCacheFileSystem.Exists(fileDestination.Container))
                {
                    documentCacheFileSystem.CreateDirectory(fileDestination.Container);
                }

                using (Stream fileOut = documentCacheFileSystem.OpenStream(fileDestination, FileOpenMode.Create, FileAccessMode.Write))
                using (BrotliStream brotliStream = new BrotliStream(fileOut, CompressionLevel.SmallestSize))
                {
                    GospelDocument.SerializePolymorphic(brotliStream, bookChapter);
                }
            }
        }

        private static void Book_Messiah_ExtractDocuments(
            IFileSystem documentCacheFileSystem,
            IFileSystem epubFileSystem,
            VirtualPath epubPath,
            ILogger logger)
        {
            foreach (BookChapterDocument bookChapter in BookExtractorMessiah.ExtractDocuments(epubFileSystem, epubPath, logger))
            {
                VirtualPath fileDestination = new VirtualPath($"{bookChapter.Language.ToBcp47Alpha3String()}\\{bookChapter.BookId}\\{bookChapter.ChapterId}.json.br");
                if (!documentCacheFileSystem.Exists(fileDestination.Container))
                {
                    documentCacheFileSystem.CreateDirectory(fileDestination.Container);
                }

                using (Stream fileOut = documentCacheFileSystem.OpenStream(fileDestination, FileOpenMode.Create, FileAccessMode.Write))
                using (BrotliStream brotliStream = new BrotliStream(fileOut, CompressionLevel.SmallestSize))
                {
                    GospelDocument.SerializePolymorphic(brotliStream, bookChapter);
                }
            }
        }
    }
}
