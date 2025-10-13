using Durandal.API;
using Durandal.Common.Logger;
using Durandal.Common.Net.Http;
using Durandal.Common.Time;
using Durandal.Common.Utils;
using System.Net;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training
{
    public class WebCrawler
    {
        private static readonly string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:80.0) Gecko/20100101 Firefox/80.0";
        private static readonly Regex LINK_EXTRACTOR = new Regex("href=([\\\"\\'])([^#]+?)(?:#.+?)?\\1", RegexOptions.IgnoreCase);
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly WebPageCache _pageCache;
        private readonly RateLimiter _rateLimiter;

        public WebCrawler(IHttpClientFactory httpClientFactory, WebPageCache pageCache)
        {
            _httpClientFactory = httpClientFactory;
            _pageCache = pageCache;
            _rateLimiter = new RateLimiter(1, 10);
        }

        public class CrawledPage
        {
            public CrawledPage(Uri url, string html)
            {
                Url = url;
                Html = html;
                Links = new List<Uri>();
            }

            public Uri Url;
            public string Html;
            public List<Uri> Links;
        }

        //public async Task DownloadFile(Uri uri, FileInfo target, ILogger logger, string referer = "")
        //{
        //    try
        //    {
        //        IHttpClient httpClient = _httpClientFactory.CreateHttpClient(uri, logger);
        //        HttpRequest request = HttpRequest.CreateOutgoing(uri.PathAndQuery);
        //        request.RequestHeaders["User-Agent"] = USER_AGENT;
        //        if (!string.IsNullOrEmpty(referer))
        //        {
        //            request.RequestHeaders["Referer"] = referer;
        //        }

        //        using (HttpResponse response = await httpClient.SendRequestAsync(request))
        //        {
        //            if (response.ResponseCode == 200)
        //            {
        //                using (FileStream writeStream = new FileStream(target.FullName, FileMode.Create, FileAccess.Write))
        //                {
        //                    using (Stream httpStream = response.ReadContentAsStream())
        //                    {
        //                        await httpStream.CopyToAsync(writeStream);
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                string responseString = await response.ReadContentAsStringAsync(CancellationToken.None, DefaultRealTimeProvider.Singleton);
        //                logger.Log("Failed to download " + uri.AbsoluteUri + ": " + response.ResponseCode + " " + responseString, LogLevel.Err);
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logger.Log(e, LogLevel.Err);
        //    }
        //}

        //public async Task<HttpResponse?> MakeRequest(Uri uri, ILogger logger, string referer = "")
        //{
        //    try
        //    {
        //        IHttpClient httpClient = _httpClientFactory.CreateHttpClient(uri, logger);
        //        using (HttpRequest request = HttpRequest.CreateOutgoing(uri.PathAndQuery))
        //        {
        //            request.RequestHeaders["User-Agent"] = USER_AGENT;
        //            if (!string.IsNullOrEmpty(referer))
        //            {
        //                request.RequestHeaders["Referer"] = referer;
        //            }

        //            return await httpClient.SendRequestAsync(request);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        logger.Log(e, LogLevel.Err);
        //        return null;
        //    }
        //}

        public async Task Crawl(
            Uri startUrl,
            Func<CrawledPage, ILogger, Task<bool>> pageAction,
            ILogger logger,
            ISet<Regex>? allowedUrlPatterns = null)
        {
            if (allowedUrlPatterns == null)
            {
                allowedUrlPatterns = new HashSet<Regex>();
            }

            ISet<string> crawledUrls = new HashSet<string>();
            Queue<Uri> urlQueue = new Queue<Uri>();
            urlQueue.Enqueue(startUrl);
            crawledUrls.Add(startUrl.AbsoluteUri);

            while (urlQueue.Count > 0)
            {
                CrawledPage thisPage = new CrawledPage(urlQueue.Dequeue(), string.Empty);
                logger.Log("Crawling " + thisPage.Url, LogLevel.Vrb);
                try
                {
                    bool crawlSucceeded = true;
                    string? cachedPage = await _pageCache.GetCachedWebpageIfExists(thisPage.Url);
                    if (cachedPage != null)
                    {
                        thisPage.Html = cachedPage;
                        ExtractUrls(thisPage.Links, thisPage.Url, thisPage.Html, logger);
                        crawlSucceeded = true;
                        logger.Log("Using cached page", LogLevel.Vrb);
                    }
                    else
                    {
                        await _rateLimiter.LimitAsync(DefaultRealTimeProvider.Singleton, CancellationToken.None);
                        IHttpClient httpClient = _httpClientFactory.CreateHttpClient(thisPage.Url, logger);
                        HttpRequest request = HttpRequest.CreateOutgoing(thisPage.Url.PathAndQuery);
                        request.RequestHeaders["User-Agent"] = USER_AGENT;
                        //request.RequestHeaders["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                        //request.RequestHeaders["Accept-Language"] = "en-US,en;q=0.5";
                        //request.RequestHeaders["Cache-Control"] = "max-age=0";
                        //request.RequestHeaders["Host"] = "www.webtoons.com";
                        //request.RequestHeaders["Upgrade-Insecure-Requests"] = "1";
                        using (HttpResponse response = await httpClient.SendRequestAsync(request))
                        {
                            if (response.ResponseCode < 300)
                            {
                                thisPage.Html = await response.ReadContentAsStringAsync(CancellationToken.None, DefaultRealTimeProvider.Singleton);
                                ExtractUrls(thisPage.Links, thisPage.Url, thisPage.Html, logger);
                                await _pageCache.StorePage(thisPage.Url, thisPage.Html);
                            }
                            else
                            {
                                crawlSucceeded = false;
                                string responseString = await response.ReadContentAsStringAsync(CancellationToken.None, DefaultRealTimeProvider.Singleton);
                                logger.Log("Failed to download " + thisPage.Url.AbsoluteUri + ": " + response.ResponseCode + " " + responseString, LogLevel.Err);
                            }
                        }
                    }

                    if (crawlSucceeded)
                    {
                        bool continueCrawling = await pageAction(thisPage, logger);
                        if (!continueCrawling)
                        {
                            logger.Log("Got signal to stop crawling");
                            urlQueue.Clear();
                        }
                        else
                        {
                            foreach (Uri url in FilterList(thisPage.Links, allowedUrlPatterns))
                            {
                                if (!crawledUrls.Contains(url.AbsoluteUri))
                                {
                                    urlQueue.Enqueue(url);
                                    crawledUrls.Add(url.AbsoluteUri);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Log(e, LogLevel.Err);
                }
            }
        }

        private static void ExtractUrls(List<Uri> returnVal, Uri baseUrl, string page, ILogger logger)
        {
            foreach (Match m in LINK_EXTRACTOR.Matches(page))
            {
                string x = WebUtility.HtmlDecode(m.Groups[2].Value);
                Uri linkUrl;

                try
                {
                    // Is it a relative uri? resolve it
                    if (x.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        x.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        linkUrl = new Uri(x);
                        returnVal.Add(linkUrl);
                    }
                    else
                    {
                        linkUrl = new Uri(baseUrl, x);
                        returnVal.Add(linkUrl);
                    }
                }
                catch (UriFormatException e)
                {
                    logger.Log(e, LogLevel.Err);
                    logger.Log($"Url was \"{x}\"", LogLevel.Err);
                }
            }
        }

        private static List<Uri> FilterList(List<Uri> list, ISet<Regex> allowedUrlPatterns)
        {
            List<Uri> filteredList = new List<Uri>();
            foreach (Uri linkUrl in list)
            {
                //Console.WriteLine ("Found link " + linkUrl);
                bool allowed = allowedUrlPatterns.Count == 0;
                foreach (Regex r in allowedUrlPatterns)
                {
                    if (r.Match(linkUrl.OriginalString).Success)
                    {
                        allowed = true;
                        break;
                    }
                }

                if (linkUrl.AbsolutePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    linkUrl.AbsolutePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    linkUrl.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                    linkUrl.AbsolutePath.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) ||
                    linkUrl.AbsolutePath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                    linkUrl.AbsolutePath.EndsWith(".avi", StringComparison.OrdinalIgnoreCase) ||
                    linkUrl.AbsolutePath.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ||
                    linkUrl.AbsolutePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                {
                    allowed = false;
                }

                if (allowed)
                {
                    //Console.WriteLine ("It's allowed");
                    filteredList.Add(linkUrl);
                }
            }

            return filteredList;
        }
    }
}