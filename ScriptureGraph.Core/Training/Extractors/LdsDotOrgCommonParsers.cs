using Durandal.Common.Logger;
using Durandal.Common.Utils;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    internal static class LdsDotOrgCommonParsers
    {
        // Input:
        // <li data-marker="a" id="note16a" data-full-marker="16a">
        // <p data-aid="128344262" id="note16a_p1"><span data-note-category="tg"><a class="scripture-ref" href="/study/scriptures/tg/skill?lang=eng"><small>TG</small> Skill</a>.</span></p>
        /// <summary>
        /// Capture group 1: node ID (e.g. "note16a")
        /// Capture group 2: Footnote HTML span
        /// </summary>
        private static readonly Regex NoteParser = new Regex("<li[^>]+?id=\\\"(.+?)\\\".+?>\\s*<p[\\w\\W]*?>([\\w\\W]+?)<\\/p>");

        // Input:
        // <a class="study-note-ref" href="/study/scriptures/bofm/1-ne/17?lang=eng#note6a" data-scroll-id="note6a"><sup class="marker" data-value="a"></sup>cast out</a>
        /// <summary>
        /// Capture group 1: footnote ID (e.g. "note6a")
        /// Capture group 2: The word or words that are tagged with that footnote (e.g. "cast out")
        /// </summary>
        private static readonly Regex InlineFootnoteParser = new Regex("<a class=\"study-note-ref\".+?#(.+?)\".+?<\\/sup>(.+?)<\\/a>");

        // Input:
        // <p data-aid="128344255" id="note14b_p1"><span data-note-category="cross-ref"><a class="scripture-ref" href="/study/scriptures/dc-testament/dc/5?lang=eng&amp;id=p2#p2">D&amp;C 5:2</a>.</span></p>
        /// <summary>
        /// Capture group 1: canon (from URL, e.g. "bofm")
        /// Capture group 2: book (from URL, e.g. "alma") OR for TG or BD, this is the article name (e.g. "god-knowledge-about")
        /// Capture group 3: The chapter integer being referenced, or empty (for refs without chapter such as TG or BD)
        /// Capture group 4: The "paragraph" string, interpreted as the verse or verse range being referenced, in the specific HTML anchor format that must be parsed. Example: "p2", "p37-p38", "p11-p12,19"
        /// </summary>
        private static readonly Regex ScriptureRefParser = new Regex("class=\\\"scripture-ref\\\"\\s+href=\\\"\\/study\\/scriptures\\/(.+?)\\/(.+?)(?:\\/(\\d+?))?\\?lang=eng(?:&amp;id=(.+?)(?:#.+?))?\\\"");

        private static readonly Regex IntroParser = new Regex("<p class=\\\"intro\\\".+?>(.+?)<\\/p>");

        private static readonly Regex VerseParser = new Regex("<p class=\\\"verse\\\".+?verse-number\\\">(\\d+) <\\/span>(.+?)<\\/p>");

        private static readonly Regex PageBreakRemover = new Regex("<span class=\"page-break\".+?</span>");

        internal static Dictionary<int, StructuredVerse> ParseVerses(string canon, string book, int chapter, string scriptureHtmlPage)
        {
            Dictionary<int, StructuredVerse> verses = new Dictionary<int, StructuredVerse>();

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

            return verses;
        }

        internal static Dictionary<string, StructuredFootnote> ParseFootnotesFromPage(
            string scriptureHtmlPage,
            ILogger logger)
        {
            Dictionary<string, StructuredFootnote> footnotes = new Dictionary<string, StructuredFootnote>();
            foreach (Match footnoteMatch in NoteParser.Matches(scriptureHtmlPage))
            {
                //logger.Log(footnoteMatch.Value);
                string noteId = footnoteMatch.Groups[1].Value;
                string footnoteTextWithAnnotation = footnoteMatch.Groups[2].Value;
                StructuredFootnote footnote = new StructuredFootnote(noteId, footnoteTextWithAnnotation);

                //logger.Log(noteId);
                ParseAllScriptureReferences(footnoteTextWithAnnotation, footnote.ScriptureReferences, logger);
                footnotes.Add(noteId, footnote);
            }

            return footnotes;
        }

        internal static void ParseAllScriptureReferences(
            string scriptureHtmlPage,
            List<ScriptureReference> destination,
            ILogger logger)
        {
            foreach (Match footnotScriptureRef in ScriptureRefParser.Matches(scriptureHtmlPage))
            {
                string refCanon = footnotScriptureRef.Groups[1].Value;
                string refBook = footnotScriptureRef.Groups[2].Value;
                if (!footnotScriptureRef.Groups[3].Success)
                {
                    // It's a reference without chapter or verse info (usually TG or BD)
                    //logger.Log($"Adding reference without chapter to {refCanon} {refBook}");
                    destination.Add(new ScriptureReference(refCanon, refBook));
                }
                else
                {
                    int refChapter = int.Parse(footnotScriptureRef.Groups[3].Value);
                    if (footnotScriptureRef.Groups[4].Success)
                    {
                        ParseVerseParagraphRangeString(footnotScriptureRef.Groups[4].Value, logger,
                            (verseNum) =>
                            {
                                destination.Add(
                                    new ScriptureReference(refCanon, refBook, refChapter, verseNum));
                            });
                    }
                    else
                    {
                        // Not sure if this is possible but whatever
                        //logger.Log($"Adding reference with chapter but no verse to {refCanon} {refBook} {refChapter}");
                        destination.Add(
                            new ScriptureReference(refCanon, refBook, refChapter, null));
                    }
                }
            }
        }

        internal static void ParseVerseParagraphRangeString(string refVerseEncoded, ILogger logger, Action<int> handler)
        {
            refVerseEncoded = refVerseEncoded.Replace("p", "");
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
                            handler(rangeStart);
                        }
                    }
                    else
                    {
                        // In this system we only really track the intro paragraph
                        // (if it was written by the original text's author) and count it
                        // as verse 0. So we just assume any non-numerical verse reference
                        // is pointing to this header and label it as verse 0
                        handler(0);
                    }
                }
                else
                {
                    int singleVerse;
                    if (int.TryParse(verseRange, out singleVerse))
                    {
                        //logger.Log($"Adding reference with single verse {refCanon} {refBook} {refChapter}:{singleVerse}");
                        handler(singleVerse);
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

        internal static List<SingleWordWithFootnotes> ParseParagraphWithStudyNoteRef(
            string paragraphText,
            ILogger logger,
            Dictionary<string, StructuredFootnote> footnotes,
            out string parsedPlaintext)
        {
            using (PooledStringBuilder pooledSb = StringBuilderPool.Rent())
            {
                List<SingleWordWithFootnotes> returnVal = new List<SingleWordWithFootnotes>();

                int startIndex = 0;
                while (startIndex < paragraphText.Length)
                {
                    Match wordMatch = EnglishWordFeatureExtractor.WordMatcher.Match(paragraphText, startIndex);
                    if (wordMatch.Success)
                    {
                        Match taggedWordMatch = InlineFootnoteParser.Match(paragraphText, startIndex);
                        if (taggedWordMatch.Success && taggedWordMatch.Index < wordMatch.Index)
                        {
                            // It's one or more words tagged with a footnote
                            string footnoteNoteRef = taggedWordMatch.Groups[1].Value; // "note20d"
                            string footnotedWords = taggedWordMatch.Groups[2].Value; // "cast out"
                            int subWordIndex = 0;
                            StructuredFootnote? footnoteForTheseWords;
                            if (!footnotes.TryGetValue(footnoteNoteRef, out footnoteForTheseWords))
                            {
                                logger.Log($"Could not resolve footnote ref {footnoteNoteRef}", LogLevel.Err);
                            }
                            else
                            {
                                while (true)
                                {
                                    Match subWordMatch = EnglishWordFeatureExtractor.WordMatcher.Match(footnotedWords, subWordIndex);
                                    if (!subWordMatch.Success)
                                    {
                                        break;
                                    }

                                    subWordIndex = subWordMatch.Index + subWordMatch.Length;
                                    returnVal.Add(new SingleWordWithFootnotes(subWordMatch.Value, footnoteForTheseWords));
                                }
                            }

                            startIndex = taggedWordMatch.Index + taggedWordMatch.Length;
                            if (pooledSb.Builder.Length > 0)
                            {
                                pooledSb.Builder.Append(' ');
                            }

                            pooledSb.Builder.Append(footnotedWords);
                        }
                        else
                        {
                            // It's just an untagged word
                            returnVal.Add(new SingleWordWithFootnotes(wordMatch.Value));
                            startIndex = wordMatch.Index + wordMatch.Length;
                            if (pooledSb.Builder.Length > 0)
                            {
                                pooledSb.Builder.Append(' ');
                            }

                            pooledSb.Builder.Append(wordMatch.Value);
                        }
                    }
                    else
                    {
                        startIndex = paragraphText.Length;
                    }
                }

                // Dump the wordbreaker and tagger output for debug
                logger.Log($"Parsed: {string.Join(' ', returnVal)}");

                parsedPlaintext = pooledSb.Builder.ToString();
                return returnVal;
            }
        }
    }
}
