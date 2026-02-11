using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas.Serializers;
using System.Text.Json.Serialization;

namespace ScriptureGraph.Core.Schemas.Index
{
    internal class GospelDocumentIndex
    {
        // Flat serializable fields
        [JsonPropertyName("lang")]
        [JsonConverter(typeof(LanguageCodeSerializer))]
        public LanguageCode Language { get; set; } = LanguageCode.UNDETERMINED;

        [JsonPropertyName("scriptures")]
        public List<IndexScriptureVerse> Scriptures { get; set; } = new List<IndexScriptureVerse>();

        [JsonPropertyName("hymns")]
        public List<IndexHymn> Hymns { get; set; } = new List<IndexHymn>();

        [JsonPropertyName("bibledict")]
        public List<IndexBibleDictionaryTopic> BibleDictionary { get; set; } = new List<IndexBibleDictionaryTopic>();

        [JsonPropertyName("books")]
        public List<IndexBookChapter> BookChapters { get; set; } = new List<IndexBookChapter>();

        [JsonPropertyName("speeches")]
        public List<IndexByuSpeech> ByuSpeeches { get; set; } = new List<IndexByuSpeech>();

        [JsonPropertyName("conference")]
        public List<IndexConferenceTalk> ConferenceTalks { get; set; } = new List<IndexConferenceTalk>();

        [JsonPropertyName("discourses")]
        public List<IndexJODDiscourse> Discourses { get; set; } = new List<IndexJODDiscourse>();

        // Structured indexes into the data, not serialized but used for lookups

        [JsonIgnore]
        private Dictionary<KnowledgeGraphNodeId, IndexableEntity> Index_IdToEntity
            = new Dictionary<KnowledgeGraphNodeId, IndexableEntity>();

        [JsonIgnore]
        private Dictionary<string, Dictionary<string, Dictionary<int, Dictionary<string, IndexScriptureVerse>>>> Index_ScriptureCanonToBookToChapterToVerse
            = new Dictionary<string, Dictionary<string, Dictionary<int, Dictionary<string, IndexScriptureVerse>>>>();

        [JsonIgnore]
        private Dictionary<int, List<IndexJODDiscourse>> Index_JODVolumeToDiscourses
            = new Dictionary<int, List<IndexJODDiscourse>>();

        [JsonIgnore]
        private Dictionary<Conference, List<IndexConferenceTalk>> Index_ConferenceToTalks
            = new Dictionary<Conference, List<IndexConferenceTalk>>();

        [JsonIgnore]
        private Dictionary<string, List<IndexBookChapter>> Index_BookIdToChapters
            = new Dictionary<string, List<IndexBookChapter>>();

        [JsonIgnore]
        private Dictionary<int, IndexHymn> Index_HymnNumberToHymn
            = new Dictionary<int, IndexHymn>();

        public void RebuildIndex()
        {
            Index_IdToEntity.Clear();
            Index_JODVolumeToDiscourses.Clear();
            Index_ConferenceToTalks.Clear();
            Index_BookIdToChapters.Clear();
            Index_HymnNumberToHymn.Clear();
            Index_ScriptureCanonToBookToChapterToVerse.Clear();

            foreach (IndexScriptureVerse verse in Scriptures)
            {
                Index_IdToEntity[verse.EntityId] = verse;

                Dictionary<string, Dictionary<int, Dictionary<string, IndexScriptureVerse>>>? bookIndex;
                if (!Index_ScriptureCanonToBookToChapterToVerse.TryGetValue(verse.Canon, out bookIndex))
                {
                    bookIndex = new Dictionary<string, Dictionary<int, Dictionary<string, IndexScriptureVerse>>>();
                    Index_ScriptureCanonToBookToChapterToVerse[verse.Canon] = bookIndex;
                }

                Dictionary<int, Dictionary<string, IndexScriptureVerse>>? chapterIndex;
                if (!bookIndex.TryGetValue(verse.Book, out chapterIndex))
                {
                    chapterIndex = new Dictionary<int, Dictionary<string, IndexScriptureVerse>>();
                    bookIndex[verse.Book] = chapterIndex;
                }

                Dictionary<string, IndexScriptureVerse>? verseIndex;
                if (!chapterIndex.TryGetValue(verse.Chapter, out verseIndex))
                {
                    verseIndex = new Dictionary<string, IndexScriptureVerse>();
                    chapterIndex[verse.Chapter] = verseIndex;
                }

                verseIndex[verse.Verse] = verse;
            }

            foreach (IndexHymn hymn in Hymns)
            {
                Index_IdToEntity[hymn.EntityId] = hymn;
                Index_HymnNumberToHymn[hymn.Number] = hymn;
            }

            foreach (IndexBibleDictionaryTopic topic in BibleDictionary)
            {
                Index_IdToEntity[topic.EntityId] = topic;
            }

            foreach (IndexBookChapter chapter in BookChapters)
            {
                Index_IdToEntity[chapter.EntityId] = chapter;
                List<IndexBookChapter>? chapterList;
                if (!Index_BookIdToChapters.TryGetValue(chapter.BookId, out chapterList))
                {
                    chapterList = new List<IndexBookChapter>();
                    Index_BookIdToChapters[chapter.BookId] = chapterList;
                }

                chapterList.Add(chapter);
            }

            foreach (IndexByuSpeech speech in ByuSpeeches)
            {
                Index_IdToEntity[speech.EntityId] = speech;
            }

            foreach (IndexConferenceTalk talk in ConferenceTalks)
            {
                Index_IdToEntity[talk.EntityId] = talk;
                List<IndexConferenceTalk>? talkList;
                if (!Index_ConferenceToTalks.TryGetValue(talk.Conference, out talkList))
                {
                    talkList = new List<IndexConferenceTalk>();
                    Index_ConferenceToTalks[talk.Conference] = talkList;
                }

                talkList.Add(talk);
            }

            foreach (IndexJODDiscourse discourse in Discourses)
            {
                Index_IdToEntity[discourse.EntityId] = discourse;
                List<IndexJODDiscourse>? discourseList;
                if (!Index_JODVolumeToDiscourses.TryGetValue(discourse.JournalVolume, out discourseList))
                {
                    discourseList = new List<IndexJODDiscourse>();
                    Index_JODVolumeToDiscourses[discourse.JournalVolume] = discourseList;
                }

                discourseList.Add(discourse);
            }
        }
    }
}
