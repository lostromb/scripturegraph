using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using Durandal.Common.Parsers;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training
{
    public class ScripturePageFeatureExtractor
    {
        private static readonly Regex UrlPathParser = new Regex("\\/study\\/scriptures\\/(.+?)\\/(.+?)\\/(\\d+)");
        private static readonly Regex IntroParser = new Regex("<p class=\\\"intro\\\".+?>(.+?)<\\/p>");
        private static readonly Regex VerseParser = new Regex("<p class=\\\"verse\\\".+?verse-number\\\">(\\d+) <\\/span>(.+?)<\\/p>");
        private static readonly Regex NoteParser = new Regex("<p[^>]+?id=\"note(.+?)(?:_p\\d+)?\">([\\w\\W]+?)<\\/p>");
        private static readonly Regex ScriptureRefParser = new Regex("class=\\\"scripture-ref\\\"\\s+href=\\\"\\/study\\/scriptures\\/(.+?)\\/(.+?)(?:\\/(\\d+?))?\\?lang=eng(?:&amp;id=(.+?)(?:#.+?))?\\\"");
        private static readonly Regex InlineFootnoteParser = new Regex("<a class=\"study-note-ref\".+?#note(.+?)\".+?<\\/sup>(.+?)<\\/a>");
        private static readonly Regex PageBreakRemover = new Regex("<span class=\"page-break\".+?</span>");

        // Matches latin characters plus extended latin for accented chars
        // Matches contractions at the end of sentences
        // Matches hyphenated words but ignores leading and trailing hyphens
        private static readonly Regex WordMatcher = new Regex("(?:[\\w\\u00c0-\\u00ff\\u0100-\\u017f][\\-\\w\\u00c0-\\u00ff\\u0100-\\u017f]+[\\w\\u00c0-\\u00ff\\u0100-\\u017f]|[\\w\\u00c0-\\u00ff\\u0100-\\u017f]+)(?:'\\w+)?");

        public static void ExtractFeatures(string scriptureHtmlPage, Uri pageUrl, ILogger logger, List<TrainingFeature> returnVal)
        {
            try
            {
                Match urlParse = UrlPathParser.Match(pageUrl.AbsolutePath);
                if (!urlParse.Success)
                {
                    logger.Log("Failed to parse URL", LogLevel.Err);
                    return;
                }

                Dictionary<int, StructuredVerse> verses = new Dictionary<int, StructuredVerse>();
                Dictionary<string, StructuredFootnote> footnotes = new Dictionary<string, StructuredFootnote>();
                string canon = urlParse.Groups[1].Value;
                string book = urlParse.Groups[2].Value;
                int chapter = int.Parse(urlParse.Groups[3].Value);

                // Parse the intro as verse 0 if present
                Match introMatch = IntroParser.Match(scriptureHtmlPage);
                if (introMatch.Success)
                {
                    string introTextWithAnnotation = introMatch.Groups[1].Value;
                    verses.Add(0, new StructuredVerse(canon, book, chapter, 0, introTextWithAnnotation));
                }

                // Parse each individual verse and note into structured form
                foreach (Match verseMatch in VerseParser.Matches(scriptureHtmlPage))
                {
                    int verseNum = int.Parse(verseMatch.Groups[1].Value);
                    string verseTextWithAnnotation = verseMatch.Groups[2].Value;
                    //logger.Log(verseTextWithAnnotation);

                    // Take the time to remove page breaks and other unused HTML tags here
                    verseTextWithAnnotation = StringUtils.RegexRemove(PageBreakRemover, verseTextWithAnnotation);

                    verses.Add(verseNum, new StructuredVerse(canon, book, chapter, verseNum, verseTextWithAnnotation));
                }

                foreach (Match footnoteMatch in NoteParser.Matches(scriptureHtmlPage))
                {
                    //logger.Log(footnoteMatch.Value);
                    string noteId = footnoteMatch.Groups[1].Value;
                    string footnoteTextWithAnnotation = footnoteMatch.Groups[2].Value;
                    StructuredFootnote footnote = new StructuredFootnote(noteId, footnoteTextWithAnnotation);

                    //logger.Log(noteId);

                    foreach (Match footnotScriptureRef in ScriptureRefParser.Matches(footnoteTextWithAnnotation))
                    {
                        string refCanon = footnotScriptureRef.Groups[1].Value;
                        string refBook = footnotScriptureRef.Groups[2].Value;
                        if (!footnotScriptureRef.Groups[3].Success)
                        {
                            // It's a reference without chapter or verse info (usually TG or BD)
                            //logger.Log($"Adding reference without chapter to {refCanon} {refBook}");
                            footnote.ScriptureReferences.Add(new ScriptureReference(refCanon, refBook));
                        }
                        else
                        {
                            int refChapter = int.Parse(footnotScriptureRef.Groups[3].Value);
                            if (footnotScriptureRef.Groups[4].Success)
                            {
                                string refVerseEncoded = footnotScriptureRef.Groups[4].Value.Replace("p", "");
                                // Parse the encoded verses.
                                // Examples:
                                // p5 (verse 5)
                                // p1-p4 (verses 1-4)
                                // p3,p6 (verses 3 and 6)
                                // p21-p23,p27 (verses 21-23 and 27)
                                //logger.Log($"Decoding verse ref {refVerseEncoded}");
                                string[] segments = refVerseEncoded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                foreach (string verseRange in segments)
                                {
                                    int hyphenIndex = verseRange.IndexOf('-');
                                    if (hyphenIndex > 0)
                                    {
                                        // We've removed 'p' already, so it's presumably something like "1-5"
                                        // Some references can be to chapter headers - this is common in D&C where
                                        // it's giving historical context present only in the headers.
                                        // Example: /scriptures/bofm/4-ne/1?lang=eng&id=title1-intro1
                                        int rangeStart, rangeEnd;
                                        if (int.TryParse(verseRange.AsSpan(0, hyphenIndex), out rangeStart) &&
                                            int.TryParse(verseRange.AsSpan(hyphenIndex + 1), out rangeEnd))
                                        {
                                            //logger.Log($"Adding reference to verse range {refCanon} {refBook} {refChapter}:{rangeStart}-{rangeEnd}");
                                            for (; rangeStart <= rangeEnd; rangeStart++)
                                            {
                                                footnote.ScriptureReferences.Add(
                                                    new ScriptureReference(refCanon, refBook, refChapter, rangeStart));
                                            }
                                        }
                                        else
                                        {
                                            // In this system we only really track the intro paragraph
                                            // (if it was written by the original text's author) and count it
                                            // as verse 0. So we just assume any non-numerical verse reference
                                            // is pointing to this header and label it as verse 0
                                            footnote.ScriptureReferences.Add(
                                                new ScriptureReference(refCanon, refBook, refChapter, 0));
                                        }
                                    }
                                    else
                                    {
                                        int singleVerse;
                                        if (int.TryParse(verseRange, out singleVerse))
                                        {
                                            //logger.Log($"Adding reference with single verse {refCanon} {refBook} {refChapter}:{singleVerse}");
                                            footnote.ScriptureReferences.Add(
                                                new ScriptureReference(refCanon, refBook, refChapter, singleVerse));
                                        }
                                        else
                                        {
                                            // Very rare cases like 1 Ne 8:19 will have footnotes refering to other footnotes
                                            // We're just gonna ignore those
                                            logger.Log($"Invalid cross-reference location to {verseRange}, ignoring...", LogLevel.Wrn);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Not sure if this is possible but whatever
                                //logger.Log($"Adding reference with chapter but no verse to {refCanon} {refBook} {refChapter}");
                                footnote.ScriptureReferences.Add(
                                    new ScriptureReference(refCanon, refBook, refChapter, null)); 
                            }
                        }
                    }

                    footnotes.Add(noteId, footnote);
                }

                // Now restructure each verse into a series of words with correlated footnotes
                List<SingleWordWithFootnotes> words = new List<SingleWordWithFootnotes>();
                StringBuilder stringBuffer = new StringBuilder();
                foreach (StructuredVerse verse in verses.Values.OrderBy(s => s.Verse))
                {
                    stringBuffer.Append(verse.Text);
                    while (true)
                    {
                        string currentText = stringBuffer.ToString();
                        Match wordMatch = WordMatcher.Match(currentText);
                        if (!wordMatch.Success)
                        {
                            break;
                        }

                        Match taggedWordMatch = InlineFootnoteParser.Match(currentText);
                        if (taggedWordMatch.Success && taggedWordMatch.Index < wordMatch.Index)
                        {
                            // It's one or more words tagged with a footnote
                            string footnoteNoteRef = taggedWordMatch.Groups[1].Value; // "20d"
                            string footnotedWords = taggedWordMatch.Groups[2].Value.ToLowerInvariant(); // "cast out"
                            int subWordIndex = 0;
                            StructuredFootnote? footnoteForTheseWords;
                            if (!footnotes.TryGetValue(footnoteNoteRef, out footnoteForTheseWords))
                            {
                                logger.Log($"Could not resolve footnote ref {footnoteNoteRef} in verse {verse.Verse}", LogLevel.Err);
                            }
                            else
                            {
                                while (true)
                                {
                                    Match subWordMatch = WordMatcher.Match(footnotedWords, subWordIndex);
                                    if (!subWordMatch.Success)
                                    {
                                        break;
                                    }

                                    subWordIndex = subWordMatch.Index + subWordMatch.Length;
                                    words.Add(new SingleWordWithFootnotes(subWordMatch.Value, footnoteForTheseWords));
                                }
                            }

                            stringBuffer.Remove(0, taggedWordMatch.Index + taggedWordMatch.Length);
                        }
                        else
                        {
                            // It's just an untagged word
                            words.Add(new SingleWordWithFootnotes(wordMatch.Value.ToLowerInvariant()));

                            stringBuffer.Remove(0, wordMatch.Index + wordMatch.Length);
                        }
                    }

                    // Dump the wordbreaker and tagger output for debug
                    //logger.Log($"Verse {verse.Verse}: {string.Join(' ', words)}");

                    // now FINALLY we can extract features
                    ExtractFeaturesFromSingleVerse(verse, words, logger, returnVal);

                    words.Clear();
                }
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }

        private static void ExtractFeaturesFromSingleVerse(
            StructuredVerse currentVerse,
            List<SingleWordWithFootnotes> words,
            ILogger logger,
            List<TrainingFeature> trainingFeaturesOut)
        {
            // Node for this verse - we use it a lot
            KnowledgeGraphNodeId thisVerseNode = FeatureToNodeMapping.ScriptureVerse(
                currentVerse.Canon,
                currentVerse.Book,
                currentVerse.Verse,
                currentVerse.Chapter);

            // Verse reference -> all the words that are in it
            foreach (var word in words)
            {
                trainingFeaturesOut.Add(new TrainingFeature(
                    thisVerseNode,
                    FeatureToNodeMapping.Word(word.Word, LanguageCode.ENGLISH),
                    TrainingFeatureType.WordAssociation));
            }

            // All words cross referenced with each other
            const int MAX_WORD_ASSOCIATION_ORDER = 5;
            for (int startIndex = 0; startIndex < words.Count - MAX_WORD_ASSOCIATION_ORDER; startIndex++)
            {
                for (int endIndex = startIndex + 1; endIndex <= startIndex + MAX_WORD_ASSOCIATION_ORDER && endIndex < words.Count; endIndex++)
                {
                    trainingFeaturesOut.Add(new TrainingFeature(
                        FeatureToNodeMapping.Word(words[startIndex].Word, LanguageCode.ENGLISH),
                        FeatureToNodeMapping.Word(words[endIndex].Word, LanguageCode.ENGLISH),
                    TrainingFeatureType.WordAssociation));
                }
            }

            // Bi- and Trigrams from words in this verse
            // TODO

            // Cross-references between this verse and other verses based on footnotes
            foreach (var word in words)
            {
                if (word.Footnote != null)
                {
                    foreach (var scriptureRef in word.Footnote.ScriptureReferences)
                    {
                        if (string.Equals(scriptureRef.Canon, "tg", StringComparison.Ordinal))
                        {
                            // Topical guide topic
                            KnowledgeGraphNodeId refNodeId = FeatureToNodeMapping.TopicalGuideKeyword(scriptureRef.Book);
                            trainingFeaturesOut.Add(new TrainingFeature(
                                thisVerseNode,
                                refNodeId,
                                TrainingFeatureType.EntityReference));
                            trainingFeaturesOut.Add(new TrainingFeature(
                                FeatureToNodeMapping.Word(word.Word, LanguageCode.ENGLISH),
                                refNodeId,
                                TrainingFeatureType.WordDesignation));
                        }
                        else if (string.Equals(scriptureRef.Canon, "bd", StringComparison.Ordinal))
                        {
                            // Bible dictionary topic
                            // TODO handle
                        }
                        else
                        {
                            if (scriptureRef.Chapter.HasValue &&
                                scriptureRef.Verse.HasValue)
                            {
                                // Regular scripture ref
                                KnowledgeGraphNodeId refNodeId = FeatureToNodeMapping.ScriptureVerse(
                                    scriptureRef.Canon,
                                    scriptureRef.Book,
                                    scriptureRef.Chapter.Value,
                                    scriptureRef.Verse.Value);
                                trainingFeaturesOut.Add(new TrainingFeature(
                                    thisVerseNode,
                                    refNodeId,
                                    TrainingFeatureType.EntityReference));
                                trainingFeaturesOut.Add(new TrainingFeature(
                                    FeatureToNodeMapping.Word(word.Word, LanguageCode.ENGLISH),
                                    refNodeId,
                                    TrainingFeatureType.WordDesignation));
                            }
                            else
                            {
                                // Something else. TODO find a case for this and handle it
                                logger.Log($"Don't know how to handle reference to {scriptureRef.Canon} {scriptureRef.Book}");
                            }
                        }
                    }
                }
            }
        }

        private class SingleWordWithFootnotes
        {
            public SingleWordWithFootnotes(string word, StructuredFootnote? footnote = null)
            {
                Word = word;
                Footnote = footnote;
            }

            public string Word;
            public StructuredFootnote? Footnote;

            public override string? ToString()
            {
                if (Footnote != null)
                {
                    return $"{Word}[{Footnote.NoteId}]";
                }
                else
                {
                    return Word;
                }
            }
        }

        private class StructuredVerse
        {
            public StructuredVerse(string canon, string book, int chapter, int verse, string text)
            {
                Canon = canon;
                Book = book;
                Chapter = chapter;
                Verse = verse;
                Text = text;
            }

            public string Canon;
            public string Book;
            public int Chapter;
            public int Verse;
            public string Text;
        }

        private class StructuredFootnote
        {
            public StructuredFootnote(string noteId, string text)
            {
                NoteId = noteId;
                Text = text;
            }

            public string NoteId;
            public string Text;
            public List<ScriptureReference> ScriptureReferences { get; } = new List<ScriptureReference>();
        }

        private class ScriptureReference
        {
            public ScriptureReference(string canon, string book, int? chapter = null, int? verse = null)
            {
                Canon = canon;
                Book = book;
                Chapter = chapter;
                Verse = verse;
            }

            public string Canon;
            public string Book;
            public int? Chapter;
            public int? Verse;
        }
    }
}
