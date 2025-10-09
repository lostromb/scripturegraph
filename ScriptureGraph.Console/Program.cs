using Durandal.Common.File;
using Durandal.Common.IO;
using Durandal.Common.Logger;
using Durandal.Common.MathExt;
using Durandal.Common.NLP.Language;
using Durandal.Common.Time;
using Durandal.Common.Utils;
using Durandal.Common.Utils.NativePlatform;
using Durandal.Extensions.Compression.Brotli;
using Durandal.Extensions.Compression.Crc;
using ScriptureGraph.Core;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Training;
using ScriptureGraph.Core.Training.Extractors;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;

namespace ScriptureGraph.Console
{
    internal static class Program
    {
        private static IFileSystem _runtimeFileSystem = NullFileSystem.Singleton;
        private static IFileSystem _webCacheFileSystem = NullFileSystem.Singleton;
        private static IFileSystem _documentCacheFileSystem = NullFileSystem.Singleton;
        private static IFileSystem _epubFileSystem = NullFileSystem.Singleton;

        public static async Task Main(string[] args)
        {
#if DEBUG
            ILogger logger = new ConsoleLogger("Main", LogLevel.All);
#else
            ILogger logger = new ConsoleLogger("Main");
#endif
            NativePlatformUtils.SetGlobalResolver(new NativeLibraryResolverImpl());
            AssemblyReflector.ApplyAccelerators(typeof(CRC32CAccelerator).Assembly, logger);

            string rootDirectory = @"D:\Code\scripturegraph";
            _runtimeFileSystem = new RealFileSystem(logger.Clone("RuntimeFS"), rootDirectory + @"\runtime");
            _webCacheFileSystem = new RealFileSystem(logger.Clone("WebCacheFS"), rootDirectory + @"\runtime\cache");
            _documentCacheFileSystem = new RealFileSystem(logger.Clone("DocumentFS"), rootDirectory + @"\runtime\documents");
            _epubFileSystem = new RealFileSystem(logger.Clone("EpubFS"), rootDirectory + @"\Books");

            // Convert graphs
            //TrainingKnowledgeGraph graph;
            //using (Stream graphIn = new FileStream(@"D:\Code\scripturegraph\runtime\all.graph", FileMode.Open, FileAccess.Read))
            //{
            //    graph = TrainingKnowledgeGraph.LoadLegacyFormat(graphIn);
            //}

            //using (Stream graphIn = new FileStream(@"D:\Code\scripturegraph\runtime\scriptures.graph", FileMode.Open, FileAccess.Read))
            //using (BrotliDecompressorStream brotliStream = new BrotliDecompressorStream(graphIn))
            //{
            //    graph = TrainingKnowledgeGraph.Load(brotliStream);
            //}

            //using (NativeMemoryHeap graphHeap = new NativeMemoryHeap())
            //using (Stream graphIn = new FileStream(@"D:\Code\scripturegraph\runtime\searchindex.graph.br", FileMode.Open, FileAccess.Read))
            //using (BrotliDecompressorStream brotliStream = new BrotliDecompressorStream(graphIn))
            //{
            //    Stopwatch timer = Stopwatch.StartNew();
            //    UnsafeReadOnlyKnowledgeGraph graph = await UnsafeReadOnlyKnowledgeGraph.Load(brotliStream, graphHeap);
            //    //TrainingKnowledgeGraph graph = TrainingKnowledgeGraph.Load(brotliStream);
            //    timer.Stop();
            //    logger.Log("Load time was " + timer.ElapsedMillisecondsPrecise());

            //    RunSearchQuery("hinckley", graph, logger);
            //}

            // compress all documents
            //DirectoryInfo docRoot = new DirectoryInfo(@"C:\Code\scripturegraph\ScriptureGraph.App\bin\Debug\net8.0-windows\content\documents");
            //foreach (var file in docRoot.GetFiles("*", SearchOption.AllDirectories))
            //{
            //    if (string.Equals(".br", file.Extension, StringComparison.OrdinalIgnoreCase))
            //    {
            //        continue;
            //    }

            //    System.Console.WriteLine("Compressing " + file.FullName);
            //    using (Stream fileIn = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            //    using (Stream compressedFileOut = new FileStream(file.FullName +".br", FileMode.Create, FileAccess.Write))
            //    using (BrotliStream brotliStream = new BrotliStream(compressedFileOut, CompressionLevel.SmallestSize))
            //    {
            //        fileIn.CopyToPooled(brotliStream);
            //    }

            //    file.Delete();
            //}

            // Read all documents
            //DirectoryInfo documentRoot = new DirectoryInfo(@"C:\Code\scripturegraph\ScriptureGraph.App\bin\Debug\net8.0-windows\content\documents");
            //foreach (FileInfo documentFileName in documentRoot.EnumerateFiles("*", SearchOption.AllDirectories))
            //{
            //    using (Stream fileIn = new FileStream(documentFileName.FullName, FileMode.Open, FileAccess.Read))
            //    using (BrotliDecompressorStream brotli = new BrotliDecompressorStream(fileIn))
            //    {
            //        GospelDocument document = GospelDocument.ParsePolymorphic(brotli);
            //    }
            //}

            // Decompress one page in the cache
            //VirtualPath inputFile = new VirtualPath("https___www.churchofjesuschrist.org_study_scriptures_bofm_1-ne_3_lang=eng.html.br");
            //VirtualPath outputFile = new VirtualPath("https___www.churchofjesuschrist.org_study_scriptures_bofm_1-ne_3_lang=eng.html");
            //using (Stream fileIn = webCacheFileSystem.OpenStream(inputFile, FileOpenMode.Open, FileAccessMode.Read))
            //using (Stream fileOut = webCacheFileSystem.OpenStream(outputFile, FileOpenMode.CreateNew, FileAccessMode.Write))
            //using (BrotliDecompressorStream brotli = new BrotliDecompressorStream(fileIn))
            //{
            //    brotli.CopyToPooled(fileOut);
            //}

            //BookExtractorATGQ.ExtractDocuments(fileSystem, new VirtualPath(@"Answers to Gospel Questions, Vo - Joseph Fielding Smith.epub"), logger).Count();

            //await Test(logger);
            //await ParseDocuments(logger);
            //await BuildAndTestSearchIndex(logger);
            await BuildAndTestUniversalGraph(logger);

            //Uri scriptureUrl = new Uri("https://www.churchofjesuschrist.org/study/scriptures/pgp/abr/1?lang=eng");
            //Uri scriptureUrl = new Uri("https://www.churchofjesuschrist.org/study/scriptures/nt/john/1?lang=eng");
            //Uri scriptureUrl = new Uri("https://www.churchofjesuschrist.org/study/scriptures/dc-testament/od/1?lang=eng");
            //string webPage = new WebClient().DownloadString(scriptureUrl);
            //ScripturePageFeatureExtractorNew.ParseDocument(webPage, scriptureUrl, logger);
        }

