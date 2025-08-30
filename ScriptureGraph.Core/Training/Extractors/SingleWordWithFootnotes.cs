using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training.Extractors
{
    internal class SingleWordWithFootnotes
    {
        public SingleWordWithFootnotes(string word, StructuredFootnote? footnote = null)
        {
            Word = word;
            Footnote = footnote;
        }

        public string Word;
        public StructuredFootnote? Footnote;

        public override string? ToString()
        {
            if (Footnote != null)
            {
                return $"{Word}[{Footnote.NoteId}]";
            }
            else
            {
                return Word;
            }
        }
    }
}
