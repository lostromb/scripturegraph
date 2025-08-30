using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training.Extractors
{
    internal class ScriptureReference
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

        public override string? ToString()
        {
            return $"{Canon} {Book} {Chapter}:{Verse}";
        }
    }
}
