using Durandal.Common.Logger;
using ScriptureGraph.Core.Training.Extractors;

namespace ScriptureGraph.Core.Training
{
    internal static class OmniParser
    {
        internal static IEnumerable<OmniParserOutput> ParseHtml(string html, ILogger logger)
        {
            ISet<ScriptureReference> references = new HashSet<ScriptureReference>();
            LdsDotOrgCommonParsers.ParseAllScriptureReferences(html, references, logger);
            foreach (ScriptureReference reference in references)
            {
                yield return new OmniParserOutput(reference);
            }
        }

        // Future things to parse:
        // "Hymns, no. 153"
        // "The Family: A Proclamation to the World"
        // https://www.churchofjesuschrist.org/study/general-conference/2006/04/true-to-the-faith?lang=eng
        // https://www.churchofjesuschrist.org/study/liahona/2021/05/49nelson?lang=eng
        // 
    }
}
