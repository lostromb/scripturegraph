using Durandal.Common.File;
using Durandal.Common.Logger;
using Durandal.Common.Time;
using Durandal.Common.Utils;
using Durandal.Extensions.Compression.Brotli;
using Org.BouncyCastle.Crypto;
using ScriptureGraph.App.Schemas;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Training;
using ScriptureGraph.Core.Training.Extractors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScriptureGraph.App
{
    internal class AppCore
    {
        private readonly ILogger _coreLogger;
        private readonly IFileSystem _fileSystem;
        private Dictionary<KnowledgeGraphNodeId, VirtualPath> _documentLibrary;
        private IKnowledgeGraph? _smallSearchIndex;
        private EntityNameIndex? _entityNameLookup;
        private IKnowledgeGraph? _largeSearchIndex;
        private NativeMemoryHeap _nativeHeap;

        public ILogger CoreLogger => _coreLogger;

        public AppCore()
        {
            _coreLogger = new TraceLogger("GraphApp");
            _fileSystem = new RealFileSystem(_coreLogger.Clone("FileSystem"), @"D:\Code\scripturegraph\runtime");
            _documentLibrary = new Dictionary<KnowledgeGraphNodeId, VirtualPath>();
            _nativeHeap = new NativeMemoryHeap();
        }

        public async Task LoadSearchIndexes()
        {
            await Task.Yield();

            Stopwatch timer = Stopwatch.StartNew();
            VirtualPath smallGraphFileName = new VirtualPath("searchindex.graph");
            VirtualPath nameLookupFileName = new VirtualPath("entitynames_eng.map");
            VirtualPath largeGraphFileName = new VirtualPath("all.graph");

            if (!(await _fileSystem.ExistsAsync(smallGraphFileName)))
            {
                throw new Exception("Can't find small search graph file");
            }

            if (!(await _fileSystem.ExistsAsync(nameLookupFileName)))
            {
                throw new Exception("Can't find name index file");
            }

            if (!(await _fileSystem.ExistsAsync(largeGraphFileName)))
            {
                throw new Exception("Can't find main search graph file");
            }

            using (Stream searchGraphIn = await _fileSystem.OpenStreamAsync(smallGraphFileName, FileOpenMode.Open, FileAccessMode.Read))
            using (BrotliDecompressorStream brotliStream = new BrotliDecompressorStream(searchGraphIn))
            {
                _coreLogger.Log("Loading small search index");
                _smallSearchIndex = await UnsafeReadOnlyKnowledgeGraph.Load(brotliStream, _nativeHeap);
            }

            using (Stream searchIndexIn = await _fileSystem.OpenStreamAsync(nameLookupFileName, FileOpenMode.Open, FileAccessMode.Read))
            {
                _coreLogger.Log("Loading name lookup index");
                _entityNameLookup = EntityNameIndex.Deserialize(searchIndexIn);
            }

            using (Stream searchGraphIn = await _fileSystem.OpenStreamAsync(largeGraphFileName, FileOpenMode.Open, FileAccessMode.Read))
            using (BrotliDecompressorStream brotliStream = new BrotliDecompressorStream(searchGraphIn))
            {
                _coreLogger.Log("Loading large search index");
                _largeSearchIndex = await UnsafeReadOnlyKnowledgeGraph.Load(brotliStream, _nativeHeap);
            }

            timer.Stop();
            _coreLogger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "Indexes loaded in {0} ms", timer.ElapsedMillisecondsPrecise());
        }

        public bool DoesDocumentExist(KnowledgeGraphNodeId entityId)
        {
            return _documentLibrary.ContainsKey(MapEntityIdToDocument(entityId));
        }

        private KnowledgeGraphNodeId MapEntityIdToDocument(KnowledgeGraphNodeId entityId)
        {
            if (entityId.Type == KnowledgeGraphNodeType.ScriptureVerse)
            {
                // Map individual verses to chapters
                return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.ScriptureChapter, entityId.Name.Substring(0, entityId.Name.LastIndexOf('|')));
            }
            else if (entityId.Type == KnowledgeGraphNodeType.ScriptureBook)
            {
                // Do the same with books -> chapter 1
                return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.ScriptureChapter, entityId.Name + "|1");
            }
            else if (entityId.Type == KnowledgeGraphNodeType.BibleDictionaryParagraph)
            {
                // Map BD paragraph -> topic
                return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.BibleDictionaryTopic, entityId.Name.Substring(0, entityId.Name.LastIndexOf('|')));
            }
            else if (entityId.Type == KnowledgeGraphNodeType.ConferenceTalkParagraph)
            {
                // Map GC talk paragraph -> talk
                return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.ConferenceTalk, entityId.Name.Substring(0, entityId.Name.LastIndexOf('|')));
            }

            return entityId;
        }

        public async Task<GospelDocument> LoadDocument(KnowledgeGraphNodeId entityId)
        {
            KnowledgeGraphNodeId actualEntityId = MapEntityIdToDocument(entityId);
            if (!DoesDocumentExist(actualEntityId))
            {
                throw new FileNotFoundException("Could not load document for entity " + entityId);
            }

            VirtualPath filePath = _documentLibrary[actualEntityId];
            using (Stream docFileIn = await _fileSystem.OpenStreamAsync(filePath, FileOpenMode.Open, FileAccessMode.Read))
            {
                return GospelDocument.ParsePolymorphic(docFileIn);
            }
        }

        public async Task LoadDocumentLibrary()
        {
            await Task.Yield();

            VirtualPath indexFile = new VirtualPath("documents_index.json");
            VirtualPath documentRoot = new VirtualPath("documents");

            if (!(await _fileSystem.ExistsAsync(documentRoot)))
            {
                _coreLogger.Log("Could not find any structured documents", LogLevel.Err);
                return;
            }

            if (await _fileSystem.ExistsAsync(indexFile))
            {
                // LoadLegacyFormat cached file
                _coreLogger.Log("Loading cached document index");
                using (Stream cacheIn = await _fileSystem.OpenStreamAsync(indexFile, FileOpenMode.Open, FileAccessMode.Read))
                {
                    Dictionary<string, string> plainDict = JsonSerializer.Deserialize<Dictionary<string, string>>(cacheIn)!;
                    Dictionary<KnowledgeGraphNodeId, VirtualPath> documentLibrary = new Dictionary<KnowledgeGraphNodeId, VirtualPath>(plainDict.Select((s) =>
                    {
                        return new KeyValuePair<KnowledgeGraphNodeId, VirtualPath>(KnowledgeGraphNodeId.Deserialize(s.Key), new VirtualPath(s.Value));
                    }));

                    _documentLibrary = documentLibrary;
                    _coreLogger.Log("Cached document index loaded");
                }
            }
            else
            {
                _coreLogger.Log("Building a new document index. THIS IS SLOW!!!");
                Dictionary<KnowledgeGraphNodeId, VirtualPath> documentLibrary = new Dictionary<KnowledgeGraphNodeId, VirtualPath>();
                await CrawlDocumentsRecursive(documentRoot, documentLibrary);
                _documentLibrary = documentLibrary;

                Dictionary<string, string> ids = new Dictionary<string, string>(documentLibrary.Select((s) =>
                {
                    return new KeyValuePair<string, string>(s.Key.Serialize(), s.Value.FullName);
                }));

                using (Stream cacheOut = await _fileSystem.OpenStreamAsync(indexFile, FileOpenMode.Create, FileAccessMode.Write))
                using (Utf8JsonWriter writer = new Utf8JsonWriter(cacheOut))
                {
                    JsonSerializer.Serialize(writer, ids);
                }

                _coreLogger.Log("Cached document index created");
            }
        }

        private async Task CrawlDocumentsRecursive(VirtualPath root, Dictionary<KnowledgeGraphNodeId, VirtualPath> documentLibrary)
        {
            foreach (VirtualPath file in await _fileSystem.ListFilesAsync(root))
            {
                _coreLogger.Log("Indexing document " + file);
                using (Stream docFileIn = await _fileSystem.OpenStreamAsync(file, FileOpenMode.Open, FileAccessMode.Read))
                {
                    GospelDocumentMeta metadata = GospelDocumentMeta.ParseHeader(docFileIn);
                    documentLibrary[metadata.DocumentEntityId] = file;
                }
            }

            foreach (VirtualPath directory in await _fileSystem.ListDirectoriesAsync(root))
            {
                await CrawlDocumentsRecursive(directory, documentLibrary);
            }
        }

        public SlowSearchQueryResult RunSlowSearchQueryFake(SlowSearchQuery query)
        {
            _coreLogger.Log("Fake querying graph");
            return new SlowSearchQueryResult()
            {
                EntityIds = new List<KnowledgeGraphNodeId>()
                {
                    FeatureToNodeMapping.ScriptureVerse("bofm", "ether", 12, 27),
                    FeatureToNodeMapping.BibleDictionaryTopic("bishop"),
                    FeatureToNodeMapping.ConferenceTalkParagraph(2023, ConferencePhase.October, "26choi", 4),
                    FeatureToNodeMapping.BibleDictionaryParagraph("bible", 8),
                    FeatureToNodeMapping.ConferenceTalk(2021, ConferencePhase.April, "12uchtdorf"),
                },
                ActivatedWords = new Dictionary<KnowledgeGraphNodeId, float>()
            };
        }

        public SlowSearchQueryResult RunSlowSearchQuery(SlowSearchQuery query)
        {
            if (_largeSearchIndex == null)
            {
                throw new InvalidOperationException("Search index is not loaded");
            }

            _coreLogger.Log("Querying big graph");

            Stopwatch timer = Stopwatch.StartNew();
            KnowledgeGraphQuery internalQuery = new KnowledgeGraphQuery();
            int scopeId = 0;
            foreach (var queryScope in query.SearchScopes)
            {
                foreach (KnowledgeGraphNodeId nodeInScope in queryScope)
                {
                    internalQuery.AddRootNode(nodeInScope, scopeId);
                }

                scopeId++;
            }

            var results = _largeSearchIndex.Query(internalQuery, _coreLogger.Clone("Query"));
            timer.Stop();
            _coreLogger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "Search time was {0:F2} ms", timer.ElapsedMillisecondsPrecise());

            SlowSearchQueryResult returnVal = new SlowSearchQueryResult()
            {
                EntityIds = new List<KnowledgeGraphNodeId>(),
                ActivatedWords = new Dictionary<KnowledgeGraphNodeId, float>()
            };

            float highestResultScore = 0;
            int maxResults = query.MaxResults;
            foreach (var result in results)
            {
                if (result.Key.Type == KnowledgeGraphNodeType.Word)
                {
                    returnVal.ActivatedWords.Add(result.Key, result.Value);
                    continue;
                }

                if (!(result.Key.Type == KnowledgeGraphNodeType.ScriptureVerse ||
                    result.Key.Type == KnowledgeGraphNodeType.ConferenceTalk ||
                    result.Key.Type == KnowledgeGraphNodeType.ConferenceTalkParagraph ||
                    result.Key.Type == KnowledgeGraphNodeType.BibleDictionaryTopic ||
                    result.Key.Type == KnowledgeGraphNodeType.BibleDictionaryParagraph))
                {
                    continue;
                }

                // TODO apply filters and stuff

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

                if (maxResults-- <= 0)
                {
                    break;
                }

                returnVal.EntityIds.Add(result.Key);
                _coreLogger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:F3} : {1}", result.Value, result.Key.ToString());
            }

            return returnVal;
        }

        public IEnumerable<FastSearchQueryResult> RunFastSearchQuery(string queryString, int maxResults = 10)
        {
            yield return new FastSearchQueryResult()
            {
                EntityIds = EnglishWordFeatureExtractor.ExtractNGrams(queryString).ToArray(),
                EntityType = SearchResultEntityType.KeywordPhrase,
                DisplayName = queryString
            };

            if (_smallSearchIndex == null || _entityNameLookup == null)
            {
                _coreLogger.Log("Search index is not loaded", LogLevel.Err);
                yield break;
            }

            // Check for scripture references
            ScriptureReference? parsedRef = ScriptureMetadata.TryParseScriptureReferenceEnglish(queryString);
            if (parsedRef != null)
            {
                _coreLogger.Log("Parsed scripture ref " + parsedRef);
                string formattedBookName = ScriptureMetadata.GetEnglishNameForBook(parsedRef.Book);
                if (parsedRef.Chapter.HasValue && parsedRef.Verse.HasValue)
                {
                    yield return new FastSearchQueryResult()
                    {
                        EntityIds = new KnowledgeGraphNodeId[] { FeatureToNodeMapping.ScriptureVerse(parsedRef.Canon, parsedRef.Book, parsedRef.Chapter.Value, parsedRef.Verse.Value) },
                        EntityType = SearchResultEntityType.ScriptureVerse,
                        DisplayName = $"{formattedBookName} {parsedRef.Chapter.Value}:{parsedRef.Verse.Value}"
                    };
                }
                else if (parsedRef.Chapter.HasValue && !parsedRef.Verse.HasValue)
                {
                    yield return new FastSearchQueryResult()
                    {
                        EntityIds = new KnowledgeGraphNodeId[] { FeatureToNodeMapping.ScriptureChapter(parsedRef.Canon, parsedRef.Book, parsedRef.Chapter.Value) },
                        EntityType = SearchResultEntityType.ScriptureChapter,
                        DisplayName = $"{formattedBookName} {parsedRef.Chapter.Value}"
                    };
                }
                else
                {
                    yield return new FastSearchQueryResult()
                    {
                        EntityIds = new KnowledgeGraphNodeId[] { FeatureToNodeMapping.ScriptureBook(parsedRef.Canon, parsedRef.Book) },
                        EntityType = SearchResultEntityType.ScriptureBook,
                        DisplayName = formattedBookName
                    };
                }

                yield break;
            }

            _coreLogger.Log("Querying graph " + queryString);

            Stopwatch timer = Stopwatch.StartNew();
            KnowledgeGraphQuery query = new KnowledgeGraphQuery();
            foreach (var feature in EnglishWordFeatureExtractor.ExtractCharLevelNGrams(queryString))
            {
                query.AddRootNode(feature, 0);
            }

            var results = _smallSearchIndex.Query(query, _coreLogger.Clone("Query"));
            timer.Stop();
            _coreLogger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "Search time was {0:F2} ms", timer.ElapsedMillisecondsPrecise());

            Dictionary<string, List<KnowledgeGraphNodeId>> sameNameMappings = new Dictionary<string, List<KnowledgeGraphNodeId>>();
            foreach (var result in results)
            {
                if (result.Key.Type == KnowledgeGraphNodeType.BibleDictionaryParagraph ||
                    result.Key.Type == KnowledgeGraphNodeType.BibleDictionaryTopic ||
                    result.Key.Type == KnowledgeGraphNodeType.GuideToScripturesTopic ||
                    result.Key.Type == KnowledgeGraphNodeType.TripleIndexTopic ||
                    result.Key.Type == KnowledgeGraphNodeType.TopicalGuideKeyword)
                {
                    string? prettyName;
                    if (_entityNameLookup.Mapping.TryGetValue(result.Key, out prettyName))
                    {
                        List<KnowledgeGraphNodeId>? entityList;
                        if (!sameNameMappings.TryGetValue(prettyName, out entityList))
                        {
                            entityList = new List<KnowledgeGraphNodeId>();
                            sameNameMappings[prettyName] = entityList;
                        }

                        entityList.Add(result.Key);
                    }
                }
            }

            HashSet<string> displayedStrings = new HashSet<string>();
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

                string? prettyName;
                if (!_entityNameLookup.Mapping.TryGetValue(result.Key, out prettyName))
                {
                    prettyName = "UNKNOWN_NAME";
                }
                else if (displayedStrings.Contains(prettyName))
                {
                    continue;
                }

                if (maxResults-- <= 0)
                {
                    break;
                }

                List<KnowledgeGraphNodeId>? nodeMappings;
                if (sameNameMappings.TryGetValue(prettyName, out nodeMappings))
                {
                    // It's a reference to one or more topics with the same name.
                    // Return all of the entities combined under a single search result, for simplicity.
                    // This is intended for things like "TG: Locust, BD: Locust, GS: Locust", where instead
                    // of displaying multiple options with the same name, we expose one "meta-option"
                    // that contains all of the entity references internally
                    yield return new FastSearchQueryResult()
                    {
                        EntityIds = nodeMappings.ToArray(),
                        DisplayName = prettyName,
                        EntityType = ConvertEntityTypeToSearchResponseType(result.Key.Type),
                    };

                    displayedStrings.Add(prettyName);
                }
                else
                {
                    yield return new FastSearchQueryResult()
                    {
                        EntityIds = new KnowledgeGraphNodeId[] { result.Key },
                        DisplayName = prettyName,
                        EntityType = ConvertEntityTypeToSearchResponseType(result.Key.Type),
                    };
                }
            }
        }

        private static SearchResultEntityType ConvertEntityTypeToSearchResponseType(KnowledgeGraphNodeType nodeType)
        {
            switch (nodeType)
            {
                case KnowledgeGraphNodeType.ScriptureBook:
                    return SearchResultEntityType.ScriptureBook;
                case KnowledgeGraphNodeType.ScriptureChapter:
                    return SearchResultEntityType.ScriptureChapter;
                case KnowledgeGraphNodeType.ScriptureVerse:
                    return SearchResultEntityType.ScriptureVerse;
                case KnowledgeGraphNodeType.ConferenceSpeaker:
                    return SearchResultEntityType.Person;
                case KnowledgeGraphNodeType.ConferenceTalk:
                case KnowledgeGraphNodeType.ConferenceTalkParagraph:
                    return SearchResultEntityType.ConferenceTalk;
                case KnowledgeGraphNodeType.BibleDictionaryTopic:
                case KnowledgeGraphNodeType.BibleDictionaryParagraph:
                case KnowledgeGraphNodeType.TopicalGuideKeyword:
                case KnowledgeGraphNodeType.TripleIndexTopic:
                case KnowledgeGraphNodeType.GuideToScripturesTopic:
                    return SearchResultEntityType.Topic;
                default:
                    return SearchResultEntityType.Unknown;
            }
        }
    }
}
