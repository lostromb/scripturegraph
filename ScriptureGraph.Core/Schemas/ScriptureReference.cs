using Durandal.Common.Parsers;
using ScriptureGraph.Core.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class ScriptureReference
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
            if (Chapter.HasValue && Verse.HasValue)
            {
                return $"{Canon} {Book} {Chapter}:{Verse}";
            }
            else if (Chapter.HasValue)
            {
                return $"{Canon} {Book} {Chapter}";
            }
            else
            {
                return $"{Canon} {Book}";
            }
        }

        public ScriptureReference(KnowledgeGraphNodeId entityId)
        {
            if (entityId.Type == KnowledgeGraphNodeType.ScriptureVerse)
            {
                string[] parts = entityId.Name.Split('|');
                Canon = parts[0];
                Book = parts[1];
                Chapter = int.Parse(parts[2]);
                Verse = int.Parse(parts[3]);
            }
            else if (entityId.Type == KnowledgeGraphNodeType.ScriptureChapter)
            {
                string[] parts = entityId.Name.Split('|');
                Canon = parts[0];
                Book = parts[1];
                Chapter = int.Parse(parts[2]);
                Verse = null;
            }
            else if (entityId.Type == KnowledgeGraphNodeType.ScriptureBook)
            {
                string[] parts = entityId.Name.Split('|');
                Canon = parts[0];
                Book = parts[1];
                Chapter = null;
                Verse = null;
            }
            else
            {
                throw new FormatException("Entity id is not a scripture reference");
            }
        }
    }
}
