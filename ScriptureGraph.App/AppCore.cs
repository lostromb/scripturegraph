using Durandal.Common.File;
using Durandal.Common.Logger;
using Durandal.Common.Time;
using Durandal.Common.Utils;
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
        private KnowledgeGraph? _searchIndexGraph;
        private EntityNameIndex? _searchNameIndex;

        public ILogger CoreLogger => _coreLogger;

        public AppCore()
        {
            _coreLogger = new TraceLogger("GraphApp");
            _fileSystem = new RealFileSystem(_coreLogger.Clone("FileSystem"), @"D:\Code\scripturegraph\runtime");
            _documentLibrary = new Dictionary<KnowledgeGraphNodeId, VirtualPath>();
        }

        public async Task LoadSearchIndex()
        {
            await Task.Yield();

            VirtualPath graphFileName = new VirtualPath("searchindex.graph");
            VirtualPath indexFileName = new VirtualPath("entitynames_eng.map");

            if (!(await _fileSystem.ExistsAsync(graphFileName)))
            {
                throw new Exception("Can't find search index file");
            }

            if (!(await _fileSystem.ExistsAsync(indexFileName)))
            {
                throw new Exception("Can't find name index file");
            }

            using (Stream searchGraphIn = await _fileSystem.OpenStreamAsync(graphFileName, FileOpenMode.Open, FileAccessMode.Read))
            using (Stream searchIndexIn = await _fileSystem.OpenStreamAsync(indexFileName, FileOpenMode.Open, FileAccessMode.Read))
            {
                _searchIndexGraph = KnowledgeGraph.Load(searchGraphIn);
                _searchNameIndex = EntityNameIndex.Deserialize(searchIndexIn);
            }
        }

        public bool DoesDocumentExist(KnowledgeGraphNodeId entityId)
        {
            return _documentLibrary.ContainsKey(MapEntityIdToDocument(entityId));
        }

        private KnowledgeGraphNodeId MapEntityIdToDocument(KnowledgeGraphNodeId entityId)
        {
            // Map individual verses to chapters
            if (entityId.Type == KnowledgeGraphNodeType.ScriptureVerse)
            {
                return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.ScriptureChapter, entityId.Name.Substring(0, entityId.Name.LastIndexOf('|')));
            }

            // Do the same with books -> chapter 1
            else if (entityId.Type == KnowledgeGraphNodeType.ScriptureBook)
            {
                return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.ScriptureChapter, entityId.Name + "|1");
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
                // Load cached file
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

        public IEnumerable<SearchQueryResult> RunSearchQuery(string queryString, int maxResults = 10)
        {
            yield return new SearchQueryResult()
            {
                EntityIds = Array.Empty<KnowledgeGraphNodeId>() ,
                EntityType = SearchResultEntityType.KeywordPhrase,
                DisplayName = queryString
            };

            if (_searchIndexGraph == null || _searchNameIndex == null)
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
                    yield return new SearchQueryResult()
                    {
                        EntityIds = new KnowledgeGraphNodeId[] { FeatureToNodeMapping.ScriptureVerse(parsedRef.Canon, parsedRef.Book, parsedRef.Chapter.Value, parsedRef.Verse.Value) },
                        EntityType = SearchResultEntityType.ScriptureVerse,
                        DisplayName = $"{formattedBookName} {parsedRef.Chapter.Value}:{parsedRef.Verse.Value}"
                    };
                }
                else if (parsedRef.Chapter.HasValue && !parsedRef.Verse.HasValue)
                {
                    yield return new SearchQueryResult()
                    {
                        EntityIds = new KnowledgeGraphNodeId[] { FeatureToNodeMapping.ScriptureChapter(parsedRef.Canon, parsedRef.Book, parsedRef.Chapter.Value) },
                        EntityType = SearchResultEntityType.ScriptureChapter,
                        DisplayName = $"{formattedBookName} {parsedRef.Chapter.Value}"
                    };
                }
                else
                {
                    yield return new SearchQueryResult()
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

            var results = _searchIndexGraph.Query(query, _coreLogger.Clone("Query"));
            timer.Stop();
            _coreLogger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, $"Search time was {0:F3} ms", timer.ElapsedMillisecondsPrecise());

            Dictionary<string, List<KnowledgeGraphNodeId>> sameNameMappings = new Dictionary<string, List<KnowledgeGraphNodeId>>();
            foreach (var result in results)
            {
                if (result.Key.Type == KnowledgeGraphNodeType.NGram ||
                    result.Key.Type == KnowledgeGraphNodeType.CharNGram ||
                    result.Key.Type == KnowledgeGraphNodeType.Word)
                {
                    continue;
                }

                string? prettyName;
                if (_searchNameIndex.Mapping.TryGetValue(result.Key, out prettyName))
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
                if (!_searchNameIndex.Mapping.TryGetValue(result.Key, out prettyName))
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
                    yield return new SearchQueryResult()
                    {
                        EntityIds = nodeMappings.ToArray(),
                        DisplayName = prettyName,
                        EntityType = ConvertEntityTypeToSearchResponseType(result.Key.Type),
                    };

                    displayedStrings.Add(prettyName);
                }
                else
                {
                    yield return new SearchQueryResult()
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
