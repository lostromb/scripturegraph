using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training.Extractors
{
    internal class StructuredFootnote
    {
        public StructuredFootnote(string noteId, string text)
        {
            NoteId = noteId;
            Text = text;
        }

        public string NoteId;
        public string Text;
        public List<ScriptureReference> ScriptureReferences { get; } = new List<ScriptureReference>();
    }
}
