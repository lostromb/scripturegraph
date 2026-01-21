using Durandal.Common.Logger;
using Durandal.Common.Utils;
using HtmlAgilityPack;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.XPath;

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
        /// Capture group 5: The "span" string, for very long ranges
        /// Capture group 6: The fragment, often the emphasis verse such as "p14"
        /// </summary>
        // \/study\/scriptures\/([^\/]+)\/([^\/]+)(?:\/(\d+?))?\?lang=\w+(?:&id=(.+?))?(?:&span=(.+?))?(?:#(.+?))?(?:$|\")
        private static readonly Regex ScriptureRefParser = new Regex("\\/study\\/scriptures\\/([^\\/]+)\\/([^\\/]+)(?:\\/(\\d+?))?\\?lang=\\w+(?:&id=(.+?))?(?:&span=(.+?))?(?:#(.+?))?(?:$|\\\")");

        // Input: https://www.churchofjesuschrist.org/scriptures/dc-testament/dc/128.22-23?lang=eng
        /// Capture group 1: canon (from URL, e.g. "bofm")
        /// Capture group 2: book (from URL, e.g. "alma") OR for TG or BD, this is the article name (e.g. "god-knowledge-about")
        /// Capture group 3: The chapter integer being referenced
        /// Capture group 4: Single verse, or start of range of verses (required)
        /// Capture group 5: End of range of verses (optional)
        /// \/scriptures\/([^\/]+)\/([^\/]+)\/(\d+)\.(\d+)(?:-(\d+))?
        private static readonly Regex ScriptureRefParserAlternate = new Regex("\\/scriptures\\/([^\\/]+)\\/([^\\/]+)\\/(\\d+)\\.(\\d+)(?:-(\\d+))?");

        // \/study\/general-conference\/(\d+?)\/(\d+?)\/(.+?)\?lang=\w+(?:&id=(.+?))?(?:#(.+?))?(?:$|\")
        private static readonly Regex ConferenceLinkParser = new Regex("\\/study\\/general-conference\\/(\\d+?)\\/(\\d+?)\\/(.+?)\\?lang=\\w+(?:&id=(.+?))?(?:#(.+?))?(?:$|\\\")");

        // Original: <p class=\"([^\"]+?)\".*?>(?:.+?\"verse-number\">(\d+)\s*<\/span>\s*)?(.+?)<\/p>
        // Group 1: paragraph's class
        // Group 2: verse number (if applicable)
        // Group 3: paragraph text (with potential inline html footnotes, etc.)
        private static readonly Regex ParagraphParser = new Regex("<p class=\\\"([^\\\"]+?)\\\".*?>(?:.+?\\\"verse-number\\\">(\\d+)\\s*<\\/span>\\s*)?(.+?)<\\/p>");

        private static readonly Regex PageBreakRemover = new Regex("<span class=\"page-break\".+?</span>");
        private static readonly Regex VerseNumSpanRemover = new Regex("<span class=\"verse-number\".+?</span>");

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

        private static readonly Regex LinkRemover = new Regex("<a.+?>([\\w\\W]+?)<\\/a>");

        private static readonly IReadOnlySet<string> CANONS = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ot", "nt", "bofm", "dc-testament", "pgp",
            "tg", "bd", "gs", "triple-index"
        };

        internal static string RemovePageBreakTags(string input)
        {
            return StringUtils.RegexRemove(PageBreakRemover, input);
        }

        internal static string RemoveLinksAndAnchorText(string input)
        {
            return StringUtils.RegexRemove(LinkRemover, input);
        }

        internal static string ReplaceBrWithNewlines(string input)
        {
            return StringUtils.RegexReplace(LineBreakMatcher, input, "\r\n");
        }

        internal static string RemoveVerseNumberSpans(string input)
        {
            return StringUtils.RegexRemove(VerseNumSpanRemover, input);
        }

        internal static string RemoveNbsp(string input)
        {
            return input.Replace('\u00a0', ' ');
        }

        internal static string StripAllButBoldAndItalics(string input)
        {
            // First, stash all tags we want to keep as [i], [/b]...
            input = MoreStringUtils.RegexGroupReplace(ItalicBoldMatcher, input, (groups) => $"[{groups[1].Value}]");

            // Remove all other HTML
            input = StringUtils.RegexRemove(HtmlTagRemover, input);

            // And restore the simplified i and b tags to regular <i>, <b>
            input = MoreStringUtils.RegexGroupReplace(StashedItalicBoldMatcher, input, (groups) => $"<{groups[1].Value}>");
            return input;
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
            foreach (var reference in ParseAllScriptureReferences(scriptureHtmlPage, logger))
            {
                destination.Add(reference);
            }
        }

        internal static void ParseAllScriptureReferences(
            string scriptureHtmlPage,
            ISet<ScriptureReference> destination,
            ILogger logger)
        {
            foreach (var reference in ParseAllScriptureReferences(scriptureHtmlPage, logger))
            {
                if (!destination.Contains(reference))
                {
                    destination.Add(reference);
                }
            }
        }

        internal static IEnumerable<ScriptureReference> ParseAllScriptureReferences(
            string scriptureHtmlPage,
            ILogger logger)
        {
            HashSet<ScriptureReference> dedup = new HashSet<ScriptureReference>();
            List<ScriptureReference> scratch = new List<ScriptureReference>();
            ScriptureReference toReturn;
            foreach (Match footnotScriptureRef in ScriptureRefParser.Matches(scriptureHtmlPage))
            {
                string refCanon = footnotScriptureRef.Groups[1].Value;
                string refBook = ScriptureMetadata.NormalizeBookId(footnotScriptureRef.Groups[2].Value, ref refCanon);
                if (!footnotScriptureRef.Groups[3].Success)
                {
                    if (!CANONS.Contains(refCanon))
                    {
                        continue;
                    }

                    // It's a reference without chapter or verse info (usually TG or BD)
                    //logger.Log($"Adding reference without chapter to {refCanon} {refBook}");
                    toReturn = new ScriptureReference(refCanon, refBook);
                    if (!dedup.Contains(toReturn))
                    {
                        dedup.Add(toReturn);
                        yield return toReturn;
                    }
                }
                else
                {
                    int refChapter = int.Parse(footnotScriptureRef.Groups[3].Value);

                    int? emphasisVerse = null;
                    int eParse;
                    if (footnotScriptureRef.Groups[6].Success && footnotScriptureRef.Groups[6].Value.StartsWith("p") &&
                        int.TryParse(footnotScriptureRef.Groups[6].Value.TrimStart('p'), out eParse))
                    {
                        emphasisVerse = eParse;
                    }

                    if (footnotScriptureRef.Groups[4].Success)
                    {
                        if (string.Equals("intro", footnotScriptureRef.Groups[4].Value))
                        {
                            // Commonly found on footnotes in D&C that refer to the "intro" of the chapter, which become a "paragraph" rather than verse reference
                            toReturn = new ScriptureReference(refCanon, refBook, refChapter, footnotScriptureRef.Groups[4].Value);
                            if (!dedup.Contains(toReturn))
                            {
                                dedup.Add(toReturn);
                                yield return toReturn;
                            }
                        }
                        else
                        {
                            // It defines a range somehow
                            scratch.Clear();
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

                                    scratch.Add(newRef);
                                });

                            foreach (var scripture in scratch)
                            {
                                // Filter out invalid verses (e.g. 3 Nephi 1:99)
                                if (scripture.Chapter.HasValue &&
                                    scripture.Verse.HasValue &&
                                    !ScriptureMetadata.IsValidVerse(scripture.Book, scripture.Chapter.Value, scripture.Verse.Value))
                                {
                                    continue;
                                }

                                if (!dedup.Contains(scripture))
                                {
                                    dedup.Add(scripture);
                                    yield return scripture;
                                }
                            }
                        }
                    }
                    else if (footnotScriptureRef.Groups[5].Success)
                    {
                        // It's a very long span across chapters.
                        // Group 5 in this case is like "12:37-13:13"
                        scratch.Clear();
                        ParseMultiChapterSpan(refBook, footnotScriptureRef.Groups[5].Value, logger,
                            (chapter, verse) =>
                            {
                                ScriptureReference newRef = new ScriptureReference(refCanon, refBook, chapter, verse);
                                newRef.LowEmphasis = emphasisVerse.HasValue && verse != emphasisVerse.Value;
                                scratch.Add(newRef);
                            });

                        foreach (var scripture in scratch)
                        {
                            if (scripture.Chapter.HasValue &&
                                scripture.Verse.HasValue &&
                                !ScriptureMetadata.IsValidVerse(scripture.Book, scripture.Chapter.Value, scripture.Verse.Value))
                            {
                                continue;
                            }

                            if (!dedup.Contains(scripture))
                            {
                                dedup.Add(scripture);
                                yield return scripture;
                            }
                        }
                    }
                    else
                    {
                        // Not sure if this is possible but whatever
                        toReturn = new ScriptureReference(refCanon, refBook, refChapter);
                        if (!dedup.Contains(toReturn) && ScriptureMetadata.IsValidChapter(refBook, refChapter))
                        {
                            //logger.Log($"Adding reference with chapter but no verse to {refCanon} {refBook} {refChapter}");
                            dedup.Add(toReturn);
                            yield return toReturn;
                        }
                    }
                }
            }

            // Handle alternate URL format
            foreach (Match footnotScriptureRef in ScriptureRefParserAlternate.Matches(scriptureHtmlPage))
            {
                string refCanon = footnotScriptureRef.Groups[1].Value;
                string refBook = ScriptureMetadata.NormalizeBookId(footnotScriptureRef.Groups[2].Value, ref refCanon);
                int refChapter = int.Parse(footnotScriptureRef.Groups[3].Value);
                int verseStart = int.Parse(footnotScriptureRef.Groups[4].Value);
                int? verseEnd = null;

                if (footnotScriptureRef.Groups[5].Success)
                {
                    verseEnd = int.Parse(footnotScriptureRef.Groups[5].Value);
                }
                else
                {
                    verseEnd = verseStart;
                }

                for (int verse = verseStart; verse <= verseEnd; verse++)
                {
                    toReturn = new ScriptureReference(refCanon, refBook, refChapter, verse);
                    if (toReturn.Chapter.HasValue &&
                        toReturn.Verse.HasValue &&
                        !ScriptureMetadata.IsValidVerse(toReturn.Book, toReturn.Chapter.Value, toReturn.Verse.Value))
                    {
                        continue;
                    }

                    if (!dedup.Contains(toReturn))
                    {
                        dedup.Add(toReturn);
                        yield return toReturn;
                    }
                }
            }

            // Also parse the plaintext and add any references there if we missed them
            string strippedHtml = WebUtility.HtmlDecode(scriptureHtmlPage);
            strippedHtml = StringUtils.RegexRemove(HtmlTagRemover, strippedHtml);
            foreach (ScriptureReference plaintextReference in ScriptureMetadataEnglish.ParseAllReferences(strippedHtml))
            {
                if (plaintextReference.Chapter.HasValue &&
                    plaintextReference.Verse.HasValue &&
                    !ScriptureMetadata.IsValidVerse(plaintextReference.Book, plaintextReference.Chapter.Value, plaintextReference.Verse.Value))
                {
                    continue;
                }

                if (!dedup.Contains(plaintextReference))
                {
                    dedup.Add(plaintextReference);
                    yield return plaintextReference;
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
                        // so the verse will be "study_intro1" or something.
                        // Just send the raw string and it should be parsed as a paragraph reference with no verse #
                        //logger.Log($"Invalid cross-reference location to {verseRange}, ignoring...", LogLevel.Wrn);
                        handler(verseRange);
                    }
                }
            }
        }

        internal static IEnumerable<string> ParseConfParagraphRangeString(string refVerseEncoded, ILogger logger)
        {
            // Parse the encoded verses.
            // Examples:
            // p5 (verse 5)
            // p1-p4 (verses 1-4)
            // p3,p6 (verses 3 and 6)
            // p21-p23,p27 (verses 21-23 and 27)
            //logger.Log($"Decoding verse ref {refVerseEncoded}");
            string[] segments = refVerseEncoded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string paraRange in segments)
            {
                int hyphenIndex = paraRange.IndexOf('-');
                if (hyphenIndex > 0)
                {
                    logger.Log($"Unsupported range of paragraphs {paraRange}, ignoring...", LogLevel.Wrn);
                }
                else
                {
                    yield return paraRange;
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

        private struct HtmlStackOp
        {
            public Action<HtmlStackOp> Operation;
            public int ClosingTagStreamPos;
            public int TagBeginCharIndex;
            public string LinkHref;

            public HtmlStackOp(Action<HtmlStackOp> operation, int streamPos, int tagBeginCharIndex, string linkHref)
            {
                Operation = operation;
                ClosingTagStreamPos = streamPos;
                TagBeginCharIndex = tagBeginCharIndex;
                LinkHref = linkHref;
            }
        }

        internal static HtmlFragmentParseModel ParseAndFormatHtmlFragmentNew(string htmlFragment, ILogger logger, bool insertLineBreaks = false)
        {
            using (PooledStringBuilder pooledSb = StringBuilderPool.Rent())
            {
                StringBuilder sb = pooledSb.Builder;
                List<Substring> links = new List<Substring>();
                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(htmlFragment);

                Stack<HtmlStackOp> closingTagStack = new Stack<HtmlStackOp>();
                HtmlNodeNavigator navigator = html.CreateNavigator() as HtmlNodeNavigator;
                var iter = navigator.Select("//node()");
                while (iter.MoveNext() && iter.Current is HtmlNodeNavigator currentNav)
                {
                    // Insert closing tags where needed
                    while (closingTagStack.Count > 0 && currentNav.CurrentNode.StreamPosition >= closingTagStack.Peek().ClosingTagStreamPos)
                    {
                        HtmlStackOp op = closingTagStack.Pop();
                        op.Operation(op);
                    }

                    if (currentNav.CurrentNode.NodeType == HtmlNodeType.Text)
                    {
                        sb.Append(RemoveNbsp(WebUtility.HtmlDecode(currentNav.CurrentNode.InnerText)));
                    }
                    else if (string.Equals("i", currentNav.CurrentNode.Name, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals("em", currentNav.CurrentNode.Name, StringComparison.OrdinalIgnoreCase) ||
                            (string.Equals("span", currentNav.CurrentNode.Name, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals("clarity-word", currentNav.CurrentNode.GetAttributeValue("class", string.Empty))))
                    {
                        sb.Append("<i>");
                        closingTagStack.Push(new HtmlStackOp((op) => sb.Append("</i>"), currentNav.CurrentNode.EndNode.StreamPosition, sb.Length, string.Empty));
                    }
                    else if (string.Equals("b", currentNav.CurrentNode.Name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals("strong", currentNav.CurrentNode.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append("<b>");
                        closingTagStack.Push(new HtmlStackOp((op) => sb.Append("</b>"), currentNav.CurrentNode.EndNode.StreamPosition, sb.Length, string.Empty));
                    }
                    else if (string.Equals("br", currentNav.CurrentNode.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (insertLineBreaks)
                        {
                            sb.Append("\r\n");
                        }
                    }
                    else if (string.Equals("a", currentNav.CurrentNode.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        string href = currentNav.CurrentNode.GetAttributeValue("href", string.Empty);
                        if (!string.IsNullOrEmpty(href))
                        {
                            href = WebUtility.HtmlDecode(href);
                            closingTagStack.Push(new HtmlStackOp((op) =>
                            {
                                links.Add(new Substring(op.LinkHref, new IntRange(op.TagBeginCharIndex, sb.Length), null));
                            }, currentNav.CurrentNode.EndNode.StreamPosition, sb.Length, href));
                        }
                        else
                        {
                            logger.Log($"Found anchor link with no href: {currentNav.CurrentNode.OuterHtml}", LogLevel.Wrn);
                        }
                    }

                    //logger.Log($"NAME \"{currentNav.CurrentNode.Name}\" TYPE \"{currentNav.CurrentNode.NodeType}\" START \"{currentNav.CurrentNode.StreamPosition}\" END \"{currentNav.CurrentNode.EndNode.StreamPosition}\" OUTER \"{currentNav.CurrentNode.OuterHtml}\" INNER \"{currentNav.CurrentNode.InnerHtml}\"");
                }

                while (closingTagStack.Count > 0)
                {
                    HtmlStackOp op = closingTagStack.Pop();
                    op.Operation(op);
                }

                return new HtmlFragmentParseModel()
                {
                    TextWithInlineFormatTags = sb.ToString(),
                    Links = links
                };
             }
        }

        public class HtmlFragmentParseModel
        {
            public required string TextWithInlineFormatTags;
            public required List<Substring> Links;
        }

        internal static IEnumerable<KnowledgeGraphNodeId> ParseAllConferenceTalkLinks(string html, ILogger logger)
        {
            foreach (Match parsedLink in ConferenceLinkParser.Matches(html))
            {
                int year = int.Parse(parsedLink.Groups[1].Value);
                int month = int.Parse(parsedLink.Groups[2].Value);
                ConferencePhase phase = month > 6 ? ConferencePhase.October : ConferencePhase.April;
                string talkId = parsedLink.Groups[3].Value;
                // this is for things like "p3-p7", but without the actual document, we can't make assumptions about what
                // paragraphs compose that range
                //if (parsedLink.Groups[4].Success)
                //{
                //    foreach (string para in ParseConfParagraphRangeString(parsedLink.Groups[4].Value, logger))
                //    {
                //        yield return FeatureToNodeMapping.ConferenceTalkParagraph(year, phase, talkId, para);
                //    }
                //}
                // So instead we only parse the "focus" paragraph extracted from url fragment #p5, if it exists
                if (parsedLink.Groups[5].Success)
                {
                    yield return FeatureToNodeMapping.ConferenceTalkParagraph(year, phase, talkId, parsedLink.Groups[5].Value);
                }
                else
                {
                    yield return FeatureToNodeMapping.ConferenceTalk(year, phase, talkId);
                }
            }
        }
    }
}
