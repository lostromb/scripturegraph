using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Training.Extractors;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training
{
    public static class OmniParser
    {
        // Hymns?(?:[,\. #]|no|number){1,6}(\d{1,4})(?:(?:[,\. #]|no|number){1,6}(\d{1,4}))?
        private static readonly Regex EXTRACTOR_HYMN_ENG = new Regex("Hymns?(?:[,\\. #]|no|number){1,6}(\\d{1,4})(?:(?:[,\\. #]|no|number){1,6}(\\d{1,4}))?", RegexOptions.IgnoreCase);

        // (the-living-christ-the-testimony-of-the-apostles|The Living Christ.{1,6}(Testimony|Proclamation|Declaration|Document)(\W|$))
        private static readonly Regex EXTRACTOR_LIVING_CHRIST_ENG = new Regex("(the-living-christ-the-testimony-of-the-apostles|The Living Christ.{1,6}(Testimony|Proclamation|Declaration|Document)(\\W|$))", RegexOptions.IgnoreCase);

        // (the-family-a-proclamation-to-the-world|The Family.{1,4}Proclamation)
        private static readonly Regex EXTRACTOR_FAMILY_PROC_ENG = new Regex("(the-family-a-proclamation-to-the-world|The Family.{1,4}Proclamation)", RegexOptions.IgnoreCase);

        public static IEnumerable<OmniParserOutput> ParseHtml(string html, ILogger logger, LanguageCode language)
        {
            ISet<OmniParserOutput> dedupOutputs = new HashSet<OmniParserOutput>();
            foreach (ScriptureReference reference in LdsDotOrgCommonParsers.ParseAllScriptureReferences(html, logger))
            {
                OmniParserOutput convertedScript = new OmniParserOutput(reference);
                if (!dedupOutputs.Contains(convertedScript))
                {
                    dedupOutputs.Add(convertedScript);
                }
            }

            // Parse hymns
            foreach (Match m in EXTRACTOR_HYMN_ENG.Matches(html))
            {
                int hymnNum = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : int.Parse(m.Groups[1].Value);
                string? hymnId;
                if (HymnMetadataEnglish.TryGetHymnId(hymnNum, out hymnId))
                {
                    OmniParserOutput hymnNode = new OmniParserOutput(FeatureToNodeMapping.Hymn(hymnId));
                    if (!dedupOutputs.Contains(hymnNode))
                    {
                        dedupOutputs.Add(hymnNode);
                    }
                }
            }

            // Parse proclamations
            if (EXTRACTOR_LIVING_CHRIST_ENG.Match(html).Success)
            {
                OmniParserOutput node = new OmniParserOutput(FeatureToNodeMapping.Proclamation(ProclamationsFeatureExtractor.PROC_ID_LIVING_CHRIST));
                if (!dedupOutputs.Contains(node))
                {
                    dedupOutputs.Add(node);
                }
            }

            if (EXTRACTOR_FAMILY_PROC_ENG.Match(html).Success)
            {
                OmniParserOutput node = new OmniParserOutput(FeatureToNodeMapping.Proclamation(ProclamationsFeatureExtractor.PROC_ID_THE_FAMILY));
                if (!dedupOutputs.Contains(node))
                {
                    dedupOutputs.Add(node);
                }
            }

            return dedupOutputs;
        }

        // Future things to parse:
        // "Hymns, no. 153"
        // “Abide with Me; ’Tis Eventide,” Hymns, no. 165 (need to rely on english hymnbook numbering)
        // "The Family: A Proclamation to the World"
        // https://www.churchofjesuschrist.org/study/general-conference/2006/04/true-to-the-faith?lang=eng
        // https://www.churchofjesuschrist.org/study/liahona/2021/05/49nelson?lang=eng
        // 
    }
}
