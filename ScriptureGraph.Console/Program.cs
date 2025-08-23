using Durandal.Common.File;
using Durandal.Common.Logger;
using Durandal.Common.Net.Http;
using Durandal.Common.Utils.NativePlatform;
using ScriptureGraph.Core.Training;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Console
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            ILogger logger = new ConsoleLogger("Main", LogLevel.All);
            NativePlatformUtils.SetGlobalResolver(new NativeLibraryResolverImpl());
            IFileSystem webCacheFileSystem = new RealFileSystem(logger.Clone("CacheFS"), @"C:\Code\scripturegraph\runtime\cache");
            WebPageCache pageCache = new WebPageCache(webCacheFileSystem);
            WebCrawler crawler = new WebCrawler(new PortableHttpClientFactory(), pageCache);

            HashSet<Regex> scriptureRegexes = new HashSet<Regex>();
            scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/.+?\\?lang=eng$"));
            scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/.+?/.+?\\?lang=eng$"));
            scriptureRegexes.Add(new Regex("^https://www.churchofjesuschrist.org/study/scriptures/.+?/.+?/\\d+\\?lang=eng$"));
            await crawler.Crawl(
                new Uri("https://www.churchofjesuschrist.org/study/scriptures/bofm?lang=eng"),
                AlwaysContinueAction,
                logger.Clone("WebCrawler"),
                scriptureRegexes);

            return;
        }

        private static Task<bool> AlwaysContinueAction(WebCrawler.CrawledPage page)
        {
            return Task.FromResult<bool>(true);
        }
    }
}
