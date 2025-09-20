using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training.Extractors
{
    internal class StructuredVerse
    {
        public StructuredVerse(string canon, string book, int chapter, string paragraphId, string paragraphClass, string text)
        {
            Canon = canon;
            Book = book;
            Chapter = chapter;
            ParagraphId = paragraphId;
            ParagraphClass = paragraphClass;
            Text = text;
        }

        public string Canon;
        public string Book;
        public int Chapter;
        public string ParagraphId;
        public string Text;
        public string ParagraphClass;
    }
}
