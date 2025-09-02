using Durandal.Common.File;
using Durandal.Common.Instrumentation;
using Durandal.Common.Logger;
using Durandal.Common.Net.Http;
using Durandal.Common.Tasks;
using Durandal.Common.Time;
using Durandal.Common.Utils.NativePlatform;
using Org.BouncyCastle.Bcpg.Sig;
using ScriptureGraph.Core;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Training;
using ScriptureGraph.Core.Training.Extractors;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Console
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
#if DEBUG
            ILogger logger = new ConsoleLogger("Main", LogLevel.All);
#else
            ILogger logger = new ConsoleLogger("Main");
#endif
            NativePlatformUtils.SetGlobalResolver(new NativeLibraryResolverImpl());

            // Decompress one page in the cache
            //VirtualPath inputFile = new VirtualPath("https___www.churchofjesuschrist.org_study_scriptures_bofm_1-ne_3_lang=eng.html.br");
            //VirtualPath outputFile = new VirtualPath("https___www.churchofjesuschrist.org_study_scriptures_bofm_1-ne_3_lang=eng.html");
            //using (Stream fileIn = webCacheFileSystem.OpenStream(inputFile, FileOpenMode.Open, FileAccessMode.Read))
            //using (Stream fileOut = webCacheFileSystem.OpenStream(outputFile, FileOpenMode.CreateNew, FileAccessMode.Write))
            //using (BrotliDecompressorStream brotli = new BrotliDecompressorStream(fileIn))
            //{
            //    brotli.CopyToPooled(fileOut);
            //}

            await BuildAndTestSearchIndex(logger);
        }

        private static async Task BuildAndTestSearchIndex(ILogger logger)
        {
            KnowledgeGraph entitySearchGraph;

            string modelFileName = @"D:\Code\scripturegraph\runtime\searchindex.graph";

            if (File.Exists(modelFileName))
            {
                using (FileStream searchGraphIn = new FileStream(modelFileName, FileMode.Open, FileAccess.Read))
                {
                    entitySearchGraph = KnowledgeGraph.Load(searchGraphIn);
                }
            }
            else
            {
                IFileSystem webCacheFileSystem = new RealFileSystem(logger.Clone("CacheFS"), @"D:\Code\scripturegraph\runtime\cache");
                WebPageCache pageCache = new WebPageCache(webCacheFileSystem);
                entitySearchGraph = await CommonTasks.BuildSearchIndex(logger, pageCache);

                using (FileStream searchGraphOut = new FileStream(modelFileName, FileMode.Create, FileAccess.Write))
                {
                    entitySearchGraph.Save(searchGraphOut);
                }
            }

            RunSearchQuery("russell m nelson", entitySearchGraph, logger);
            RunSearchQuery("jeffrey", entitySearchGraph, logger);
            RunSearchQuery("fruit that remains", entitySearchGraph, logger);
            RunSearchQuery("outer space", entitySearchGraph, logger);
            RunSearchQuery("monson", entitySearchGraph, logger);

            while (true)
            {
                string? query = System.Console.ReadLine();
                if (string.IsNullOrEmpty(query))
                {
                    return;
                }

                RunSearchQuery(query, entitySearchGraph, logger);
            }
        }

        private static void RunSearchQuery(string queryString, KnowledgeGraph graph, ILogger logger)
        {
            logger.Log("Querying " + queryString);

            KnowledgeGraphQuery query = new KnowledgeGraphQuery();

            foreach (var feature in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(queryString))
            {
                query.AddRootNode(feature, 0);
            }

            Stopwatch timer = Stopwatch.StartNew();
            var results = graph.Query(query, logger.Clone("Query"));
            timer.Stop();

            float highestResultScore = 0;
            foreach (var result in results)
            {
                if (result.Key.Type == KnowledgeGraphNodeType.NGram ||
                    result.Key.Type == KnowledgeGraphNodeType.CharNGram ||
                    result.Key.Type == KnowledgeGraphNodeType.Word)
                {
                    continue;
                }

                if (result.Value > highestResultScore)
                {
                    // assumes highest scoring result is first
                    highestResultScore = result.Value;
                }
                else if (result.Value < highestResultScore * 0.25f)
                {
                    // too low of confidence
                    break;
                }

                logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:F3} : {1}", result.Value, result.Key.ToString());
            }
        }

        private static async Task BuildUniversalGraph(ILogger logger)
        {
            KnowledgeGraph graph;

            string modelFileName = @"D:\Code\scripturegraph\runtime\all.graph";

            if (File.Exists(modelFileName))
            {
                logger.Log("Loading model");
                using (FileStream testGraphIn = new FileStream(modelFileName, FileMode.Open, FileAccess.Read))
                {
                    graph = KnowledgeGraph.Load(testGraphIn);
                }
            }
            else
            {
                IFileSystem webCacheFileSystem = new RealFileSystem(logger.Clone("CacheFS"), @"D:\Code\scripturegraph\runtime\cache");
                WebPageCache pageCache = new WebPageCache(webCacheFileSystem);
                graph = await CommonTasks.BuildUniversalGraph(logger, pageCache);
                using (FileStream testGraphOut = new FileStream(modelFileName, FileMode.Create, FileAccess.Write))
                {
                    graph.Save(testGraphOut);
                }
            }

            int dispLines;
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

            logger.Log("Querying");
            KnowledgeGraphQuery query = new KnowledgeGraphQuery();

            foreach (var feature in EnglishWordFeatureExtractor.ExtractNGrams("pure intelligence"))
            {
                query.AddRootNode(feature, 0);
            }

            //foreach (var feature in EnglishWordFeatureExtractor.ExtractNGrams("plan of redemption"))
            //{
            //    query.AddRootNode(feature, 0);
            //}

            //foreach (var feature in EnglishWordFeatureExtractor.ExtractNGrams("quench"))
            //{
            //    query.AddRootNode(feature, 1);
            //}

            //query.AddRootNode(FeatureToNodeMapping.ScriptureBook("bofm", "jacob"), 2);
            //query.AddRootNode(FeatureToNodeMapping.ScriptureBook("ot", "isa"), 2);

            Stopwatch timer = Stopwatch.StartNew();
            var results = graph.Query(query, logger.Clone("Query"));
            timer.Stop();
            logger.Log(string.Format("Query finished in {0} ms", timer.ElapsedMillisecondsPrecise()));

            dispLines = 40;
            foreach (var result in results)
            {
                if (dispLines-- <= 0)
                {
                    break;
                }

                logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:F3} : {1}", result.Value, result.Key.ToString());
            }
        }

        private static async Task ParseDocuments(ILogger logger)
        {
            IFileSystem webCacheFileSystem = new RealFileSystem(logger.Clone("CacheFS"), @"D:\Code\scripturegraph\runtime\cache");
            IFileSystem documentCacheFileSystem = new RealFileSystem(logger.Clone("CacheFS"), @"D:\Code\scripturegraph\runtime\documents");
            WebPageCache pageCache = new WebPageCache(webCacheFileSystem);
            await CommonTasks.ParseDocuments(logger, pageCache, documentCacheFileSystem);
        }
    }
}
