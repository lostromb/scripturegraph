using Durandal.Common.Logger;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
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
        // <li[^>]+?id=\"(.+?)\".+?>\s*<p[\w\W]*?>([\w\W]+?)<\/p>
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
        // class=\"scripture-ref\"\s+href=\"\/study\/scriptures\/(.+?)\/(.+?)(?:\/(\d+?))?\?lang=eng(?:&id=(.+?))?(?:#.+?)?(?:&span=(.+?)(?:#.+?))?\"
        private static readonly Regex ScriptureRefParser = new Regex("class=\\\"scripture-ref\\\"\\s+href=\\\"\\/study\\/scriptures\\/(.+?)\\/(.+?)(?:\\/(\\d+?))?\\?lang=eng(?:&id=(.+?))?(#.+?)?(?:&span=(.+?)(#.+?)?)?\\\"");

        // Original: <p class=\"([^\"]+?)\".*?>(?:.+?\"verse-number\">(\d+)\s*<\/span>\s*)?(.+?)<\/p>
        // Group 1: paragraph's class
        // Group 2: verse number (if applicable)
        // Group 3: paragraph text (with potential inline html footnotes, etc.)
        private static readonly Regex ParagraphParser = new Regex("<p class=\\\"([^\\\"]+?)\\\".*?>(?:.+?\\\"verse-number\\\">(\\d+)\\s*<\\/span>\\s*)?(.+?)<\\/p>");

        private static readonly Regex PageBreakRemover = new Regex("<span class=\"page-break\".+?</span>");

        // Matches "entry" paragraphs on topical guide, index, and guide to scriptures
        public static readonly Regex IndexEntryParser = new Regex("<p class=\"entry\".+?>([\\w\\W]+?)<\\/p>");
        // Matches "title" paragraphs (usually "See also xxxx" headings) on topical guide, index, and guide to scriptures
        public static readonly Regex IndexTitleParser = new Regex("<p class=\"title\".+?>([\\w\\W]+?)<\\/p>");
        // Used to remove scripture reference URLs from text, or just remove their anchor text
        public static readonly Regex ScriptureRefReplacer = new Regex("(<a class=\\\"scripture-ref\\\".+?>)([\\w\\W]+?)<\\/a>");
        // Used to remove all HTML tags from text
        public static readonly Regex HtmlTagRemover = new Regex("<\\/?[a-z]+(?: [\\w\\W]+?)?>");

        private static readonly Regex ItalicBoldMatcher = new Regex("<(\\/?[ib])(?: [\\w\\W]+?)?>");

        private static readonly Regex StashedItalicBoldMatcher = new Regex("\\[(\\/?[ib])\\]");

        private static readonly Regex LineBreakMatcher = new Regex("<\\s*\\/?\\s*br\\s*\\/?\\s*>");

        internal static string RemovePageBreakTags(string input)
        {
            return StringUtils.RegexRemove(PageBreakRemover, input);
        }

        internal static string ReplaceBrWithNewlines(string input)
        {
            return StringUtils.RegexReplace(LineBreakMatcher, input, "\r\n");
        }

        internal static string RemoveNbsp(string input)
        {
            return input.Replace('\u00a0', ' ');
        }

        internal static string StripAllButBoldAndItalics(string input)
        {
            // First, stash all tags we want to keep as [i], [/b]...
            input = RegexGroupReplace(ItalicBoldMatcher, input, (groups) => $"[{groups[1]}]");

            // Remove all other HTML
            input = StringUtils.RegexRemove(HtmlTagRemover, input);

            // And restore the simplified i and b tags to regular <i>, <b>
            input = RegexGroupReplace(StashedItalicBoldMatcher, input, (groups) => $"<{groups[1]}>");
            return input;
        }

        public static string RegexGroupReplace(Regex expression, string input, Func<IReadOnlyList<string>, string> replacementDelegate, int maxReplacements = -1)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            List<string> groups = new List<string>();
            MatchCollection matchCollection = expression.Matches(input);
            string text = string.Empty;
            int num = 0;
            int num2 = 0;
            foreach (Match item in matchCollection)
            {
                groups.Clear();
                for (int group = 0; group < item.Groups.Count; group++)
                {
                    groups.Add(item.Groups[group].Value);
                }

                text += input.Substring(num, item.Index - num);
                num = item.Index + item.Length;
                text += replacementDelegate(groups);
                num2++;
                if (maxReplacements > 0 && num2 >= maxReplacements)
                {
                    break;
                }
            }

            return text + input.Substring(num);
        }

        internal static List<StructuredVerse> ParseVerses(string canon, string book, int chapter, string scriptureHtmlPage)
        {
            List<StructuredVerse> verses = new List<StructuredVerse>();

            // Parse each individual verse and note into structured form
            foreach (Match verseMatch in ParagraphParser.Matches(scriptureHtmlPage))
            {
                string paraClass = verseMatch.Groups[1].Value;
                if (!string.Equals(paraClass, "intro") ||
                    !string.Equals(paraClass, "verse") ||
                    !string.Equals(paraClass, "study-summary"))
                {
                    continue;
                }

                string paraId = verseMatch.Groups[2].Value;
                string verseTextWithAnnotation = verseMatch.Groups[2].Value;
                //logger.Log(verseTextWithAnnotation);

                // Take the time to remove page breaks and other unused HTML tags here
                verseTextWithAnnotation = StringUtils.RegexRemove(PageBreakRemover, verseTextWithAnnotation);

                verses.Add(new StructuredVerse(canon, book, chapter, paraId, paraClass, verseTextWithAnnotation));
            }

            return verses;
        }

        internal static KnowledgeGraphNodeId ConvertScriptureRefToNodeId(ScriptureReference scriptureRef)
        {
            if (string.Equals(scriptureRef.Canon, "tg", StringComparison.Ordinal))
            {
                // Topical guide topic
                return FeatureToNodeMapping.TopicalGuideKeyword(scriptureRef.Book);
            }
            else if (string.Equals(scriptureRef.Canon, "bd", StringComparison.Ordinal))
            {
                // Bible dictionary topic
                return FeatureToNodeMapping.BibleDictionaryTopic(scriptureRef.Book);
            }
            else if (string.Equals(scriptureRef.Canon, "gs", StringComparison.Ordinal))
            {
                // GS topic
                return FeatureToNodeMapping.GuideToScripturesTopic(scriptureRef.Book);
            }
            else
            {
                if (scriptureRef.Chapter.HasValue &&
                    scriptureRef.Verse.HasValue)
                {
                    // Regular scripture ref
                    return FeatureToNodeMapping.ScriptureVerse(
                        scriptureRef.Canon,
                        scriptureRef.Book,
                        scriptureRef.Chapter.Value,
                        scriptureRef.Verse.Value);
                }
                else if (scriptureRef.Chapter.HasValue)
                {
                    if (scriptureRef.Paragraph != null)
                    {
                        // Non-numerical paragraph
                        return FeatureToNodeMapping.ScriptureSupplementalParagraph(
                            scriptureRef.Canon,
                            scriptureRef.Book,
                            scriptureRef.Chapter.Value,
                            scriptureRef.Paragraph);
                    }
                    else
                    {
                        // Reference to an entire chapter
                        return FeatureToNodeMapping.ScriptureChapter(
                            scriptureRef.Canon,
                            scriptureRef.Book,
                            scriptureRef.Chapter.Value);
                    }
                }
                else
                {
                    // Reference to an entire book
                    return FeatureToNodeMapping.ScriptureBook(
                        scriptureRef.Canon,
                        scriptureRef.Book);
                }
            }
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

        // Parsing examples:
        // Single verse:
        // <a class="scripture-ref" href="/study/scriptures/dc-testament/dc/98?lang=eng&amp;id=p11#p11">D&amp;C 98:11</a>
        // A few verses:
        // <a class="scripture-ref" href="/study/scriptures/dc-testament/dc/128?lang=eng&amp;id=p15,p18#p15">D&amp;C 128:15, 18</a>
        // <a class="scripture-ref" href="/study/scriptures/dc-testament/dc/130?lang=eng&amp;id=p20-p21#p20">D&amp;C 130:20–21</a>
        // A chapter:
        // <a class="scripture-ref" href="/study/scriptures/ot/ex/18?lang=eng">18</a>
        // Multi-chapter spans (often seen in BD)
        // <a class="scripture-ref" href="/study/scriptures/ot/ex/12?lang=eng&span=12:37-13:16#p37">12:37–13:16</a>

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
                        if (string.Equals("intro", footnotScriptureRef.Groups[4].Value))
                        {
                            // Commonly found on footnotes in D&C that refer to the "intro" of the chapter, which become a "paragraph" rather than verse reference
                            destination.Add(new ScriptureReference(refCanon, refBook, refChapter, footnotScriptureRef.Groups[4].Value));
                        }
                        else
                        {
                            int? emphasisVerse = null;
                            if (footnotScriptureRef.Groups[5].Success)
                            {
                                emphasisVerse = int.Parse(footnotScriptureRef.Groups[5].Value.TrimStart('#').TrimStart('p'));
                            }

                            // It defines a range somehow
                            ParseVerseParagraphRangeString(footnotScriptureRef.Groups[4].Value, logger,
                                (referenceString) =>
                                {
                                    int verseNum;
                                    ScriptureReference newRef;
                                    if (int.TryParse(referenceString, out verseNum))
                                    {
                                        newRef = new ScriptureReference(refCanon, refBook, refChapter, verseNum);
                                        newRef.LowEmphasis = emphasisVerse.HasValue && verseNum != emphasisVerse.Value;
                                    }
                                    else
                                    {
                                        newRef = new ScriptureReference(refCanon, refBook, refChapter, referenceString);
                                    }

                                    destination.Add(newRef);
                                });
                        }
                    }
                    else if (footnotScriptureRef.Groups[6].Success)
                    {
                        int? emphasisVerse = null;
                        if (footnotScriptureRef.Groups[7].Success)
                        {
                            emphasisVerse = int.Parse(footnotScriptureRef.Groups[7].Value.TrimStart('#').TrimStart('p'));
                        }

                        // It's a very long span across chapters.
                        // Group 6 in this case is like "12:37-13:13"
                        ParseMultiChapterSpan(refBook, footnotScriptureRef.Groups[6].Value, logger,
                            (chapter, verse) =>
                            {
                                ScriptureReference newRef = new ScriptureReference(refCanon, refBook, chapter, verse);
                                newRef.LowEmphasis = emphasisVerse.HasValue && verse != emphasisVerse.Value;
                                destination.Add(newRef);
                            });
                    }
                    else
                    {
                        // Not sure if this is possible but whatever
                        //logger.Log($"Adding reference with chapter but no verse to {refCanon} {refBook} {refChapter}");
                        destination.Add(
                            new ScriptureReference(refCanon, refBook, refChapter));
                    }
                }
            }
        }

        internal static void ParseVerseParagraphRangeString(string refVerseEncoded, ILogger logger, Action<string> handler)
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
                            handler(rangeStart.ToString());
                        }
                    }
                    else
                    {
                        handler(verseRange.AsSpan(0, hyphenIndex).ToString());
                        handler(verseRange.AsSpan(hyphenIndex + 1).ToString());
                    }
                }
                else
                {
                    int singleVerse;
                    if (int.TryParse(verseRange, out singleVerse))
                    {
                        //logger.Log($"Adding reference with single verse {refCanon} {refBook} {refChapter}:{singleVerse}");
                        handler(singleVerse.ToString());
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

        private static readonly Regex MultiChapterRangeParser = new Regex("(\\d+):(\\d+)-(\\d+):(\\d+)");

        internal static void ParseMultiChapterSpan(string bookId, string refVerseEncoded, ILogger logger, Action<int, int> handler)
        {
            // Input is in the form of "12:47-13:12"
            // Need to know the name of the book we're in also so we can enumerate the correct chapter length
            Match regexMatch = MultiChapterRangeParser.Match(refVerseEncoded);
            if (!regexMatch.Success)
            {
                logger.Log($"Invalid multi-chapter span {refVerseEncoded}, ignoring...", LogLevel.Wrn);
                return;
            }

            int chapterStart = int.Parse(regexMatch.Groups[1].Value);
            int verseStart = int.Parse(regexMatch.Groups[2].Value);
            int chapterEnd = int.Parse(regexMatch.Groups[3].Value);
            int verseEnd = int.Parse(regexMatch.Groups[4].Value);

            while (chapterStart < chapterEnd)
            {
                int numVersesInThisChapter = ScriptureMetadata.GetNumVersesInChapter(bookId, chapterStart);
                while (verseStart <= numVersesInThisChapter)
                {
                    //logger.Log($"Adding reference to verse range {refCanon} {refBook} {refChapter}:{rangeStart}-{rangeEnd}");
                    handler(chapterStart, verseStart);
                    verseStart++;
                }

                chapterStart++;
                verseStart = 1;
            }

            while (verseStart <= verseEnd)
            {
                //logger.Log($"Adding reference to verse range {refCanon} {refBook} {refChapter}:{rangeStart}-{rangeEnd}");
                handler(chapterStart, verseStart);
                verseStart++;
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
                //logger.Log($"Parsed: {string.Join(' ', returnVal)}");

                parsedPlaintext = pooledSb.Builder.ToString();
                return returnVal;
            }
        }
    }
}
