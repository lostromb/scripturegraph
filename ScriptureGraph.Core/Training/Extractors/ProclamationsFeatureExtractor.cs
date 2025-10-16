using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using Durandal.Common.Parsers;
using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Schemas;
using ScriptureGraph.Core.Schemas.Documents;
using System.Net;
using System.Text.RegularExpressions;

namespace ScriptureGraph.Core.Training.Extractors
{
    public class ProclamationsFeatureExtractor
    {
        public static string PROC_ID_LIVING_CHRIST = "lc";
        public static string PROC_ID_THE_FAMILY = "fam";
    }
}
