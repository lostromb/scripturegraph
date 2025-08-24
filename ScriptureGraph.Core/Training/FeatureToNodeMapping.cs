using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Graph;
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
                $"{word}\t{lang.ToBcp47Alpha3String()}");
        }

        public static KnowledgeGraphNodeId NGram(string word1, string word2, LanguageCode lang)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.NGram,
                $"{word1}\t{word2}\t{lang.ToBcp47Alpha3String()}");
        }

        public static KnowledgeGraphNodeId NGram(string word1, string word2, string word3, LanguageCode lang)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.NGram,
                $"{word1}\t{word2}\t{word3}\t{lang.ToBcp47Alpha3String()}");
        }

        public static KnowledgeGraphNodeId ScriptureVerse(string canon, string book, int chapter, int verse)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.ScriptureVerse,
                $"{canon}\t{book}\t{chapter}\t{verse}");
        }

        public static KnowledgeGraphNodeId TopicalGuideKeyword(string keyword)
        {
            return new KnowledgeGraphNodeId(KnowledgeGraphNodeType.TopicalGuideKeyword, keyword);
        }
    }
}
