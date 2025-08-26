using Durandal.Common.File;
using Durandal.Common.IO;
using Durandal.Common.Logger;
using Durandal.Common.Net.Http;
using Durandal.Common.Utils.NativePlatform;
using Durandal.Extensions.Compression.Brotli;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Training;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Console
{
    internal class Program
    {
        private static KnowledgeGraph graph;

        public static async Task Main(string[] args)
        {
            ILogger logger = new ConsoleLogger("Main", LogLevel.All);
            NativePlatformUtils.SetGlobalResolver(new NativeLibraryResolverImpl());
            IFileSystem webCacheFileSystem = new RealFileSystem(logger.Clone("CacheFS"), @"C:\Code\scripturegraph\runtime\cache");
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

            HashSet<Regex> scriptureRegexes = new HashSet<Regex>();
            //scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/.+?\\?lang=eng$"));
            //scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/.+?/.+?\\?lang=eng$"));
            //scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/.+?/.+?/\\d+\\?lang=eng$"));
            scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/bofm/1-ne/\\d+\\?lang=eng$"));
            await crawler.Crawl(
                //new Uri("https://www.churchofjesuschrist.org/study/scriptures/bofm?lang=eng"),
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/bofm/1-ne/1?lang=eng"),
                ParseScripturePageAction,
                logger.Clone("WebCrawler"),
                scriptureRegexes);

            return;
        }

        private static readonly Regex UrlPathParser = new Regex("\\/study\\/scriptures\\/(.+?)\\/(.+?)\\/(\\d+)");

        private static Task<bool> ParseScripturePageAction(WebCrawler.CrawledPage page, ILogger logger)
        {
            List<TrainingFeature> features = new List<TrainingFeature>();
            if (UrlPathParser.Match(page.Url.AbsolutePath).Success)
            {
                // It's a scripture page. Try and parse it
                ScripturePageFeatureExtractor.ExtractFeatures(page.Html, page.Url, logger, features);
            }

            foreach (var feature in features)
            {
                graph.Train(feature);
            }

            return Task.FromResult<bool>(true);
        }
    }
}
