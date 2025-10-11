using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.App.Schemas
{
    internal enum SearchResultEntityType
    {
        Unknown,
        KeywordPhrase,
        ScriptureBook,
        ScriptureChapter,
        ScriptureVerse,
        Person,
        ConferenceTalk,
        Topic,
        Book_ATGQ,
        Book_MD,
    }
}
