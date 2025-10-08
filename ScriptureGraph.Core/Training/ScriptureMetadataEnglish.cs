using Durandal.Common.Collections.Interning;
using Durandal.Common.Parsers;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Training.Extractors;
using System.Text;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training
{
    public static class ScriptureMetadataEnglish
    {
        // Baseline: ^\s*(1st nephi|1 ne|moroni|mormon|enos....)(?:\s*(\d)\s*(?:\:\s*(\d))?)?$
        // group 1 : book name
        // group 2 (optional) : chapter
        // group 3 (optional) : verse
        private static readonly Regex EnglishScriptureRefMatcher;

        // Baseline: (1st nephi|1 ne|moroni|mormon|enos....)
        private static readonly Regex EnglishScriptureBookNameMatcher;

        static ScriptureMetadataEnglish()
        {
            // we could use internalizers for super fast scripture parsing but meh....
            //IPrimitiveInternalizer<char> internalizer = InternalizerFactory.CreateInternalizer(new InternedKeySource<ReadOnlyMemory<char>>(),
            //    InternalizerFeature.CaseInsensitive | InternalizerFeature.OnlyMatchesWithinSet);

            bool first = true;
            StringBuilder singleRefRegexBuilder = new StringBuilder();
            StringBuilder bookNameRegexBuilder = new StringBuilder();
            singleRefRegexBuilder.Append("^\\s*(");
            bookNameRegexBuilder.Append("(");
            foreach (string englishBookName in NAME_TO_ID.Keys)
            {
                if (!first)
                {
                    singleRefRegexBuilder.Append('|');
                    bookNameRegexBuilder.Append('|');
                }

                singleRefRegexBuilder.Append(Regex.Escape(englishBookName));
                singleRefRegexBuilder.Append("\\.?");
                bookNameRegexBuilder.Append(Regex.Escape(englishBookName));
                bookNameRegexBuilder.Append("\\.?");
                first = false;
            }

            singleRefRegexBuilder.Append(")(?:\\s*(\\d+)\\s*(?:\\:\\s*(\\d+))?)?$");
            bookNameRegexBuilder.Append(")");
            EnglishScriptureRefMatcher = new Regex(singleRefRegexBuilder.ToString(), RegexOptions.IgnoreCase);
            EnglishScriptureBookNameMatcher = new Regex(bookNameRegexBuilder.ToString(), RegexOptions.IgnoreCase);
        }

        #region Tables

        /// <summary>
        /// Maps from book name (internal) to the pretty display name in English
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> ID_TO_NAME = new Dictionary<string, string>()
        {
            // ot
            { "gen", "Genesis" },
            { "ex", "Exodus" },
            { "lev", "Leviticus" },
            { "num", "Numbers" },
            { "deut", "Deuteronomy" },
            { "josh", "Joshua" },
            { "judg", "Judges" },
            { "ruth", "Ruth" },
            { "1-sam", "1 Samuel" },
            { "2-sam", "2 Samuel" },
            { "1-kgs", "1 Kings" },
            { "2-kgs", "2 Kings" },
            { "1-chr", "1 Chronicles" },
            { "2-chr", "2 Chronicles" },
            { "ezra", "Ezra" },
            { "neh", "Nehemiah" },
            { "esth", "Esther" },
            { "job", "Job" },
            { "ps", "Psalms" },
            { "prov", "Proverbs" },
            { "eccl", "Ecclesiastes" },
            { "song", "Song of Solomon" },
            { "isa", "Isaiah" },
            { "jer", "Jeremiah" },
            { "lam", "Lamentations" },
            { "ezek", "Ezekiel" },
            { "dan", "Daniel" },
            { "hosea", "Hosea" },
            { "joel", "Joel" },
            { "amos", "Amos" },
            { "obad", "Obadiah" },
            { "jonah", "Jonah" },
            { "micah", "Micah" },
            { "nahum", "Nahum" },
            { "hab", "Habakkuk" },
            { "zeph", "Zephaniah" },
            { "hag", "Haggai" },
            { "zech", "Zechariah" },
            { "mal", "Malachi" },
            // nt
            { "matt", "Matthew" },
            { "mark", "Mark" },
            { "luke", "Luke" },
            { "john", "John" },
            { "acts", "Acts" },
            { "rom", "Romans" },
            { "1-cor", "1 Corinthians" },
            { "2-cor", "2 Corinthians" },
            { "gal", "Galatians" },
            { "eph", "Ephesians" },
            { "philip", "Phillipians" },
            { "col", "Colossians" },
            { "1-thes", "1 Thessalonians" },
            { "2-thes", "2 Thessalonians" },
            { "1-tim", "1 Timothy" },
            { "2-tim", "2 Timothy" },
            { "titus", "Titus" },
            { "philem", "Philemon" },
            { "heb", "Hebrews" },
            { "james", "James" },
            { "1-pet", "1 Peter" },
            { "2-pet", "2 Peter" },
            { "1-jn", "1 John" },
            { "2-jn", "2 John" },
            { "3-jn", "3 John" },
            { "jude", "Jude" },
            { "rev", "Revelation" },
            // bom
            { "1-ne", "1 Nephi" },
            { "2-ne", "2 Nephi" },
            { "jacob", "Jacob" },
            { "enos", "Enos" },
            { "jarom", "Jarom" },
            { "omni", "Omni" },
            { "w-of-m", "Words of Mormon" },
            { "mosiah", "Mosiah" },
            { "alma", "Alma" },
            { "hel", "Helaman" },
            { "3-ne", "3 Nephi" },
            { "4-ne", "4 Nephi" },
            { "morm", "Mormon" },
            { "ether", "Ether" },
            { "moro", "Moroni" },
            // dc-testament
            { "dc", "Doctrine & Covenants" },
            { "od", "Official Declarations" },
            // pgp
            { "moses", "Moses" },
            { "abr", "Abraham" },
            { "js-m", "Joseph Smith - Matthew" },
            { "js-h", "Joseph Smith - History" },
            { "a-of-f", "Articles of Faith" },
        };

        private static readonly IReadOnlyDictionary<string, string> NAME_TO_ID = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // ot
            { "genesis", "gen" },
            { "gen", "gen" },
            { "exodus", "ex" },
            { "exod", "ex" },
            { "exo", "ex" },
            { "leviticus", "lev" },
            { "levit", "lev" },
            { "lev", "lev" },
            { "numbers", "num" },
            { "num", "num" },
            { "nmb", "num" },
            { "numb", "num" },
            { "deuteronomy", "deut" },
            { "deu", "deut" },
            { "deut", "deut" },
            { "deuter", "deut" },
            { "joshua", "josh" },
            { "josh", "josh" },
            { "jos", "josh" },
            { "judges", "judg" },
            { "jdg", "judg" },
            { "judg", "judg" },
            { "ruth", "ruth" },
            { "rth", "ruth" },
            { "1 samuel", "1-sam" },
            { "1st samuel", "1-sam" },
            { "1 sam", "1-sam" },
            { "1sam", "1-sam" },
            { "1st sam", "1-sam" },
            { "2 samuel", "2-sam" },
            { "2nd samuel", "2-sam" },
            { "2 sam", "2-sam" },
            { "2sam", "2-sam" },
            { "2nd sam", "2-sam" },
            { "1 kings", "1-kgs" },
            { "1 kng", "1-kgs" },
            { "1kng", "1-kgs" },
            { "1 kgs", "1-kgs" },
            { "1kgs", "1-kgs" },
            { "1st kings", "1-kgs" },
            { "1st kng", "1-kgs" },
            { "1st kgs", "1-kgs" },
            { "2 kings", "2-kgs" },
            { "2 kng", "2-kgs" },
            { "2kng", "2-kgs" },
            { "2 kgs", "2-kgs" },
            { "2kgs", "2-kgs" },
            { "2nd kings", "2-kgs" },
            { "2nd kng", "2-kgs" },
            { "2nd kgs", "2-kgs" },
            { "1 chronicles", "1-chr" },
            { "1 chron", "1-chr" },
            { "1 chr", "1-chr" },
            { "1chr", "1-chr" },
            { "1st chronicles", "1-chr" },
            { "1st chron", "1-chr" },
            { "1st chr", "1-chr" },
            { "2 chronicles", "2-chr" },
            { "2 chron", "2-chr" },
            { "2 chr", "2-chr" },
            { "2chr", "2-chr" },
            { "2nd chronicles", "2-chr" },
            { "2nd chron", "2-chr" },
            { "2nd chr", "2-chr" },
            { "ezra", "ezra" },
            { "ezr", "ezra" },
            { "nehemiah", "neh" },
            { "neh", "neh" },
            { "esther", "esth" },
            { "est", "esth" },
            { "esth", "esth" },
            { "job", "job" },
            { "psalms", "ps" },
            { "psa", "ps" },
            { "ps", "ps" },
            { "proverbs", "prov" },
            { "prov", "prov" },
            { "ecclesiastes", "eccl" },
            { "eccl", "eccl" },
            { "ecc", "eccl" },
            { "song of solomon", "song" },
            { "song", "song" },
            { "sos", "song" },
            { "isaiah", "isa" },
            { "isa", "isa" },
            { "ish", "isa" },
            { "jeremiah", "jer" },
            { "jerem", "jer" },
            { "jer", "jer" },
            { "lamentations", "lam" },
            { "lament", "lam" },
            { "lam", "lam" },
            { "ezekiel", "ezek" },
            { "eze", "ezek" },
            { "ezek", "ezek" },
            { "ezk", "ezek" },
            { "ekl", "ezek" },
            { "daniel", "dan" },
            { "dan", "dan" },
            { "hosea", "hosea" },
            { "hos", "hosea" },
            { "joel", "joel" },
            { "jol", "joel" },
            { "amos", "amos" },
            { "obadiah", "obad" },
            { "obad", "obad" },
            { "obd", "obad" },
            { "jonah", "jonah" },
            { "jon", "jonah" },
            { "jna", "jonah" },
            { "jnh", "jonah" },
            { "micah", "micah" },
            { "mic", "micah" },
            { "mch", "micah" },
            { "nahum", "nahum" },
            { "nah", "nahum" },
            { "nhm", "nahum" },
            { "habakkuk", "hab" },
            { "hab", "hab" },
            { "zephaniah", "zeph" },
            { "zeph", "zeph" },
            { "zph", "zeph" },
            { "haggai", "hag" },
            { "hag", "hag" },
            { "hgi", "hag" },
            { "zechariah", "zech" },
            { "zech", "zech" },
            { "zch", "zech" },
            { "malachi", "mal" },
            { "mal", "mal" },
            { "mci", "mal" },
            // nt
            { "matthew", "matt" },
            { "matt", "matt" },
            { "mark", "mark" },
            { "luke", "luke" },
            { "john", "john" },
            { "acts", "acts" },
            { "romans", "rom" },
            { "rom", "rom" },
            { "1 corinthians", "1-cor" },
            { "1 cor", "1-cor" },
            { "1cor", "1-cor" },
            { "1st corinthians", "1-cor" },
            { "1st cor", "1-cor" },
            { "2 corinthians", "2-cor" },
            { "2 cor", "2-cor" },
            { "2cor", "2-cor" },
            { "2nd corinthians", "2-cor" },
            { "2nd cor", "2-cor" },
            { "galatians", "gal" },
            { "gal", "gal" },
            { "ephesians", "eph" },
            { "eph", "eph" },
            { "phillipians", "philip" },
            { "phil", "philip" },
            { "phi", "philip" },
            { "colossians", "col" },
            { "col", "col" },
            { "1 thessalonians", "1-thes" },
            { "1 thesselonians", "1-thes" },
            { "1 thess", "1-thes" },
            { "1 thes", "1-thes" },
            { "1thes", "1-thes" },
            { "1st thessalonians", "1-thes" },
            { "1st thesselonians", "1-thes" },
            { "1st thess", "1-thes" },
            { "1st thes", "1-thes" },
            { "2 thessalonians", "2-thes" },
            { "2 thesselonians", "2-thes" },
            { "2 thess", "2-thes" },
            { "2 thes", "2-thes" },
            { "2thes", "2-thes" },
            { "2nd thessalonians", "2-thes" },
            { "2nd thesselonians", "2-thes" },
            { "2nd thess", "2-thes" },
            { "2nd thes", "2-thes" },
            { "1 timothy", "1-tim" },
            { "1st timothy", "1-tim" },
            { "1 tim", "1-tim" },
            { "1tim", "1-tim" },
            { "1st tim", "1-tim" },
            { "2 timothy", "2-tim" },
            { "2nd timothy", "2-tim" },
            { "2 tim", "2-tim" },
            { "2tim", "2-tim" },
            { "2nd tim", "2-tim" },
            { "titus", "titus" },
            { "tit", "titus" },
            { "philemon", "philem" },
            { "plm", "philem" }, // TODO not sure what the actual abbreviation on the tabs of a quad, need to not interfere with "phillipians"
            { "hebrews", "heb" },
            { "heb", "heb" },
            { "james", "james" },
            { "jas", "james" },
            { "jms", "james" },
            { "1 peter", "1-pet" },
            { "1peter", "1-pet" },
            { "1 pet", "1-pet" },
            { "1pet", "1-pet" },
            { "1st peter", "1-pet" },
            { "1st pet", "1-pet" },
            { "2 peter", "2-pet" },
            { "2peter", "2-pet" },
            { "2 pet", "2-pet" },
            { "2pet", "2-pet" },
            { "2nd peter", "2-pet" },
            { "2nd pet", "2-pet" },
            { "1 john", "1-jn" },
            { "1john", "1-jn" },
            { "1 jhn", "1-jn" },
            { "1jhn", "1-jn" },
            { "1st john", "1-jn" },
            { "1st jhn", "1-jn" },
            { "2 john", "2-jn" },
            { "2john", "2-jn" },
            { "2 jhn", "2-jn" },
            { "2jhn", "2-jn" },
            { "2nd john", "2-jn" },
            { "2nd jhn", "2-jn" },
            { "3 john", "3-jn" },
            { "3john", "3-jn" },
            { "3 jhn", "3-jn" },
            { "3jhn", "3-jn" },
            { "3rd john", "3-jn" },
            { "3rd jhn", "3-jn" },
            { "jude", "jude" },
            { "jud", "jude" },
            { "revelation", "rev" },
            { "rev", "rev" },
            // bom
            { "1 nephi", "1-ne" },
            { "1nephi", "1-ne" },
            { "1 ne", "1-ne" },
            { "1ne", "1-ne" },
            { "1st nephi", "1-ne" },
            { "1st ne", "1-ne" },
            { "2 nephi", "2-ne" },
            { "2nephi", "2-ne" },
            { "2 ne", "2-ne" },
            { "2ne", "2-ne" },
            { "2nd nephi", "2-ne" },
            { "2nd ne", "2-ne" },
            { "jacob", "jacob" },
            { "jac", "jacob" },
            { "enos", "enos" },
            { "ens", "enos" },
            { "jarom", "jarom" },
            { "jrm", "jarom" },
            { "omni", "omni" },
            { "omn", "omni" },
            { "words of mormon", "w-of-m" },
            { "w. of mormon", "w-of-m" },
            { "w of mormon", "w-of-m" },
            { "w-of-m", "w-of-m" },
            { "wmn", "w-of-m" },
            { "mosiah", "mosiah" },
            { "msh", "mosiah" },
            { "alma", "alma" },
            { "alm", "alma" },
            { "helaman", "hel" },
            { "hela", "hel" },
            { "hel", "hel" },
            { "3 nephi", "3-ne" },
            { "3nephi", "3-ne" },
            { "3 ne", "3-ne" },
            { "3ne", "3-ne" },
            { "3rd nephi", "3-ne" },
            { "3rd ne", "3-ne" },
            { "4 nephi", "4-ne" },
            { "4nephi", "4-ne" },
            { "4 ne", "4-ne" },
            { "4ne", "4-ne" },
            { "4th nephi", "4-ne" },
            { "4th ne", "4-ne" },
            { "mormon", "morm" },
            { "morm", "morm" },
            { "mmn", "morm" },
            { "ether", "ether" },
            { "eth", "ether" },
            { "moroni", "moro" },
            { "morn", "moro" },
            { "mni", "moro" },
            // dc-testament
            { "doctrine & covenants", "dc" },
            { "doc. & cov.", "dc" },
            { "doc & cov.", "dc" },
            { "doct & cov.", "dc" },
            { "doct. & cov.", "dc" },
            { "doctrine and covenants", "dc" },
            { "d & c", "dc" },
            { "d &c", "dc" },
            { "d& c", "dc" },
            { "d&c", "dc" },
            { "d.&c.", "dc" },
            { "d.& c.", "dc" },
            { "d. &c.", "dc" },
            { "d. & c.", "dc" },
            { "d. and c", "dc" },
            { "d and c", "dc" },
            { "official declarations", "od" },
            { "official declaration", "od" },
            { "declarations", "od" },
            { "declaration", "od" },
            { "o. d.", "od" },
            { "o.d.", "od" },
            // pgp
            { "moses", "moses" },
            { "mos", "moses" },
            { "abraham", "abr" },
            { "abr", "abr" },
            { "joseph smith - matthew", "js-m" },
            { "joseph smith matthew", "js-m" },
            { "js matthew", "js-m" },
            { "js-m", "js-m" },
            { "joseph smith - history", "js-h" },
            { "joseph smith history", "js-h" },
            { "js history", "js-h" },
            { "js-h", "js-h" },
            { "articles of faith", "a-of-f" },
            { "article of faith", "a-of-f" },
            { "a. of faith", "a-of-f" },
            { "a. faith", "a-of-f" },
            { "aof", "a-of-f" },
            { "aoff", "a-of-f" },
            { "a-of-f", "a-of-f" },
        };

        #endregion

        public static string GetNameForBook(string bookId)
        {
            return ID_TO_NAME[bookId];
        }

        public static string GetNameForCanon(string canon)
        {
            if (string.Equals(canon, "ot", StringComparison.Ordinal))
            {
                return "Old Testament";
            }
            else if (string.Equals(canon, "nt", StringComparison.Ordinal))
            {
                return "New Testament";
            }
            else if (string.Equals(canon, "bofm", StringComparison.Ordinal))
            {
                return "Book of Mormon";
            }
            else if (string.Equals(canon, "dc-testament", StringComparison.Ordinal))
            {
                return "D & C";
            }
            else if (string.Equals(canon, "pgp", StringComparison.Ordinal))
            {
                return "Pearl of Great Price";
            }
            else if (string.Equals(canon, "bd", StringComparison.Ordinal))
            {
                return "Bible Dictionary";
            }
            else if (string.Equals(canon, "gs", StringComparison.Ordinal))
            {
                return "Guide to the Scriptures";
            }
            else if (string.Equals(canon, "triple-index", StringComparison.Ordinal))
            {
                return "Index";
            }
            else
            {
                throw new FormatException("Unknown canon " + canon);
            }
        }

        /// <summary>
        /// Given a string like "2nd Peter 1:5", attempt to parse it into a specific scripture reference.
        /// Only applies to single verse forms! Doesn't correctly handle things like "Matt 3:1-5" or "John 3:16, 17"
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static ScriptureReference? TryParseScriptureReference(string reference)
        {
            Match match = EnglishScriptureRefMatcher.Match(reference.Replace(".", string.Empty));
            string? bookId;
            if (match.Success && NAME_TO_ID.TryGetValue(match.Groups[1].Value, out bookId))
            {
                string canon = ScriptureMetadata.GetCanonForBook(bookId);
                int? chapter = null;
                if (match.Groups[2].Success)
                {
                    chapter = int.Parse(match.Groups[2].Value);

                    if (chapter <= 0 || chapter > 200)
                    {
                        return null;
                    }
                }

                int? verse = null;
                if (match.Groups[3].Success)
                {
                    verse = int.Parse(match.Groups[3].Value);

                    if (verse <= 0 || verse > 200)
                    {
                        return null;
                    }
                }

                // If the chapter matches but not the verse on a book with only one chapter, correct that
                // (to handle edge cases like "enos 5" referring to the verse of the single chapter)
                if (ScriptureMetadata.GetNumChaptersInBook(bookId) == 1 && !verse.HasValue)
                {
                    return new ScriptureReference(canon, bookId, 1, chapter);
                }

                return new ScriptureReference(canon, bookId, chapter, verse);
            }

            return null;
        }

        public static IEnumerable<ScriptureReference> ParseAllReferences(string inputText, bool includeExtra = false)
        {
            // Start by parsing all book names, and divide those into spans of "(book name) (whatever...) (verse references or ranges within this book)"
            // So the separated spans might be "Moro. 7:1-5", or "D & C 76:11, see also 22" or "John 17:3-5; see also verses 24, 25"
            int lastIndex = -1;
            int lastLength = -1;
            foreach (Match m in EnglishScriptureBookNameMatcher.Matches(inputText))
            {
                if (lastIndex >= 0)
                {
                    foreach (ScriptureReference r in ParseSingleReference(
                        inputText.Substring(lastIndex, lastLength),
                        inputText.Substring(lastIndex, m.Index - lastIndex)))
                    {
                        yield return r;
                    }
                }

                lastIndex = m.Index;
                lastLength = m.Length;
            }

            if (lastIndex >= 0)
            {
                foreach (ScriptureReference r in ParseSingleReference(
                        inputText.Substring(lastIndex, lastLength),
                        inputText.Substring(lastIndex)))
                {
                    yield return r;
                }
            }
        }

        // orig. (?:(\d+)\s*\:|(\d+)(?:\s*[-–—‒]\s*(\d+))?)
        // all groups optional
        // group 1: chapter
        // group 2: verse range start
        // group 3: verse range end
        private static readonly Regex RobustVerseNumParser = new Regex("(?:(\\d+)\\s*\\:|(\\d+)(?:\\s*[-–—‒]\\s*(\\d+))?)");

        private static IEnumerable<ScriptureReference> ParseSingleReference(string book, string entireThing)
        {
            string bookId = NAME_TO_ID[book.Replace(".", string.Empty)];
            string canonId = ScriptureMetadata.GetCanonForBook(bookId);

            int? chapter = null;
            foreach (Match m in RobustVerseNumParser.Matches(entireThing))
            {
                if (m.Groups[1].Success)
                {
                    // Update currently contextual chapter
                    chapter = int.Parse(m.Groups[1].Value);
                }
                else if (chapter.HasValue)
                {
                    if (m.Groups[2].Success && m.Groups[3].Success)
                    {
                        // It's a range
                        int rangeStart = int.Parse(m.Groups[2].Value);
                        int rangeEnd = int.Parse(m.Groups[3].Value);
                        while (rangeStart <= rangeEnd)
                        {
                            yield return new ScriptureReference(canonId, bookId, chapter, rangeStart);
                            rangeStart++;
                        }
                    }
                    else if (m.Groups[2].Success)
                    {
                        // It's just a single verse
                        int verse = int.Parse(m.Groups[2].Value);
                        yield return new ScriptureReference(canonId, bookId, chapter, verse);
                    }
                }
            }
        }
    }
}