        private static async Task Test(ILogger logger)
        {
            TrainingKnowledgeGraph graph;
            string inModelFileName = @"C:\Program Files\ScriptureGraph\content\all.graph.br";

            if (File.Exists(inModelFileName))
            {
                logger.Log("Loading model");
                using (FileStream testGraphIn = new FileStream(inModelFileName, FileMode.Open, FileAccess.Read))
                using (Stream brotli = new BrotliDecompressorStream(testGraphIn))
                {
                    graph = TrainingKnowledgeGraph.Load(brotli);
                }

                StatisticalSet stats = new StatisticalSet();
                int longListCount = 3;
                int shortListCount = 5;
                var enumerator = graph.GetUnsafeEnumerator();
                while (enumerator.MoveNext())
                {
                    int edgeCount = enumerator.Current.Value.Edges.NumEdges;
                    stats.Add(edgeCount);

                    if (longListCount > 0 && edgeCount > 140)
                    {
                        logger.Log("   ");
                        logger.Log($"Edges from the node \"{enumerator.Current.Key.ToString()}\"");
                        var edgeEnum = enumerator.Current.Value.Edges.GetEnumerator();
                        int edge = 1;
                        while (edgeEnum.MoveNext())
                        {
                            logger.Log($"{edge++} {edgeEnum.Current().Mass:F3} -> {edgeEnum.Current().Target.ToString()}");
                        }

                        longListCount--;
                    }

                    if (shortListCount > 0 && edgeCount > 4 && edgeCount < 10)
                    {
                        logger.Log("   ");
                        logger.Log($"Edges from the node \"{enumerator.Current.Key.ToString()}\"");
                        var edgeEnum = enumerator.Current.Value.Edges.GetEnumerator();
                        while (edgeEnum.MoveNext())
                        {
                            logger.Log($"{edgeEnum.Current().Mass:F3} -> {edgeEnum.Current().Target.ToString()}");
                        }

                        shortListCount--;
                    }
                }

                for (int percentile = 1; percentile <= 99; percentile++)
                {
                    logger.Log($"Percentile {percentile} NumEdges {stats.Percentile((double)percentile / 100.0)}");
                }

                logger.Log($"Num nodes: {stats.SampleCount}");

                //WebPageCache pageCache = new WebPageCache(webCacheFileSystem);
                //await CommonTasks.BuildUniversalGraph(logger, graph, pageCache);
                //using (FileStream testGraphOut = new FileStream(outModelFileName, FileMode.Create, FileAccess.Write))
                //{
                //    graph.Save(testGraphOut);
                //}
            }

            //IFileSystem documentFileSystem = new InMemoryFileSystem();
            //WebPageCache pageCache = new WebPageCache(webCacheFileSystem);
            //await CommonTasks.ParseDocuments(logger, pageCache, documentFileSystem);
        }

