using Durandal.Common.Logger;
using Durandal.Common.Time;
using Durandal.Common.Utils;
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
using System.Threading.Tasks;

namespace ScriptureGraph.App
{
    internal class AppCore
    {
        private ILogger _coreLogger;
        private KnowledgeGraph? _searchIndexGraph;
        private EntityNameIndex? _searchNameIndex;

        public ILogger CoreLogger => _coreLogger;

        public AppCore()
        {
            _coreLogger = new TraceLogger("GraphApp");
        }

        public async Task LoadSearchIndex(Stream searchIndexStream, Stream nameIndexStream)
        {
            await Task.Yield();
            _searchIndexGraph = KnowledgeGraph.Load(searchIndexStream);
            _searchNameIndex = EntityNameIndex.Deserialize(nameIndexStream);
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
