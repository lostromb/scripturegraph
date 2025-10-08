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
            Paragraph = verse.ToString();
            Verse = verse;
            LowEmphasis = false;
        }

        public ScriptureReference(string canon, string book, int chapter, string paragraph)
        {
            Canon = canon;
            Book = book;
            Chapter = chapter;
            Paragraph = paragraph;
            Verse = null;
            LowEmphasis = false;
        }

        public string Canon;
        public string Book;
        public int? Chapter;
        public int? Verse;

        // Special cases:
        // "intro" - diagetic introduction
        // "study-summary" - modern-day chapter summary
        public string Paragraph;

        // Usually indicates that this reference was part of a long range of verses, e.g. "Isaiah 53:1-11", and
        // this is not the verse that is the "main" verse within that range.
        public bool LowEmphasis;

        public override string? ToString()
        {
            if (Chapter.HasValue && !string.IsNullOrEmpty(Paragraph))
            {
                return $"{Canon} {Book} {Chapter}:{Paragraph}";
            }
            else if (Chapter.HasValue && Verse.HasValue)
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

        public override bool Equals(object? obj)
        {
            return obj != null &&
                obj is ScriptureReference other &&
                Chapter == other.Chapter &&
                string.Equals(Book, other.Book, StringComparison.Ordinal) &&
                string.Equals(Paragraph, other.Paragraph, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return 
                (7931 * Chapter.GetValueOrDefault(0).GetHashCode()) ^
                (1903 * Book.GetHashCode()) ^
                (5443 * Paragraph.GetHashCode());
        }

        public ScriptureReference(KnowledgeGraphNodeId entityId)
        {
            if (entityId.Type == KnowledgeGraphNodeType.ScriptureVerse)
            {
                string[] parts = entityId.Name.Split('|');
                Book = parts[0];
                Canon = ScriptureMetadata.GetCanonForBook(Book);
                Chapter = int.Parse(parts[1]);
                int verseNum;
                if (int.TryParse(parts[2], out verseNum))
                {
                    Verse = verseNum;
                }
                else
                {
                    Verse = null;
                }

                Paragraph = parts[2];
            }
            else if (entityId.Type == KnowledgeGraphNodeType.ScriptureChapter)
            {
                string[] parts = entityId.Name.Split('|');
                Book = parts[0];
                Canon = ScriptureMetadata.GetCanonForBook(Book);
                Chapter = int.Parse(parts[1]);
                Verse = null;
            }
            else if (entityId.Type == KnowledgeGraphNodeType.ScriptureBook)
            {
                Book = entityId.Name;
                Canon = ScriptureMetadata.GetCanonForBook(Book);
                Chapter = null;
                Verse = null;
            }
            else
            {
                throw new FormatException("Entity id is not a scripture reference");
            }
        }

        public KnowledgeGraphNodeId ToNodeId()
        {
            if (string.Equals(Canon, "tg", StringComparison.Ordinal))
            {
                // Topical guide topic
                return FeatureToNodeMapping.TopicalGuideKeyword(Book);
            }
            else if (string.Equals(Canon, "bd", StringComparison.Ordinal))
            {
                // Bible dictionary topic
                return FeatureToNodeMapping.BibleDictionaryTopic(Book);
            }
            else if (string.Equals(Canon, "gs", StringComparison.Ordinal))
            {
                // GS topic
                return FeatureToNodeMapping.GuideToScripturesTopic(Book);
            }
            else
            {
                if (Chapter.HasValue &&
                    Verse.HasValue)
                {
                    // Regular scripture ref
                    return FeatureToNodeMapping.ScriptureVerse(
                        Book,
                        Chapter.Value,
                        Verse.Value);
                }
                else if (Chapter.HasValue)
                {
                    if (Paragraph != null)
                    {
                        // Non-numerical paragraph
                        return FeatureToNodeMapping.ScriptureSupplementalParagraph(
                            Book,
                            Chapter.Value,
                            Paragraph);
                    }
                    else
                    {
                        // Reference to an entire chapter
                        return FeatureToNodeMapping.ScriptureChapter(
                            Book,
                            Chapter.Value);
                    }
                }
                else
                {
                    // Reference to an entire book
                    return FeatureToNodeMapping.ScriptureBook(
                        Book);
                }
            }
        }
    }
}
