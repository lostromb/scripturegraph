using Durandal.Common.IO;
using Durandal.Common.NLP.Language;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
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

        public static IEnumerable<Substring> BreakWords(string input)
        {
            int startIndex = 0;
            while (startIndex < input.Length)
            {
                Match wordMatch = WordMatcher.Match(input, startIndex);
                if (wordMatch.Success)
                {
                    yield return new Substring(wordMatch.Value, wordMatch, null);
                    startIndex = wordMatch.Index + wordMatch.Length;
                }
                else
                {
                    startIndex = input.Length;
                }
            }
        }

        public static IEnumerable<Substring> BreakWordsLowerCase(string input)
        {
            foreach (Substring word in BreakWords(input))
            {
                yield return new Substring(word.Text.ToLowerInvariant(), word.Range, null);
            }
        }

        public static IEnumerable<Substring> BreakSentences(string input, KnowledgeGraphNodeId? paragraphEntity = null)
        {
            int startIndex = 0;
            int sentenceIdx = 0;
            while (startIndex < input.Length)
            {
                Match sentenceMatch = SentenceMatcher.Match(input, startIndex);
                if (sentenceMatch.Success)
                {
                    sentenceIdx++;
                    KnowledgeGraphNodeId? sentenceEntity = null;
                    if (paragraphEntity != null)
                    {
                        sentenceEntity = FeatureToNodeMapping.MapParagraphToSentenceEntityIfApplicable(paragraphEntity.Value, sentenceIdx);
                    }

                    yield return new Substring(sentenceMatch.Value, sentenceMatch, sentenceEntity);
                    startIndex = sentenceMatch.Index + sentenceMatch.Length;
                }
                else
                {
                    startIndex = input.Length;
                }
            }
        }

        public static IEnumerable<Substring> BreakSentencesLowerCase(string input)
        {
            foreach (Substring sentence in BreakSentences(input))
            {
                yield return new Substring(sentence.Text.ToLowerInvariant(), sentence.Range, sentence.EntityId);
            }
        }

        public static IEnumerable<KnowledgeGraphNodeId> ExtractNGrams(string input)
        {
            foreach (Substring sentence in BreakSentencesLowerCase(input))
            {
                Substring[] words = BreakWords(sentence.Text).ToArray();
                for (int startIndex = 0; startIndex < words.Length; startIndex++)
                {
                    yield return FeatureToNodeMapping.Word(words[startIndex].Text, LanguageCode.ENGLISH);
                    if (startIndex < words.Length - 1)
                    {
                        yield return FeatureToNodeMapping.NGram(words[startIndex].Text, words[startIndex + 1].Text, LanguageCode.ENGLISH);

                        if (startIndex < words.Length - 2)
                        {
                            yield return FeatureToNodeMapping.NGram(words[startIndex].Text, words[startIndex + 1].Text, words[startIndex + 2].Text, LanguageCode.ENGLISH);
                        }
                    }
                }
            }
        }

        public static IEnumerable<Substring> ExtractTrainingFeatures(
            string input,
            List<TrainingFeature> trainingFeaturesOut,
            KnowledgeGraphNodeId? rootEntity = null)
        {
            KnowledgeGraphNodeId? prevSentenceEntity = null;
            foreach (Substring sentence in BreakSentencesLowerCase(input))
            {
                KnowledgeGraphNodeId? sentenceEntity = sentence.EntityId;
                if (rootEntity.HasValue && sentenceEntity.HasValue)
                {
                    // sentence -> root (vertical)
                    trainingFeaturesOut.Add(new TrainingFeature(
                        rootEntity.Value,
                        sentenceEntity.Value,
                        TrainingFeatureType.SentenceAssociation));
                }

                yield return sentence;

                KnowledgeGraphNodeId? sentenceOrRootEntity = sentenceEntity ?? rootEntity;

                Substring[] words = BreakWords(sentence.Text).ToArray();
                for (int startIndex = 0; startIndex < words.Length; startIndex++)
                {
                    // Single words associated with the root entity
                    if (sentenceOrRootEntity.HasValue)
                    {
                        trainingFeaturesOut.Add(new TrainingFeature(
                            sentenceOrRootEntity.Value,
                            FeatureToNodeMapping.Word(words[startIndex].Text, LanguageCode.ENGLISH),
                            TrainingFeatureType.WordAssociation));
                    }

                    if (startIndex < words.Length - 1)
                    {
                        // All words cross referenced with each other
                        for (int endIndex = startIndex + 1; endIndex <= startIndex + MAX_WORD_ASSOCIATION_ORDER && endIndex < words.Length; endIndex++)
                        {
                            trainingFeaturesOut.Add(new TrainingFeature(
                                FeatureToNodeMapping.Word(words[startIndex].Text, LanguageCode.ENGLISH),
                                FeatureToNodeMapping.Word(words[endIndex].Text, LanguageCode.ENGLISH),
                                TrainingFeatureType.WordAssociation));
                        }

                        // Bigrams
                        // bigram -> root entity
                        KnowledgeGraphNodeId bigram = FeatureToNodeMapping.NGram(words[startIndex].Text, words[startIndex + 1].Text, LanguageCode.ENGLISH);
                        if (sentenceOrRootEntity.HasValue)
                        {
                            trainingFeaturesOut.Add(new TrainingFeature(
                            sentenceOrRootEntity.Value,
                            bigram,
                            TrainingFeatureType.NgramAssociation));
                        }

                        // bigram -> words that are in it
                        trainingFeaturesOut.Add(new TrainingFeature(
                            FeatureToNodeMapping.Word(words[startIndex].Text, LanguageCode.ENGLISH),
                            bigram,
                            TrainingFeatureType.WordAssociation));

                        trainingFeaturesOut.Add(new TrainingFeature(
                            FeatureToNodeMapping.Word(words[startIndex + 1].Text, LanguageCode.ENGLISH),
                            bigram,
                            TrainingFeatureType.WordAssociation));

                        // Trigrams
                        if (startIndex < words.Length - 2)
                        {
                            // trigram -> root entity
                            KnowledgeGraphNodeId trigram = FeatureToNodeMapping.NGram(words[startIndex].Text, words[startIndex + 1].Text, words[startIndex + 2].Text, LanguageCode.ENGLISH);
                            if (sentenceOrRootEntity.HasValue)
                            {
                                trainingFeaturesOut.Add(new TrainingFeature(
                                sentenceOrRootEntity.Value,
                                trigram,
                                TrainingFeatureType.NgramAssociation));
                            }

                            // trigram -> words that are in it
                            trainingFeaturesOut.Add(new TrainingFeature(
                                FeatureToNodeMapping.Word(words[startIndex].Text, LanguageCode.ENGLISH),
                                trigram,
                                TrainingFeatureType.WordAssociation));

                            trainingFeaturesOut.Add(new TrainingFeature(
                                FeatureToNodeMapping.Word(words[startIndex + 1].Text, LanguageCode.ENGLISH),
                                trigram,
                                TrainingFeatureType.WordAssociation));

                            trainingFeaturesOut.Add(new TrainingFeature(
                                FeatureToNodeMapping.Word(words[startIndex + 2].Text, LanguageCode.ENGLISH),
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
            foreach (Substring sentence in BreakSentencesLowerCase(input))
            {
                // Word-level ngrams, including start of sentence / end of sentence boundaries
                List<Substring> words = BreakWords(sentence.Text).ToList();
                for (int index = 0; index < words.Count; index++)
                {
                    yield return FeatureToNodeMapping.Word(words[index].Text, LanguageCode.ENGLISH);
                }

                words.Insert(0, new Substring("STKN", new IntRange(0, 0), null));
                words.Add(new Substring("ETKN", new IntRange(sentence.Range.End, sentence.Range.End), null));
                for (int startIndex = 0; startIndex < words.Count; startIndex++)
                {
                    if (startIndex < words.Count - 1)
                    {
                        yield return FeatureToNodeMapping.NGram(words[startIndex].Text, words[startIndex + 1].Text, LanguageCode.ENGLISH);

                        if (startIndex < words.Count - 2)
                        {
                            yield return FeatureToNodeMapping.NGram(words[startIndex].Text, words[startIndex + 1].Text, words[startIndex + 2].Text, LanguageCode.ENGLISH);
                        }
                    }
                }

                // And char-level ngrams
                foreach (Substring word in words)
                {
                    foreach (KnowledgeGraphNodeId ngram in ExtractCharLevelNGramsSingleWord(word.Text))
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
