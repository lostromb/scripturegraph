using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Graph;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    public static class EnglishWordFeatureExtractor
    {
        // Matches latin characters plus extended latin for accented chars
        // Matches contractions at the end of sentences
        // Matches hyphenated words but ignores leading and trailing hyphens
        public static readonly Regex WordMatcher = new Regex("(?:[\\w\\u00c0-\\u00ff\\u0100-\\u017f][\\-\\w\\u00c0-\\u00ff\\u0100-\\u017f]+[\\w\\u00c0-\\u00ff\\u0100-\\u017f]|[\\w\\u00c0-\\u00ff\\u0100-\\u017f]+)(?:'\\w+)?");

        private static readonly Regex SentenceMatcher = new Regex("[\\w\\W]+?(?:$|[\\.\\?\\!][\\s\\.\\?\\'\\\"\\)\\]\\!\\”$]+)");

        private const int MAX_WORD_ASSOCIATION_ORDER = 7;

        public static IEnumerable<string> BreakWords(string input)
        {
            int startIndex = 0;
            while (startIndex < input.Length)
            {
                Match wordMatch = WordMatcher.Match(input, startIndex);
                if (wordMatch.Success)
                {
                    yield return wordMatch.Value;
                    startIndex = wordMatch.Index + wordMatch.Length;
                }
                else
                {
                    startIndex = input.Length;
                }
            }
        }

        public static IEnumerable<string> BreakWordsLowerCase(string input)
        {
            foreach (string word in BreakWords(input))
            {
                yield return word.ToLowerInvariant();
            }
        }

        public static IEnumerable<string> BreakSentence(string input)
        {
            int startIndex = 0;
            while (startIndex < input.Length)
            {
                Match sentenceMatch = SentenceMatcher.Match(input, startIndex);
                if (sentenceMatch.Success)
                {
                    yield return sentenceMatch.Value;
                    startIndex = sentenceMatch.Index + sentenceMatch.Length;
                }
                else
                {
                    startIndex = input.Length;
                }
            }
        }

        public static IEnumerable<string> BreakSentenceLowerCase(string input)
        {
            foreach (string sentence in BreakSentence(input))
            {
                yield return sentence.ToLowerInvariant();
            }
        }

        public static IEnumerable<KnowledgeGraphNodeId> ExtractNGrams(string input)
        {
            foreach (string sentence in BreakSentenceLowerCase(input))
            {
                string[] words = BreakWords(sentence).ToArray();
                for (int startIndex = 0; startIndex < words.Length; startIndex++)
                {
                    yield return FeatureToNodeMapping.Word(words[startIndex], LanguageCode.ENGLISH);
                    if (startIndex < words.Length - 1)
                    {
                        yield return FeatureToNodeMapping.NGram(words[startIndex], words[startIndex + 1], LanguageCode.ENGLISH);

                        if (startIndex < words.Length - 2)
                        {
                            yield return FeatureToNodeMapping.NGram(words[startIndex], words[startIndex + 1], words[startIndex + 2], LanguageCode.ENGLISH);
                        }
                    }
                }
            }
        }

        public static void ExtractTrainingFeatures(string input, List<TrainingFeature> trainingFeaturesOut, KnowledgeGraphNodeId? rootEntity = null)
        {
            foreach (string sentence in BreakSentenceLowerCase(input))
            {
                string[] words = BreakWords(sentence).ToArray();
                for (int startIndex = 0; startIndex < words.Length; startIndex++)
                {
                    // Single words associated with the root entity
                    if (rootEntity.HasValue)
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            rootEntity.Value,
                            FeatureToNodeMapping.Word(words[startIndex], LanguageCode.ENGLISH),
                            TrainingFeatureType.WordAssociation));
                    }

                    if (startIndex < words.Length - 1)
                    {
                        // All words cross referenced with each other
                        for (int endIndex = startIndex + 1; endIndex <= startIndex + MAX_WORD_ASSOCIATION_ORDER && endIndex < words.Length; endIndex++)
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
                        if (startIndex < words.Length - 2)
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
}
