using Durandal.Common.Collections.Interning;
using Durandal.Common.NLP.Language;
using Durandal.Common.Parsers;
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
        /// The length in verses of each chapter of every canonical scripture
        /// </summary>
        private static readonly IReadOnlyDictionary<string, int[]> BOOK_CHAPTER_LENGTHS = new Dictionary<string, int[]>()
        {
            {"gen",new int[] { 31,25,24,26,32,22,24,22,29,32,32,20,18,24,21,16,27,33,38,18,34,24,20,67,34,35,46,22,35,43,55,32,20,31,29,43,36,30,23,23,57,38,34,34,28,34,31,22,33,26 }},
            {"ex",new int[] { 22,25,22,31,23,30,25,32,35,29,10,51,22,31,27,36,16,27,25,26,36,31,33,18,40,37,21,43,46,38,18,35,23,35,35,38,29,31,43,38 }},
            {"lev",new int[] { 17,16,17,35,19,30,38,36,24,20,47,8,59,57,33,34,16,30,37,27,24,33,44,23,55,46,34 }},
            {"num",new int[] { 54,34,51,49,31,27,89,26,23,36,35,16,33,45,41,50,13,32,22,29,35,41,30,25,18,65,23,31,40,16,54,42,56,29,34,13 }},
            {"deut",new int[] { 46,37,29,49,33,25,26,20,29,22,32,32,18,29,23,22,20,22,21,20,23,30,25,22,19,19,26,68,29,20,30,52,29,12 }},
            {"josh",new int[] { 18,24,17,24,15,27,26,35,27,43,23,24,33,15,63,10,18,28,51,9,45,34,16,33 }},
            {"judg",new int[] { 36,23,31,24,31,40,25,35,57,18,40,15,25,20,20,31,13,31,30,48,25 }},
            {"ruth",new int[] { 22,23,18,22 }},
            {"1-sam",new int[] { 28,36,21,22,12,21,17,22,27,27,15,25,23,52,35,23,58,30,24,42,15,23,29,22,44,25,12,25,11,31,13 }},
            {"2-sam",new int[] { 27,32,39,12,25,23,29,18,13,19,27,31,39,33,37,23,29,33,43,26,22,51,39,25 }},
            {"1-kgs",new int[] { 53,46,28,34,18,38,51,66,28,29,43,33,34,31,34,34,24,46,21,43,29,53 }},
            {"2-kgs",new int[] { 18,25,27,44,27,33,20,29,37,36,21,21,25,29,38,20,41,37,37,21,26,20,37,20,30 }},
            {"1-chr",new int[] { 54,55,24,43,26,81,40,40,44,14,47,40,14,17,29,43,27,17,19,8,30,19,32,31,31,32,34,21,30 }},
            {"2-chr",new int[] { 17,18,17,22,14,42,22,18,31,19,23,16,22,15,19,14,19,34,11,37,20,12,21,27,28,23,9,27,36,27,21,33,25,33,27,23 }},
            {"ezra",new int[] { 11,70,13,24,17,22,28,36,15,44 }},
            {"neh",new int[] { 11,20,32,23,19,19,73,18,38,39,36,47,31 }},
            {"esth",new int[] { 22,23,15,17,14,14,10,17,32,3 }},
            {"job",new int[] { 22,13,26,21,27,30,21,22,35,22,20,25,28,22,35,22,16,21,29,29,34,30,17,25,6,14,23,28,25,31,40,22,33,37,16,33,24,41,30,24,34,17 }},
            {"ps",new int[] { 6,12,8,8,12,10,17,9,20,18,7,8,6,7,5,11,15,50,14,9,13,31,6,10,22,12,14,9,11,12,24,11,22,22,28,12,40,22,13,17,13,11,5,26,17,11,9,14,20,23,19,9,6,7,23,13,11,11,17,12,8,12,11,10,13,20,7,35,36,5,24,20,28,23,10,12,20,72,13,19,16,8,18,12,13,17,7,18,52,17,16,15,5,23,11,13,12,9,9,5,8,28,22,35,45,48,43,13,31,7,10,10,9,8,18,19,2,29,176,7,8,9,4,8,5,6,5,6,8,8,3,18,3,3,21,26,9,8,24,13,10,7,12,15,21,10,20,14,9,6 }},
            {"prov",new int[] { 33,22,35,27,23,35,27,36,18,32,31,28,25,35,33,33,28,24,29,30,31,29,35,34,28,28,27,28,27,33,31 }},
            {"eccl",new int[] { 18,26,22,16,20,12,29,17,18,20,10,14 }},
            {"song",new int[] { 17,17,11,16,16,13,13,14 }},
            {"isa",new int[] { 31,22,26,6,30,13,25,22,21,34,16,6,22,32,9,14,14,7,25,6,17,25,18,23,12,21,13,29,24,33,9,20,24,17,10,22,38,22,8,31,29,25,28,28,25,13,15,22,26,11,23,15,12,17,13,12,21,14,21,22,11,12,19,12,25,24 }},
            {"jer",new int[] { 19,37,25,31,31,30,34,22,26,25,23,17,27,22,21,21,27,23,15,18,14,30,40,10,38,24,22,17,32,24,40,44,26,22,19,32,21,28,18,16,18,22,13,30,5,28,7,47,39,46,64,34 }},
            {"lam",new int[] { 22,22,66,22,22 }},
            {"ezek",new int[] { 28,10,27,17,17,14,27,18,11,22,25,28,23,23,8,63,24,32,14,49,32,31,49,27,17,21,36,26,21,26,18,32,33,31,15,38,28,23,29,49,26,20,27,31,25,24,23,35 }},
            {"dan",new int[] { 21,49,30,37,31,28,28,27,27,21,45,13 }},
            {"hosea",new int[] { 11,23,5,19,15,11,16,14,17,15,12,14,16,9 }},
            {"joel",new int[] { 20,32,21 }},
            {"amos",new int[] { 15,16,15,13,27,14,17,14,15 }},
            {"obad",new int[] { 21 }},
            {"jonah",new int[] { 17,10,10,11 }},
            {"micah",new int[] { 16,13,12,13,15,16,20 }},
            {"nahum",new int[] { 15,13,19 }},
            {"hab",new int[] { 17,20,19 }},
            {"zeph",new int[] { 18,15,20 }},
            {"hag",new int[] { 15,23 }},
            {"zech",new int[] { 21,13,10,14,11,15,14,23,17,12,17,14,9,21 }},
            {"mal",new int[] { 14,17,18,6 }},
            {"matt",new int[] { 25,23,17,25,48,34,29,34,38,42,30,50,58,36,39,28,27,35,30,34,46,46,39,51,46,75,66,20 }},
            {"mark",new int[] { 45,28,35,41,43,56,37,38,50,52,33,44,37,72,47,20 }},
            {"luke",new int[] { 80,52,38,44,39,49,50,56,62,42,54,59,35,35,32,31,37,43,48,47,38,71,56,53 }},
            {"john",new int[] { 51,25,36,54,47,71,53,59,41,42,57,50,38,31,27,33,26,40,42,31,25 }},
            {"acts",new int[] { 26,47,26,37,42,15,60,40,43,48,30,25,52,28,41,40,34,28,41,38,40,30,35,27,27,32,44,31 }},
            {"rom",new int[] { 32,29,31,25,21,23,25,39,33,21,36,21,14,23,33,27 }},
            {"1-cor",new int[] { 31,16,23,21,13,20,40,13,27,33,34,31,13,40,58,24 }},
            {"2-cor",new int[] { 24,17,18,18,21,18,16,24,15,18,33,21,14 }},
            {"gal",new int[] { 24,21,29,31,26,18 }},
            {"eph",new int[] { 23,22,21,32,33,24 }},
            {"philip",new int[] { 30,30,21,23 }},
            {"col",new int[] { 29,23,25,18 }},
            {"1-thes",new int[] { 10,20,13,18,28 }},
            {"2-thes",new int[] { 12,17,18 }},
            {"1-tim",new int[] { 20,15,16,16,25,21 }},
            {"2-tim",new int[] { 18,26,17,22 }},
            {"titus",new int[] { 16,15,15 }},
            {"philem",new int[] { 25 }},
            {"heb",new int[] { 14,18,19,16,14,20,28,13,28,39,40,29,25 }},
            {"james",new int[] { 27,26,18,17,20 }},
            {"1-pet",new int[] { 25,25,22,19,14 }},
            {"2-pet",new int[] { 21,22,18 }},
            {"1-jn",new int[] { 10,29,24,21,21 }},
            {"2-jn",new int[] { 13 }},
            {"3-jn",new int[] { 14 }},
            {"jude",new int[] { 25 }},
            {"rev",new int[] { 20,29,22,11,14,17,17,13,21,11,19,17,18,20,8,21,18,24,21,15,27,21 }},
            {"1-ne",new int[] { 20,24,31,38,22,6,22,38,6,22,36,23,42,30,36,39,55,25,24,22,26,31 }},
            {"2-ne",new int[] { 32,30,25,35,34,18,11,25,54,25,8,22,26,6,30,13,25,22,21,34,16,6,22,32,30,33,35,32,14,18,21,9,15 }},
            {"jacob",new int[] { 19,35,14,18,77,13,27 }},
            {"enos",new int[] { 27 }},
            {"jarom",new int[] { 15 }},
            {"omni",new int[] { 30 }},
            {"w-of-m",new int[] { 18 }},
            {"mosiah",new int[] { 18,41,27,30,15,7,33,21,19,22,29,37,35,12,31,15,20,35,29,26,36,16,39,25,24,39,37,20,47 }},
            {"alma",new int[] { 33,38,27,20,62,8,27,32,34,32,46,37,31,29,19,21,39,43,36,30,23,35,18,30,17,37,30,14,17,60,38,43,23,41,16,30,47,15,19,26,15,31,54,24,24,41,36,25,30,40,37,40,23,24,35,57,36,41,13,36,21,52,17 }},
            {"hel",new int[] { 34,14,37,26,52,41,29,28,41,19,38,26,39,31,17,25 }},
            {"3-ne",new int[] { 30,19,26,33,26,30,26,25,22,19,41,48,34,27,24,20,25,39,36,46,29,17,14,18,6,21,33,40,9,2 }},
            {"4-ne",new int[] { 49 }},
            {"morm",new int[] { 19,29,22,23,24,22,10,41,37 }},
            {"ether",new int[] { 43,25,28,19,6,30,27,26,35,34,23,41,31,31,34 }},
            {"moro",new int[] { 4,3,4,3,2,9,48,30,26,34 }},
            {"dc",new int[] { 39,3,20,7,35,37,8,12,14,70,30,9,1,11,6,6,9,47,41,84,12,4,7,19,16,2,18,16,50,11,13,5,18,12,27,8,4,42,24,3,12,93,35,6,75,33,4,6,28,46,20,44,7,10,6,20,16,65,24,17,39,9,66,43,6,13,14,35,8,18,11,26,6,7,36,119,15,22,4,5,7,24,6,120,12,11,8,141,21,37,6,2,53,17,17,9,28,48,8,17,101,34,40,86,41,8,100,8,80,16,11,34,10,2,19,1,16,6,7,1,46,9,17,145,4,3,12,25,9,23,8,66,74,12,7,42,10,60 }},
            {"od",new int[] { 20,18 }},
            {"moses",new int[] { 42,31,25,32,59,68,69,30 }},
            {"abr",new int[] { 31,25,28,31,21 }},
            {"js-m",new int[] { 55 }},
            {"js-h",new int[] { 75 }},
            {"a-of-f",new int[] { 13 }},
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
                int prevBookChapter = GetNumChaptersInBook(prevBookName);
                return FeatureToNodeMapping.ScriptureChapter(prevBookCanon, prevBookName, prevBookChapter);
            }
        }

        internal static KnowledgeGraphNodeId? GetNextChapter(string canon, string book, int chapter)
        {
            int thisBookLength = GetNumChaptersInBook(book);
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

        public static string GetCanonForBook(string bookId)
        {
            return BOOK_TO_CANON[bookId];
        }

        public static int GetNumChaptersInBook(string bookId)
        {
            return BOOK_CHAPTER_LENGTHS[bookId].Length;
        }

        public static int GetNumVersesInChapter(string bookId, int chapter)
        {
            return BOOK_CHAPTER_LENGTHS[bookId][chapter - 1];
        }

        public static IEnumerable<ScriptureReference> ParseAllReferences(string inputText, LanguageCode language, bool includeExtra = false)
        {
            if (language.CountryAgnostic().Equals(LanguageCode.ENGLISH))
            {
                return ScriptureMetadataEnglish.ParseAllReferences(inputText, includeExtra);
            }
            else
            {
                throw new NotImplementedException("Unsupported language");
            }
        }

        public static string GetNameForBook(string bookId, LanguageCode language)
        {
            if (language.CountryAgnostic().Equals(LanguageCode.ENGLISH))
            {
                return ScriptureMetadataEnglish.GetNameForBook(bookId);
            }
            else
            {
                throw new NotImplementedException("Unsupported language");
            }
        }

        public static string GetNameForCanon(string canon, LanguageCode language)
        {
            if (language.CountryAgnostic().Equals(LanguageCode.ENGLISH))
            {
                return ScriptureMetadataEnglish.GetNameForCanon(canon);
            }
            else
            {
                throw new NotImplementedException("Unsupported language");
            }
        }
    }
}
