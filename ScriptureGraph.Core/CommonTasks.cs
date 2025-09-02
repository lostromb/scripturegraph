using Durandal.Common.File;
using Durandal.Common.Instrumentation;
using Durandal.Common.Logger;
using Durandal.Common.Net.Http;
using Durandal.Common.Tasks;
using Durandal.Common.Time;
using Durandal.Common.Utils.NativePlatform;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Training;
using ScriptureGraph.Core.Training.Extractors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static ScriptureGraph.Core.Training.WebCrawler;

namespace ScriptureGraph.Core
{
    public static class CommonTasks
    {
        public static async Task<KnowledgeGraph> BuildSearchIndex(ILogger logger, WebPageCache pageCache)
        {
            WebCrawler crawler = new WebCrawler(new PortableHttpClientFactory(), pageCache);
            KnowledgeGraph entitySearchGraph = new KnowledgeGraph();
            DocumentProcessorForSearchIndex processor = new DocumentProcessorForSearchIndex(entitySearchGraph);
            await CrawlGeneralConference(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
            await CrawlBibleDictionary(crawler, processor.ProcessFromWebCrawlerThreaded, logger);
            logger.Log("Waiting for index building to finish");
            await processor.WaitForThreadsToFinish();
            return entitySearchGraph;
        }

        /// <summary>
        /// Web page processor which extracts search features from pages to build a search index
        /// </summary>
        private class DocumentProcessorForSearchIndex
        {
            private static readonly Regex ScriptureChapterUrlMatcher = new Regex("\\/study\\/scriptures\\/(?:bofm|ot|nt|dc-testament|pgp)\\/.+?\\/\\d+");
            private static readonly Regex ReferenceUrlMatcher = new Regex("\\/study\\/scriptures\\/(tg|bd|gs|triple-index)\\/.+?(?:\\?|$)");
            private static readonly Regex ConferenceTalkUrlMatcher = new Regex("\\/study\\/general-conference\\/\\d+\\/\\d+\\/.+?(?:\\?|$)");
            private readonly KnowledgeGraph _trainingGraph;
            private readonly IThreadPool _trainingThreadPool;

            public DocumentProcessorForSearchIndex(KnowledgeGraph graph)
            {
                _trainingGraph = graph;
                _trainingThreadPool = new FixedCapacityThreadPool(
                    new TaskThreadPool(),
                    NullLogger.Singleton,
                    NullMetricCollector.Singleton,
                    DimensionSet.Empty,
                    "TrainingThreads");
            }

            public async Task WaitForThreadsToFinish()
            {
                do
                {
                    // fixme this is jank
                    await Task.Delay(10000);
                    await _trainingThreadPool.WaitForCurrentTasksToFinish(CancellationToken.None, DefaultRealTimeProvider.Singleton);
                } while (_trainingThreadPool.RunningWorkItems > 0);
            }

            public Task<bool> ProcessFromWebCrawlerThreaded(WebCrawler.CrawledPage page, ILogger logger)
            {
                _trainingThreadPool.EnqueueUserWorkItem(() => ProcessFromWebCrawler(page, logger));
                return Task.FromResult<bool>(true);
            }

            public Task<bool> ProcessFromWebCrawler(WebCrawler.CrawledPage page, ILogger logger)
            {
                List<TrainingFeature> features = new List<TrainingFeature>();
                Match match = ScriptureChapterUrlMatcher.Match(page.Url.AbsolutePath);
                if (!match.Success)
                {
                    match = ReferenceUrlMatcher.Match(page.Url.AbsolutePath);
                    if (string.Equals(match.Groups[1].Value, "bd", StringComparison.Ordinal))
                    {
                        logger.Log($"Building search index from BD page {page.Url.AbsolutePath}");
                        BibleDictionaryFeatureExtractor.ExtractSearchIndexFeatures(page.Html, page.Url, logger, features);
                    }
                    else if (string.Equals(match.Groups[1].Value, "tg", StringComparison.Ordinal) ||
                        string.Equals(match.Groups[1].Value, "gs", StringComparison.Ordinal) ||
                        string.Equals(match.Groups[1].Value, "triple-index", StringComparison.Ordinal))
                    {
                        // TODO parse these
                    }
                    else
                    {
                        match = ConferenceTalkUrlMatcher.Match(page.Url.AbsolutePath);
                        if (match.Success)
                        {
                            logger.Log($"Building search index from conference talk {page.Url.AbsolutePath}");
                            ConferenceTalkFeatureExtractor.ExtractSearchIndexFeatures(page.Html, page.Url, logger, features);
                        }
                        else
                        {
                            logger.Log($"Unknown page type {page.Url.AbsolutePath}", LogLevel.Wrn);
                        }
                    }
                }

                foreach (var feature in features)
                {
                    _trainingGraph.Train(feature);
                }

                return Task.FromResult<bool>(true);
            }
        }

        private static async Task CrawlGeneralConference(WebCrawler crawler, Func<CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            HashSet<Regex> allowedUrls =
            [
                new Regex("^https://www.churchofjesuschrist.org/study/general-conference\\?lang=eng$"), // overall conference index
                new Regex("^https://www.churchofjesuschrist.org/study/general-conference/\\d+\\?lang=eng$"), // decade index pages
                new Regex("^https://www.churchofjesuschrist.org/study/general-conference/\\d+/\\d+\\?lang=eng$"), // conference index page
                new Regex("^https://www.churchofjesuschrist.org/study/general-conference/\\d+/\\d+/.+?\\?lang=eng$"), // specific talks
            ];

            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/general-conference?lang=eng"),
                pageAction,
                logger.Clone("WebCrawler-GC"),
                allowedUrls);
        }

        private static async Task CrawlBookOfMormon(WebCrawler crawler, Func<CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
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

        private static async Task CrawlOldTestament(WebCrawler crawler, Func<CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
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

        private static async Task CrawlNewTestament(WebCrawler crawler, Func<CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            HashSet<Regex> allowedUrls =
            [
                new Regex("^https://www.churchofjesuschrist.org/study/scriptures/nt/.+?/\\d+\\?lang=eng$")
            ];

            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/nt?lang=eng"),
                pageAction,
                logger.Clone("WebCrawler-NT"),
                allowedUrls);
        }

        private static async Task CrawlDC(WebCrawler crawler, Func<CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
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

        private static async Task CrawlPGP(WebCrawler crawler, Func<CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
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

        public static async Task CrawlStandardWorks(WebCrawler crawler, Func<CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            await CrawlBookOfMormon(crawler, pageAction, logger);
            await CrawlOldTestament(crawler, pageAction, logger);
            await CrawlNewTestament(crawler, pageAction, logger);
            await CrawlDC(crawler, pageAction, logger);
            await CrawlPGP(crawler, pageAction, logger);
        }

        private static async Task CrawlBibleDictionary(WebCrawler crawler, Func<CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
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

        private static async Task CrawlTopicalGuide(WebCrawler crawler, Func<CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
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

        private static async Task CrawlTripleIndex(WebCrawler crawler, Func<CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
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

        private static async Task CrawlGuideToScriptures(WebCrawler crawler, Func<CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
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

        public static async Task CrawlReferenceMaterials(WebCrawler crawler, Func<CrawledPage, ILogger, Task<bool>> pageAction, ILogger logger)
        {
            await CrawlTopicalGuide(crawler, pageAction, logger);
            await CrawlTripleIndex(crawler, pageAction, logger);
            await CrawlGuideToScriptures(crawler, pageAction, logger);
            await CrawlBibleDictionary(crawler, pageAction, logger);
        }
    }
}
