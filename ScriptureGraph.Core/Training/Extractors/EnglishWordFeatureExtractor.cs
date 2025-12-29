using Durandal.Common.IO;
using Durandal.Common.NLP.Language;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    public static class EnglishWordFeatureExtractor
    {
        // Matches latin characters plus extended latin for accented chars
        // Matches contractions at the end of sentences
        // Matches hyphenated words but ignores leading and trailing hyphens
        public static readonly Regex WordMatcher = new Regex("(?:[\\w\\u00c0-\\u00ff\\u0100-\\u017f][\\-\\w\\u00c0-\\u00ff\\u0100-\\u017f]+[\\w\\u00c0-\\u00ff\\u0100-\\u017f]|[\\w\\u00c0-\\u00ff\\u0100-\\u017f]+)(?:[\\'\\’]\\w+)?");

        private static readonly Regex SentenceMatcher = new Regex("[\\w\\W]+?(?:$|[\\.\\?\\!][\\s\\.\\?\\'\\’\\\"\\)\\]\\!\\”]+)");

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
            int sentenceIdx = 0;
            KnowledgeGraphNodeId? prevSentenceEntity = null;
            foreach (string sentence in BreakSentenceLowerCase(input))
            {
                sentenceIdx++;
                KnowledgeGraphNodeId? sentenceEntity = null;
                if (rootEntity != null)
                {
                    sentenceEntity = FeatureToNodeMapping.MapParagraphToSentenceEntityIfApplicable(rootEntity.Value, sentenceIdx);

                    if (sentenceEntity != null)
                    {
                        // sentence -> root (vertical)
                        trainingFeaturesOut.Add(new TrainingFeature(
                            rootEntity.Value,
                            sentenceEntity.Value,
                            TrainingFeatureType.SentenceAssociation));
                    }
                }

                KnowledgeGraphNodeId? sentenceOrRootEntity = sentenceEntity ?? rootEntity;

                string[] words = BreakWords(sentence).ToArray();
                for (int startIndex = 0; startIndex < words.Length; startIndex++)
                {
                    // Single words associated with the root entity
                    if (sentenceOrRootEntity.HasValue)
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            sentenceOrRootEntity.Value,
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
                        if (sentenceOrRootEntity.HasValue)
                        {
                            trainingFeaturesOut.Add(new TrainingFeature(
                            sentenceOrRootEntity.Value,
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
                            if (sentenceOrRootEntity.HasValue)
                            {
                                trainingFeaturesOut.Add(new TrainingFeature(
                                sentenceOrRootEntity.Value,
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

                if (sentenceEntity != null && prevSentenceEntity != null)
                {
                    // sentence -> sentence (horizontal)
                    trainingFeaturesOut.Add(new TrainingFeature(
                        sentenceEntity.Value,
                        prevSentenceEntity.Value,
                        TrainingFeatureType.SentenceAssociation));
                }

                prevSentenceEntity = sentenceEntity;
            }
        }

        // (.+?),\s*(.+?)(?:$|(,\s*.+))
        private static readonly Regex CommaInverter = new Regex("(.+?),\\s*(.+?)(?:$|(,\\s*.+))");

        /// <summary>
        /// Given a sentence like "Jesus Christ, Appearances of", invert the comma so it appears first "Appearances of Jesus Christ".
        /// This attempts to reverse the "librarian's" alphabetical format applies to many articles in TG, BD, etc.
        /// For sentences with multiple commas, only the first one will be inverted.
        /// Operates on a string in-place and returns true if the string was changed.
        /// </summary>
        /// <param name="sentence"></param>
        /// <returns></returns>
        public static bool PerformCommaInversion(ref string sentence)
        {
            using (PooledStringBuilder pooledSb = StringBuilderPool.Rent())
            {
                Match m = CommaInverter.Match(sentence);
                if (m.Success)
                {
                    pooledSb.Builder.Append(m.Groups[2].Value);
                    pooledSb.Builder.Append(' ');
                    pooledSb.Builder.Append(m.Groups[1].Value);
                    if (m.Groups[3].Success)
                    {
                        // There's a second comma. Just append it (this group includes the comma itself)
                        pooledSb.Builder.Append(m.Groups[3].Value);
                    }

                    sentence = pooledSb.Builder.ToString();
                    return true;
                }

                return false;
            }
        }

        public static IEnumerable<KnowledgeGraphNodeId> ExtractCharLevelNGrams(string input)
        {
            foreach (string sentence in BreakSentenceLowerCase(input))
            {
                // Word-level ngrams, including start of sentence / end of sentence boundaries
                List<string> words = BreakWords(sentence).ToList();
                for (int index = 0; index < words.Count; index++)
                {
                    yield return FeatureToNodeMapping.Word(words[index], LanguageCode.ENGLISH);
                }

                words.Insert(0, "STKN");
                words.Add("ETKN");
                for (int startIndex = 0; startIndex < words.Count; startIndex++)
                {
                    if (startIndex < words.Count - 1)
                    {
                        yield return FeatureToNodeMapping.NGram(words[startIndex], words[startIndex + 1], LanguageCode.ENGLISH);

                        if (startIndex < words.Count - 2)
                        {
                            yield return FeatureToNodeMapping.NGram(words[startIndex], words[startIndex + 1], words[startIndex + 2], LanguageCode.ENGLISH);
                        }
                    }
                }

                // And char-level ngrams
                foreach (string word in words)
                {
                    foreach (KnowledgeGraphNodeId ngram in ExtractCharLevelNGramsSingleWord(word))
                    {
                        yield return ngram;
                    }
                }
            }
        }

        private static IEnumerable<KnowledgeGraphNodeId> ExtractCharLevelNGramsSingleWord(string singleWord)
        {
            int numChars = singleWord.Length + 2;
            using (PooledBuffer<char> chars = BufferPool<char>.Rent(numChars))
            {
                singleWord.CopyTo(0, chars.Buffer, 1, singleWord.Length);
                chars.Buffer[0] = '[';
                chars.Buffer[numChars - 1] = ']';
                for (int startIndex = 0; startIndex < numChars; startIndex++)
                {
                    if (startIndex < numChars - 2)
                    {
                        yield return FeatureToNodeMapping.CharNGram(chars.Buffer[startIndex], chars.Buffer[startIndex + 1], chars.Buffer[startIndex + 2]);
                    }
                }
            }
        }
    }
}
