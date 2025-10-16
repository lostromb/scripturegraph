using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.App
{
    internal static class Localization
    {
        public static string GetBookName(string bookId)
        {
            if (bookId.Equals("atgq", StringComparison.OrdinalIgnoreCase))
                return "Answers to Gospel Questions";
            else if (bookId.Equals("md", StringComparison.OrdinalIgnoreCase))
                return "Mormon Doctrine";
            else if (bookId.Equals("messiah1", StringComparison.OrdinalIgnoreCase))
                return "Promised Messiah";
            else if (bookId.Equals("messiah2", StringComparison.OrdinalIgnoreCase))
                return "Mortal Messiah";
            else if (bookId.Equals("messiah3", StringComparison.OrdinalIgnoreCase))
                return "Millennial Messiah"; 
            else
                return "UNKNOWN_BOOK";
        }
    }
}