        private static async Task BuildAndTestSearchIndex(ILogger logger)
        {
            TrainingKnowledgeGraph entitySearchGraph;

            VirtualPath modelFileName = new VirtualPath("searchindex.graph.br");
            VirtualPath entityMapFileName = new VirtualPath("entitynames_eng.map");

            if (_runtimeFileSystem.Exists(modelFileName))
            {
                using (Stream searchGraphIn = _runtimeFileSystem.OpenStream(modelFileName, FileOpenMode.Open, FileAccessMode.Read))
                using (BrotliDecompressorStream brotliStream = new BrotliDecompressorStream(searchGraphIn))
                {
                    entitySearchGraph = TrainingKnowledgeGraph.Load(brotliStream);
                }
            }
            else
            {
                WebPageCache pageCache = new WebPageCache(_webCacheFileSystem);
                Tuple<TrainingKnowledgeGraph, EntityNameIndex> returnVal = await CommonTasks.BuildSearchIndex(logger, pageCache, _epubFileSystem);
                entitySearchGraph = returnVal.Item1;

                using (Stream searchGraphOut = _runtimeFileSystem.OpenStream(modelFileName, FileOpenMode.Create, FileAccessMode.Write))
                using (BrotliStream brotliStream = new BrotliStream(searchGraphOut, CompressionLevel.SmallestSize))
                {
                    entitySearchGraph.Save(brotliStream);
                }

                using (Stream nameMappingOut = _runtimeFileSystem.OpenStream(entityMapFileName, FileOpenMode.Create, FileAccessMode.Write))
                {
                    returnVal.Item2.Serialize(nameMappingOut);
                }
            }

            logger.Log("Ready to query");
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

        private static void RunSearchQuery(string queryString, IKnowledgeGraph graph, ILogger logger)
        {
            logger.Log("Querying " + queryString);

            Stopwatch timer = Stopwatch.StartNew();
            KnowledgeGraphQuery query = new KnowledgeGraphQuery();
            foreach (var feature in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(queryString))
            {
                query.AddRootNode(feature, 0);
            }

            var results = graph.Query(query, logger.Clone("Query"));
            timer.Stop();
            logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "Search time was {0:F2} ms", timer.ElapsedMillisecondsPrecise());

            float highestResultScore = 0;
            int linesRemaining = 10;
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

                if (linesRemaining-- <= 0)
                {
                    break;
                }

                logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:F3} : {1}", result.Value, result.Key.ToString());
            }
        }

        private static async Task BuildAndTestUniversalGraph(ILogger logger)
        {
            TrainingKnowledgeGraph graph;
            VirtualPath modelFileIn = new VirtualPath("all.graph");

            if (_runtimeFileSystem.Exists(modelFileIn))
            {
                logger.Log("Loading model");
                using (Stream testGraphIn = _runtimeFileSystem.OpenStream(modelFileIn, FileOpenMode.Open, FileAccessMode.Read))
                using (BrotliDecompressorStream brotliStream = new BrotliDecompressorStream(testGraphIn))
                {
                    graph = TrainingKnowledgeGraph.Load(brotliStream);
                }
            }
            else
            {
                WebPageCache pageCache = new WebPageCache(_webCacheFileSystem);
                graph = new TrainingKnowledgeGraph();
                await CommonTasks.BuildUniversalGraph(logger, graph, pageCache);
                using (Stream testGraphOut = _runtimeFileSystem.OpenStream(modelFileIn, FileOpenMode.Create, FileAccessMode.Write))
                using (BrotliStream brotliStream = new BrotliStream(testGraphOut, CompressionLevel.SmallestSize))
                {
                    graph.Save(brotliStream);
                }
            }

            int dispLines;
            //logger.Log("Edge dump");
            //dispLines = 100;
            //KnowledgeGraphNode node;
            //if (graph.TryGet(FeatureToNodeMapping.NGram("exceedingly", "great", "joy", LanguageCode.ENGLISH), out node))
            //{
            //    var edgeEnumerator = node.Edges.GetEnumerator();
            //    while (edgeEnumerator.MoveNext())
            //    {
            //        if (dispLines-- <= 0)
            //        {
            //            break;
            //        }

            //        KnowledgeGraphEdge edge = edgeEnumerator.Current();
            //        logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:F3} : {1}", edge.Mass, edge.Target.ToString());
            //    }
            //}

            logger.Log("Ready to query");
            KnowledgeGraphQuery query = new KnowledgeGraphQuery();

            //foreach (var feature in EnglishWordFeatureExtractor.ExtractNGrams("pure intelligence"))
            //{
            //    query.AddRootNode(feature, 0);
            //}

            //foreach (var feature in EnglishWordFeatureExtractor.ExtractNGrams("plan of redemption"))
            //{
            //    query.AddRootNode(feature, 0);
            //}

            query.AddRootNode(FeatureToNodeMapping.ConferenceSpeaker("neal a maxwell"), 0);

            foreach (var feature in EnglishWordFeatureExtractor.ExtractNGrams("intelligence"))
            {
                query.AddRootNode(feature, 1);
            }

            //query.AddRootNode(FeatureToNodeMapping.ScriptureBook("bofm", "jacob"), 2);
            //query.AddRootNode(FeatureToNodeMapping.ScriptureBook("ot", "isa"), 2);

            Stopwatch timer = Stopwatch.StartNew();
            var results = graph.Query(query, logger.Clone("Query"));
            timer.Stop();
            logger.Log(string.Format("Query finished in {0} ms", timer.ElapsedMillisecondsPrecise()));

            dispLines = 50;
            foreach (var result in results)
            {
                if (dispLines-- <= 0)
                {
                    break;
                }

                logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:F3} : {1}", result.Value, result.Key.ToString());
            }


            while (true)
            {
                string? queryString = System.Console.ReadLine();
                if (string.IsNullOrEmpty(queryString))
                {
                    return;
                }

                RunGraphQuery(queryString, graph, logger);
            }
        }

        private static void RunGraphQuery(string queryString, IKnowledgeGraph graph, ILogger logger)
        {
            logger.Log("Querying " + queryString);

            Stopwatch timer = Stopwatch.StartNew();
            KnowledgeGraphQuery query = new KnowledgeGraphQuery();

            ScriptureReference? parsedRef = ScriptureMetadataEnglish.TryParseScriptureReference(queryString);
            if (parsedRef != null)
            {
                logger.Log("Parsed query as " + parsedRef);
                if (!parsedRef.Chapter.HasValue)
                {
                    query.AddRootNode(FeatureToNodeMapping.ScriptureBook(parsedRef.Book), 0);
                }
                else if (!parsedRef.Verse.HasValue)
                {
                    query.AddRootNode(FeatureToNodeMapping.ScriptureChapter(parsedRef.Book, parsedRef.Chapter.Value), 0);
                }
                else
                {
                    query.AddRootNode(FeatureToNodeMapping.ScriptureVerse(parsedRef.Book, parsedRef.Chapter.Value, parsedRef.Verse.Value), 0);
                }
            }
            else
            {
                foreach (var feature in EnglishWordFeatureExtractor.ExtractNGrams(queryString))
                {
                    query.AddRootNode(feature, 0);
                }
            }

            var results = graph.Query(query, logger.Clone("Query"));
            timer.Stop();
            logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "Search time was {0:F2} ms", timer.ElapsedMillisecondsPrecise());

            float highestResultScore = 0;
            int linesRemaining = 30;
            foreach (var result in results)
            {
                if (result.Key.Type == KnowledgeGraphNodeType.NGram ||
                    result.Key.Type == KnowledgeGraphNodeType.CharNGram ||
                    result.Key.Type == KnowledgeGraphNodeType.Word ||
                    //result.Key.Type == KnowledgeGraphNodeType.GuideToScripturesTopic ||
                    //result.Key.Type == KnowledgeGraphNodeType.TopicalGuideKeyword ||
                    //result.Key.Type == KnowledgeGraphNodeType.TripleIndexTopic ||
                    result.Key.Type == KnowledgeGraphNodeType.ConferenceSpeaker)
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

                if (linesRemaining-- <= 0)
                {
                    break;
                }

                logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:F5} : {1}", result.Value, result.Key.ToString());
            }
        }

        private static async Task ParseDocuments(ILogger logger)
        {
            
            WebPageCache pageCache = new WebPageCache(_webCacheFileSystem);
            await CommonTasks.ParseDocuments(logger, pageCache, _documentCacheFileSystem, _epubFileSystem);
        }
    }
}
