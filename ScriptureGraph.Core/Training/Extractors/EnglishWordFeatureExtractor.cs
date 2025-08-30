using Durandal.Common.Audio.WebRtc;
using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using Durandal.Common.Parsers;
using ScriptureGraph.Core.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training.Extractors
{
    public static class EnglishWordFeatureExtractor
    {
        // Matches latin characters plus extended latin for accented chars
        // Matches contractions at the end of sentences
        // Matches hyphenated words but ignores leading and trailing hyphens
        public static readonly Regex WordMatcher = new Regex("(?:[\\w\\u00c0-\\u00ff\\u0100-\\u017f][\\-\\w\\u00c0-\\u00ff\\u0100-\\u017f]+[\\w\\u00c0-\\u00ff\\u0100-\\u017f]|[\\w\\u00c0-\\u00ff\\u0100-\\u017f]+)(?:'\\w+)?");

        private const int MAX_WORD_ASSOCIATION_ORDER = 7;

        private static List<string> WordBreak(string input)
        {
            List<string> brokenWords = new List<string>();

            int startIndex = 0;
            while (startIndex < input.Length)
            {
                Match wordMatch = WordMatcher.Match(input, startIndex);
                if (wordMatch.Success)
                {
                    brokenWords.Add(wordMatch.Value.ToLowerInvariant());
                    startIndex = wordMatch.Index + wordMatch.Length;
                }
                else
                {
                    startIndex = input.Length;
                }
            }

            return brokenWords;
        }

        public static IEnumerable<KnowledgeGraphNodeId> ExtractNGrams(string input)
        {
            List<string> words = WordBreak(input);
            for (int startIndex = 0; startIndex < words.Count; startIndex++)
            {
                yield return FeatureToNodeMapping.Word(words[startIndex], LanguageCode.ENGLISH);
                if (startIndex < words.Count - 1)
                {
                    yield return FeatureToNodeMapping.NGram(words[startIndex], words[startIndex + 1], LanguageCode.ENGLISH);

                    if (startIndex < words.Count - 2)
                    {
                        yield return FeatureToNodeMapping.NGram(words[startIndex], words[startIndex + 1], words[startIndex + 2], LanguageCode.ENGLISH);
                    }
                }
            }
        }

        public static void ExtractTrainingFeatures(string input, List<TrainingFeature> trainingFeaturesOut, KnowledgeGraphNodeId? rootEntity = null)
        {
            List<string> words = WordBreak(input);
            for (int startIndex = 0; startIndex < words.Count; startIndex++)
            {
                // Single words associated with the root entity
                if (rootEntity.HasValue)
                {
                    trainingFeaturesOut.Add(new TrainingFeature(
                        rootEntity.Value,
                        FeatureToNodeMapping.Word(words[startIndex], LanguageCode.ENGLISH),
                        TrainingFeatureType.WordAssociation));
                }

                if (startIndex < words.Count - 1)
                {
                    // All words cross referenced with each other
                    for (int endIndex = startIndex + 1; endIndex <= startIndex + MAX_WORD_ASSOCIATION_ORDER && endIndex < words.Count; endIndex++)
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            FeatureToNodeMapping.Word(words[startIndex], LanguageCode.ENGLISH),
                            FeatureToNodeMapping.Word(words[endIndex], LanguageCode.ENGLISH),
                            TrainingFeatureType.WordAssociation));
                    }

                    // Bigrams
                    // bigram -> root entity
                    KnowledgeGraphNodeId bigram = FeatureToNodeMapping.NGram(words[startIndex], words[startIndex + 1], LanguageCode.ENGLISH);
                    if (rootEntity.HasValue)
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                        rootEntity.Value,
                        bigram,
                        TrainingFeatureType.NgramAssociation));
                    }

                    // bigram -> words that are in it
                    trainingFeaturesOut.Add(new TrainingFeature(
                        FeatureToNodeMapping.Word(words[startIndex], LanguageCode.ENGLISH),
                        bigram,
                        TrainingFeatureType.WordAssociation));

                    trainingFeaturesOut.Add(new TrainingFeature(
                        FeatureToNodeMapping.Word(words[startIndex + 1], LanguageCode.ENGLISH),
                        bigram,
                        TrainingFeatureType.WordAssociation));

                    // Trigrams
                    if (startIndex < words.Count - 2)
                    {
                        // trigram -> root entity
                        KnowledgeGraphNodeId trigram = FeatureToNodeMapping.NGram(words[startIndex], words[startIndex + 1], words[startIndex + 2], LanguageCode.ENGLISH);
                        if (rootEntity.HasValue)
                        {
                            trainingFeaturesOut.Add(new TrainingFeature(
                            rootEntity.Value,
                            trigram,
                            TrainingFeatureType.NgramAssociation));
                        }

                        // trigram -> words that are in it
                        trainingFeaturesOut.Add(new TrainingFeature(
                            FeatureToNodeMapping.Word(words[startIndex], LanguageCode.ENGLISH),
                            trigram,
                            TrainingFeatureType.WordAssociation));

                        trainingFeaturesOut.Add(new TrainingFeature(
                            FeatureToNodeMapping.Word(words[startIndex + 1], LanguageCode.ENGLISH),
                            trigram,
                            TrainingFeatureType.WordAssociation));

                        trainingFeaturesOut.Add(new TrainingFeature(
                            FeatureToNodeMapping.Word(words[startIndex + 2], LanguageCode.ENGLISH),
                            trigram,
                            TrainingFeatureType.WordAssociation));
                    }
                }
            }
        }
    }
}
