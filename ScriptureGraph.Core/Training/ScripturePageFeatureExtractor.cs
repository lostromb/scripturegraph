using Durandal.Common.Logger;
using Durandal.Common.Utils;
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

            // Parse each individual verse and note into structured form
            foreach (Match verseMatch in VerseParser.Matches(scriptureHtmlPage))
            {
                int verseNum = int.Parse(verseMatch.Groups[1].Value);
                string verseTextWithAnnotation = verseMatch.Groups[2].Value;
                //logger.Log(verseTextWithAnnotation);

                // Take the time to remove page breaks and other unused HTML tags here
                verseTextWithAnnotation = StringUtils.RegexRemove(PageBreakRemover, verseTextWithAnnotation);

                verses.Add(verseNum, new StructuredVerse()
                {
                    Canon = canon,
                    Book = book,
                    Chapter = chapter,
                    Verse = verseNum,
                    Text = verseTextWithAnnotation
                });
            }

            foreach (Match footnoteMatch in NoteParser.Matches(scriptureHtmlPage))
            {
                //logger.Log(footnoteMatch.Value);
                string noteId = footnoteMatch.Groups[1].Value;
                string footnoteTextWithAnnotation = footnoteMatch.Groups[2].Value;
                StructuredFootnote footnote = new StructuredFootnote()
                {
                    NoteId = noteId,
                    Text = footnoteTextWithAnnotation
                };

                //logger.Log(noteId);

                foreach (Match footnotScriptureRef in ScriptureRefParser.Matches(footnoteTextWithAnnotation))
                {
                    string refCanon = footnotScriptureRef.Groups[1].Value;
                    string refBook = footnotScriptureRef.Groups[2].Value;
                    if (!footnotScriptureRef.Groups[3].Success)
                    {
                        // It's a reference without chapter or verse info (usually TG or BD)
                        //logger.Log($"Adding reference without chapter to {refCanon} {refBook}");
                        footnote.ScriptureReferences.Add(new ScriptureReference()
                        {
                            Canon = refCanon,
                            Book = refBook,
                            Chapter = null,
                            Verse = null
                        });
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
                                    int rangeStart = int.Parse(verseRange.AsSpan(0, hyphenIndex));
                                    int rangeEnd = int.Parse(verseRange.AsSpan(hyphenIndex + 1));
                                    //logger.Log($"Adding reference to verse range {refCanon} {refBook} {refChapter}:{rangeStart}-{rangeEnd}");
                                    for (;rangeStart <= rangeEnd; rangeStart++)
                                    {
                                        footnote.ScriptureReferences.Add(new ScriptureReference()
                                        {
                                            Canon = refCanon,
                                            Book = refBook,
                                            Chapter = refChapter,
                                            Verse = rangeStart
                                        });
                                    }
                                }
                                else
                                {
                                    int singleVerse = int.Parse(verseRange);
                                    //logger.Log($"Adding reference with single verse {refCanon} {refBook} {refChapter}:{singleVerse}");
                                    footnote.ScriptureReferences.Add(new ScriptureReference()
                                    {
                                        Canon = refCanon,
                                        Book = refBook,
                                        Chapter = refChapter,
                                        Verse = singleVerse
                                    });
                                }
                            }
                        }
                        else
                        {

                            //logger.Log($"Adding reference with chapter but no verse to {refCanon} {refBook} {refChapter}");
                            footnote.ScriptureReferences.Add(new ScriptureReference()
                            {
                                Canon = refCanon,
                                Book = refBook,
                                Chapter = refChapter,
                                Verse = null
                            });
                        }
                    }
                }

                footnotes.Add(noteId, footnote);
            }

            // Now restructure each verse into a series of words with correlated footnotes
            List<SingleWordWithFootnotes> words = new List<SingleWordWithFootnotes>();
            StringBuilder stringBuffer = new StringBuilder();
            foreach (StructuredVerse verse in verses.Values)
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
                        string footnotedWords = taggedWordMatch.Groups[2].Value; // "cast out"
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
                                words.Add(new SingleWordWithFootnotes()
                                {
                                    Word = subWordMatch.Value,
                                    Footnote = footnoteForTheseWords
                                });
                            }
                        }

                        stringBuffer.Remove(0, taggedWordMatch.Length);
                    }
                    else
                    {
                        // It's just an untagged word
                        words.Add(new SingleWordWithFootnotes()
                        {
                            Word = wordMatch.Value,
                            Footnote = null
                        });

                        stringBuffer.Remove(0, wordMatch.Index + wordMatch.Length);
                    }
                }

                //logger.Log(string.Join(' ', words));
                words.Clear();
            }
        }

        private class SingleWordWithFootnotes
        {
            public string? Word;
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
            public string? Canon;
            public string? Book;
            public int Chapter;
            public int Verse;
            public string? Text;
        }

        private class StructuredFootnote
        {
            public string? NoteId;
            public string? Text;
            public List<ScriptureReference> ScriptureReferences { get; } = new List<ScriptureReference>();
        }

        private class ScriptureReference
        {
            public string? Canon;
            public string? Book;
            public int? Chapter;
            public int? Verse;
        }
    }
}
