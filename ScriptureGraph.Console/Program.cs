using Durandal.Common.File;
using Durandal.Common.IO;
using Durandal.Common.Logger;
using Durandal.Common.MathExt;
using Durandal.Common.Net.Http;
using Durandal.Common.NLP.Language;
using Durandal.Common.Time;
using Durandal.Common.Utils.NativePlatform;
using Durandal.Extensions.Compression.Brotli;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Training;
using ScriptureGraph.Core.Training.Extractors;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Console
{
    internal class Program
    {
        private static KnowledgeGraph graph;

        public static async Task Main(string[] args)
        {
#if DEBUG
            ILogger logger = new ConsoleLogger("Main", LogLevel.All);
#else
            ILogger logger = new ConsoleLogger("Main");
#endif
            NativePlatformUtils.SetGlobalResolver(new NativeLibraryResolverImpl());
            IFileSystem webCacheFileSystem = new RealFileSystem(logger.Clone("CacheFS"), @"D:\Code\scripturegraph\runtime\cache");
            WebPageCache pageCache = new WebPageCache(webCacheFileSystem);
            WebCrawler crawler = new WebCrawler(new PortableHttpClientFactory(), pageCache);

            // Decompress one page in the cache
            //VirtualPath inputFile = new VirtualPath("https___www.churchofjesuschrist.org_study_scriptures_bofm_1-ne_3_lang=eng.html.br");
            //VirtualPath outputFile = new VirtualPath("https___www.churchofjesuschrist.org_study_scriptures_bofm_1-ne_3_lang=eng.html");
            //using (Stream fileIn = webCacheFileSystem.OpenStream(inputFile, FileOpenMode.Open, FileAccessMode.Read))
            //using (Stream fileOut = webCacheFileSystem.OpenStream(outputFile, FileOpenMode.CreateNew, FileAccessMode.Write))
            //using (BrotliDecompressorStream brotli = new BrotliDecompressorStream(fileIn))
            //{
            //    brotli.CopyToPooled(fileOut);
            //}

            graph = new KnowledgeGraph();

            //for (float weight = -5; weight < 5; weight += 0.2f)
            //{
            //    System.Console.WriteLine(weight + " : " + FastMath.Sigmoid(weight));
            //}

            //graph.Train(new TrainingFeature(
            //    FeatureToNodeMapping.Entity("Logan Stromberg"),
            //    FeatureToNodeMapping.Word("logan", LanguageCode.ENGLISH),
            //    TrainingFeatureType.WordAssociation));

            //graph.Train(new TrainingFeature(
            //    FeatureToNodeMapping.Entity("Logan Stromberg"),
            //    FeatureToNodeMapping.Word("stromberg", LanguageCode.ENGLISH),
            //    TrainingFeatureType.WordAssociation));

            //graph.Train(new TrainingFeature(
            //    FeatureToNodeMapping.Entity("Logan Stromberg"),
            //    FeatureToNodeMapping.NGram("logan", "stromberg", LanguageCode.ENGLISH),
            //    TrainingFeatureType.NgramAssociation));

            //string modelFileName = @"D:\Code\scripturegraph\runtime\bom.graph";

            //if (File.Exists(modelFileName))
            //{
            //    logger.Log("Loading model");
            //    using (FileStream testGraphIn = new FileStream(modelFileName, FileMode.Open, FileAccess.Read))
            //    {
            //        graph = KnowledgeGraph.Load(testGraphIn);
            //    }
            //}
            //else
            {
                HashSet<Regex> scriptureRegexes = new HashSet<Regex>();
                scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/.+?\\?lang=eng$"));
                //scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/.+?/.+?\\?lang=eng$"));
                //scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/.+?/.+?/\\d+\\?lang=eng$"));
                //scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/bofm/.+?/\\d+\\?lang=eng$"));
                //scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/bofm/1-ne/\\d+\\?lang=eng$"));
                //scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/bofm/1-ne/1\\?lang=eng$"));
                await crawler.Crawl(
                    //new Uri("https://www.churchofjesuschrist.org/study/scriptures/bofm?lang=eng"),
                    //new Uri("https://www.churchofjesuschrist.org/study/scriptures/bofm/1-ne/1?lang=eng"),
                    new Uri("https://www.churchofjesuschrist.org/study/scriptures/tg/afraid?lang=eng"),
                    ParseScripturePageAction,
                    logger.Clone("WebCrawler"),
                    scriptureRegexes);

                //using (FileStream testGraphOut = new FileStream(modelFileName, FileMode.Create, FileAccess.Write))
                //{
                //    graph.Save(testGraphOut);
                //}
            }

            //int dispLines;
            //logger.Log("Edge dump");
            //dispLines = 100;
            //var edgeEnumerator = graph.Get(FeatureToNodeMapping.NGram("exceedingly", "great", "joy", LanguageCode.ENGLISH)).Edges.GetEnumerator();
            //while (edgeEnumerator.MoveNext())
            //{
            //    if (dispLines-- <= 0)
            //    {
            //        break;
            //    }

            //    KnowledgeGraphEdge edge = edgeEnumerator.Current();
            //    logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:F3} : {1}", edge.Mass, edge.Target.ToString());
            //}

            //logger.Log("Querying");
            //KnowledgeGraphQuery query = new KnowledgeGraphQuery();
            //foreach (var feature in EnglishWordFeatureExtractor.ExtractNGrams("plan of redemption"))
            //{
            //    query.AddRootNode(feature, 0);
            //}

            //foreach (var feature in EnglishWordFeatureExtractor.ExtractNGrams("quench"))
            //{
            //    query.AddRootNode(feature, 1);
            //}

            //query.AddRootNode(FeatureToNodeMapping.ScriptureBook("bofm", "jacob"), 2);

            //Stopwatch timer = Stopwatch.StartNew();
            //var results = graph.Query(query, logger.Clone("Query"));
            //timer.Stop();
            //logger.Log(string.Format("Query finished in {0} ms", timer.ElapsedMillisecondsPrecise()));

            //dispLines = 40;
            //foreach (var result in results)
            //{
            //    if (dispLines-- <= 0)
            //    {
            //        break;
            //    }

            //    logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:F3} : {1}", result.Value, result.Key.ToString());
            //}
        }

        private static readonly Regex ScriptureChapterUrlMatcher = new Regex("\\/study\\/scriptures\\/(?:bofm|ot|nt|dc-testament|pgp)\\/.+?\\/\\d+");
        private static readonly Regex ReferenceUrlMatcher = new Regex("\\/study\\/scriptures\\/(tg|bd|gs)\\/.+?(?:\\?|$)");

        private static Task<bool> ParseScripturePageAction(WebCrawler.CrawledPage page, ILogger logger)
        {
            List<TrainingFeature> features = new List<TrainingFeature>(50000);
            Match match = ScriptureChapterUrlMatcher.Match(page.Url.AbsolutePath);
            if (match.Success)
            {
                logger.Log($"Parsing scripture page {page.Url.AbsolutePath}");
                ScripturePageFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, features);
            }

            match = ReferenceUrlMatcher.Match(page.Url.AbsolutePath);
            if (match.Success)
            {
                if (string.Equals(match.Groups[1].Value, "tg", StringComparison.Ordinal))
                {
                    logger.Log($"Parsing TG page {page.Url.AbsolutePath}");
                    TopicalGuideFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, features);
                }
                else if (string.Equals(match.Groups[1].Value, "bd", StringComparison.Ordinal))
                {
                    logger.Log($"Parsing BD page {page.Url.AbsolutePath}");
                    TopicalGuideFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, features);
                }
                else if (string.Equals(match.Groups[1].Value, "gs", StringComparison.Ordinal))
                {
                    logger.Log($"Parsing GS page {page.Url.AbsolutePath}");
                    TopicalGuideFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, features);
                }
            }

            foreach (var feature in features)
            {
                graph.Train(feature);
            }

            return Task.FromResult<bool>(true);
        }
    }
}
