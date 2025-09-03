using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Training.Extractors;
using System.Text;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training
{
    public static class ScriptureMetadata
    {
        #region Tables

        /// <summary>
        /// A generalized reading order for all books in the standard words, going from OT -> NT -> BoM -> DC -> PgP
        /// </summary>
        private static readonly string[] BOOKS_IN_ORDER = new string[]
            {
                // ot
                "gen","ex","lev","num","deut","josh","judg","ruth","1-sam","2-sam","1-kgs","2-kgs","1-chr","2-chr","ezra","neh","esth",
                "job","ps","prov","eccl","song","isa","jer","lam","ezek","dan","hosea","joel","amos","obad","jonah","micah","nahum","hab",
                "zeph","hag","zech","mal",
                // nt
                "matt","mark","luke","john","acts","rom","1-cor","2-cor","gal","eph","philip","col","1-thes","2-thes","1-tim","2-tim",
                "titus","philem","heb","james","1-pet","2-pet","1-jn","2-jn","3-jn","jude","rev",
                // bofm
                "1-ne","2-ne","jacob","enos","jarom","omni","w-of-m","mosiah","alma","hel","3-ne","4-ne","morm","ether","moro",
                // dc-testament
                "dc","od",
                // pgp
                "moses","abr","js-m","js-h","a-of-f"
            };

        /// <summary>
        /// Maps from book name to canon that it appears in
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> BOOK_TO_CANON = new Dictionary<string, string>()
        {
            // ot
            { "gen", "ot" },
            { "ex", "ot" },
            { "lev", "ot" },
            { "num", "ot" },
            { "deut", "ot" },
            { "josh", "ot" },
            { "judg", "ot" },
            { "ruth", "ot" },
            { "1-sam", "ot" },
            { "2-sam", "ot" },
            { "1-kgs", "ot" },
            { "2-kgs", "ot" },
            { "1-chr", "ot" },
            { "2-chr", "ot" },
            { "ezra", "ot" },
            { "neh", "ot" },
            { "esth", "ot" },
            { "job", "ot" },
            { "ps", "ot" },
            { "prov", "ot" },
            { "eccl", "ot" },
            { "song", "ot" },
            { "isa", "ot" },
            { "jer", "ot" },
            { "lam", "ot" },
            { "ezek", "ot" },
            { "dan", "ot" },
            { "hosea", "ot" },
            { "joel", "ot" },
            { "amos", "ot" },
            { "obad", "ot" },
            { "jonah", "ot" },
            { "micah", "ot" },
            { "nahum", "ot" },
            { "hab", "ot" },
            { "zeph", "ot" },
            { "hag", "ot" },
            { "zech", "ot" },
            { "mal", "ot" },
            // nt
            { "matt", "nt" },
            { "mark", "nt" },
            { "luke", "nt" },
            { "john", "nt" },
            { "acts", "nt" },
            { "rom", "nt" },
            { "1-cor", "nt" },
            { "2-cor", "nt" },
            { "gal", "nt" },
            { "eph", "nt" },
            { "philip", "nt" },
            { "col", "nt" },
            { "1-thes", "nt" },
            { "2-thes", "nt" },
            { "1-tim", "nt" },
            { "2-tim", "nt" },
            { "titus", "nt" },
            { "philem", "nt" },
            { "heb", "nt" },
            { "james", "nt" },
            { "1-pet", "nt" },
            { "2-pet", "nt" },
            { "1-jn", "nt" },
            { "2-jn", "nt" },
            { "3-jn", "nt" },
            { "jude", "nt" },
            { "rev", "nt" },
            { "1-ne", "bofm" },
            { "2-ne", "bofm" },
            { "jacob", "bofm" },
            { "enos", "bofm" },
            { "jarom", "bofm" },
            { "omni", "bofm" },
            { "w-of-m", "bofm" },
            { "mosiah", "bofm" },
            { "alma", "bofm" },
            { "hel", "bofm" },
            { "3-ne", "bofm" },
            { "4-ne", "bofm" },
            { "morm", "bofm" },
            { "ether", "bofm" },
            { "moro", "bofm" },
            // dc-testament
            { "dc", "dc-testament" },
            { "od", "dc-testament" },
            // pgp
            { "moses", "pgp" },
            { "abr", "pgp" },
            { "js-m", "pgp" },
            { "js-h", "pgp" },
            { "a-of-f", "pgp" },
        };

        /// <summary>
        /// Maps from book name to the number of chapters in that book
        /// </summary>
        private static readonly IReadOnlyDictionary<string, int> BOOK_CHAPTER_LENGTHS = new Dictionary<string, int>()
        {
            // ot
            { "gen", 50 },
            { "ex", 40 },
            { "lev", 27 },
            { "num", 36 },
            { "deut", 34 },
            { "josh", 24 },
            { "judg", 21 },
            { "ruth", 4 },
            { "1-sam", 31 },
            { "2-sam", 24 },
            { "1-kgs", 22 },
            { "2-kgs", 25 },
            { "1-chr", 29 },
            { "2-chr", 36 },
            { "ezra", 10 },
            { "neh", 13 },
            { "esth", 10 },
            { "job", 42 },
            { "ps", 150 },
            { "prov", 31 },
            { "eccl", 12 },
            { "song", 8 },
            { "isa", 66 },
            { "jer", 52 },
            { "lam", 5 },
            { "ezek", 48 },
            { "dan", 12 },
            { "hosea", 14 },
            { "joel", 3 },
            { "amos", 9 },
            { "obad", 1 },
            { "jonah", 4 },
            { "micah", 7 },
            { "nahum", 3 },
            { "hab", 3 },
            { "zeph", 3 },
            { "hag", 2 },
            { "zech", 14 },
            { "mal", 4 },
            // nt
            { "matt", 28 },
            { "mark", 16 },
            { "luke", 24 },
            { "john", 21 },
            { "acts", 28 },
            { "rom", 16 },
            { "1-cor", 16 },
            { "2-cor", 13 },
            { "gal", 6 },
            { "eph", 6 },
            { "philip", 4 },
            { "col", 4 },
            { "1-thes", 5 },
            { "2-thes", 3 },
            { "1-tim", 6 },
            { "2-tim", 4 },
            { "titus", 3 },
            { "philem", 1 },
            { "heb", 13 },
            { "james", 5 },
            { "1-pet", 5 },
            { "2-pet", 3 },
            { "1-jn", 5 },
            { "2-jn", 1 },
            { "3-jn", 1 },
            { "jude", 1 },
            { "rev", 22 },
            // bofm
            { "1-ne", 22 },
            { "2-ne", 33 },
            { "jacob", 7 },
            { "enos", 1 },
            { "jarom", 1 },
            { "omni", 1 },
            { "w-of-m", 1 },
            { "mosiah", 29 },
            { "alma", 63 },
            { "hel", 16 },
            { "3-ne", 30 },
            { "4-ne", 1 },
            { "morm", 9 },
            { "ether", 15 },
            { "moro", 10 },
            // dc-testament
            { "dc", 138 },
            { "od", 2 },
            // pgp
            { "moses", 8 },
            { "abr", 5 },
            { "js-m", 1 },
            { "js-h", 1 },
            { "a-of-f", 1 },
        };

        /// <summary>
        /// Maps from book name (internal) to the pretty display name in English
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> BOOK_TO_ENGLISH_NAME = new Dictionary<string, string>()
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

        #endregion

        internal static KnowledgeGraphNodeId? GetPrevChapter(string canon, string book, int chapter)
        {
            if (chapter > 1)
            {
                return FeatureToNodeMapping.ScriptureChapter(canon, book, chapter - 1);
            }
            else
            {
                int thisBookIndex = Array.IndexOf(BOOKS_IN_ORDER, book);
                if (thisBookIndex <= 0)
                {
                    return null;
                }

                string prevBookName = BOOKS_IN_ORDER[thisBookIndex - 1];
                string prevBookCanon = BOOK_TO_CANON[prevBookName];
                int prevBookChapter = BOOK_CHAPTER_LENGTHS[prevBookName];
                return FeatureToNodeMapping.ScriptureChapter(prevBookCanon, prevBookName, prevBookChapter);
            }
        }

        internal static KnowledgeGraphNodeId? GetNextChapter(string canon, string book, int chapter)
        {
            int thisBookLength = BOOK_CHAPTER_LENGTHS[book];
            if (chapter < thisBookLength)
            {
                return FeatureToNodeMapping.ScriptureChapter(canon, book, chapter + 1);
            }
            else
            {
                int thisBookIndex = Array.IndexOf(BOOKS_IN_ORDER, book);
                if (thisBookIndex >= BOOKS_IN_ORDER.Length - 1)
                {
                    return null;
                }

                string nextBookName = BOOKS_IN_ORDER[thisBookIndex + 1];
                string nextBookCanon = BOOK_TO_CANON[nextBookName];
                return FeatureToNodeMapping.ScriptureChapter(nextBookCanon, nextBookName, 1);
            }
        }

        public static string GetEnglishNameForBook(string bookId)
        {
            return BOOK_TO_ENGLISH_NAME[bookId];
        }

        public static string GetEnglishNameForCanon(string canon)
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

        public static int GetNumChaptesInBook(string bookId)
        {
            return BOOK_CHAPTER_LENGTHS[bookId];
        }

        private static readonly IReadOnlyDictionary<string, string> ENGLISH_BOOK_NAMES = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // ot
            { "genesis", "gen" },
            { "gen", "gen" },
            { "exodus", "ex" },
            { "exo", "ex" },
            { "leviticus", "lev" },
            { "lev", "lev" },
            { "numbers", "num" },
            { "num", "num" },
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
            { "1 thess", "1-thes" },
            { "1 thes", "1-thes" },
            { "1thes", "1-thes" },
            { "1st thessalonians", "1-thes" },
            { "1st thess", "1-thes" },
            { "1st thes", "1-thes" },
            { "2 thessalonians", "2-thes" },
            { "2 thess", "2-thes" },
            { "2 thes", "2-thes" },
            { "2thes", "2-thes" },
            { "2nd thessalonians", "2-thes" },
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
            { "w-of-m", "w-of-m" },
            { "mosiah", "mosiah" },
            { "msh", "mosiah" },
            { "alma", "alma" },
            { "alm", "alma" },
            { "helaman", "hel" },
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
            { "d & c", "dc" },
            { "d&c", "dc" },
            { "d and c", "dc" },
            { "official declarations", "od" },
            { "official declaration", "od" },
            { "declaration", "od" },
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
            { "aof", "a-of-f" },
            { "aoff", "a-of-f" },
            { "a-of-f", "a-of-f" },
        };

        // Baseline: ^\s*(1st nephi|1 ne|moroni|mormon|enos)(?:\s*(\d)\s*(?::\s*(\d))?)?$
        // group 1 : book name
        // group 2 (optional) : chapter
        // group 3 (optional) : verse
        private static readonly Regex EnglishScriptureRefMatcher;

        static ScriptureMetadata()
        {
            bool first = true;
            StringBuilder regexBuilder = new StringBuilder();
            regexBuilder.Append("^\\s*(");
            foreach (string englishBookName in ENGLISH_BOOK_NAMES.Keys)
            {
                if (!first)
                {
                    regexBuilder.Append('|');
                }

                regexBuilder.Append(Regex.Escape(englishBookName));
                regexBuilder.Append("\\.?");
                first = false;
            }

            regexBuilder.Append(")(?:\\s*(\\d+)\\s*(?::\\s*(\\d+))?)?$");
            EnglishScriptureRefMatcher = new Regex(regexBuilder.ToString(), RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Given a string like "2nd Peter 1:5", attempt to parse it into a specific scripture reference.
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static ScriptureReference? TryParseScriptureReferenceEnglish(string reference)
        {
            Match match = EnglishScriptureRefMatcher.Match(reference);
            string? bookId;
            if (match.Success && ENGLISH_BOOK_NAMES.TryGetValue(match.Groups[1].Value, out bookId))
            {
                string canon = BOOK_TO_CANON[bookId];
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

                return new ScriptureReference(canon, bookId, chapter, verse);
            }

            return null;
        }
    }
}
