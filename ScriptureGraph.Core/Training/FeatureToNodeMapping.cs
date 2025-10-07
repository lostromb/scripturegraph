using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training
{
    public static class FeatureToNodeMapping
    {
        public static KnowledgeGraphNodeId Entity(string entityName)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.Entity, entityName);
        }

        public static KnowledgeGraphNodeId Word(string word, LanguageCode lang)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.Word,
                $"{word}|{lang.ToBcp47Alpha3String()}");
        }

        public static KnowledgeGraphNodeId NGram(string word1, string word2, LanguageCode lang)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.NGram,
                $"{word1}|{word2}|{lang.ToBcp47Alpha3String()}");
        }

        public static KnowledgeGraphNodeId NGram(string word1, string word2, string word3, LanguageCode lang)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.NGram,
                $"{word1}|{word2}|{word3}|{lang.ToBcp47Alpha3String()}");
        }

        public static KnowledgeGraphNodeId ScriptureVerse(string book, int chapter, int verse)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.ScriptureVerse,
                $"{book}|{chapter}|{verse}");
        }

        public static KnowledgeGraphNodeId ScriptureSupplementalParagraph(string book, int chapter, string paragraph)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.ScriptureSupplementalPara,
                $"{book}|{chapter}|{paragraph}");
        }

        public static KnowledgeGraphNodeId ScriptureChapter(string book, int chapter)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.ScriptureChapter,
                $"{book}|{chapter}");
        }

        public static KnowledgeGraphNodeId ScriptureBook(string book)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.ScriptureBook,
                $"{book}");
        }

        public static KnowledgeGraphNodeId TopicalGuideKeyword(string keyword)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.TopicalGuideKeyword, keyword);
        }

        public static KnowledgeGraphNodeId BibleDictionaryTopic(string topic)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.BibleDictionaryTopic, topic);
        }

        public static KnowledgeGraphNodeId BibleDictionaryParagraph(string topic, int paragraph)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.BibleDictionaryParagraph, $"{topic}|{paragraph}");
        }

        public static KnowledgeGraphNodeId GuideToScripturesTopic(string topic)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.GuideToScripturesTopic, topic);
        }

        public static KnowledgeGraphNodeId TripleIndexTopic(string topic)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.TripleIndexTopic, topic);
        }

        public static KnowledgeGraphNodeId ConferenceTalk(int year, ConferencePhase phase, string talkId)
        {
            string phaseStr = phase == ConferencePhase.April ? "04" : "10";
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.ConferenceTalk, $"{year}|{phaseStr}|{talkId}");
        }

        public static KnowledgeGraphNodeId ConferenceTalkParagraph(int year, ConferencePhase phase, string talkId, int paragraph)
        {
            string phaseStr = phase == ConferencePhase.April ? "04" : "10";
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.ConferenceTalkParagraph, $"{year}|{phaseStr}|{talkId}|{paragraph}");
        }

        public static KnowledgeGraphNodeId ConferenceSpeaker(string speakerName)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.ConferenceSpeaker, speakerName.ToLowerInvariant().Replace(".", string.Empty));
        }

        // character bigrams are very inaccurate (in english)
        //public static KnowledgeGraphNodeId CharNGram(char ch1, char ch2)
        //{
        //    return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.CharNGram, $"{ch1}|{ch2}");
        //}

        public static KnowledgeGraphNodeId CharNGram(char ch1, char ch2, char ch3)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.CharNGram, $"{ch1}{ch2}{ch3}");
        }

        public static KnowledgeGraphNodeId BookChapter(string bookName, string chapter)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.BookChapter, $"{bookName}|{chapter}");
        }

        public static KnowledgeGraphNodeId BookChapterParagraph(string bookName, string chapter, string paragraph)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.BookParagraph, $"{bookName}|{chapter}|{paragraph}");
        }

        public static KnowledgeGraphNodeId BookChapterParagraph(KnowledgeGraphNodeId documentId, string paragraph)
        {
            if (documentId.Type != KnowledgeGraphNodeType.BookChapter)
            {
                throw new ArgumentException("Parent document ID must be a book chapter");
            }

            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.BookParagraph, $"{documentId.Name}|{paragraph}");
        }
    }
}
