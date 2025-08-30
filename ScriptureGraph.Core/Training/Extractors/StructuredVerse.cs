using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training.Extractors
{
    internal class StructuredVerse
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
}
