using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training
{
    internal class EntityFeature : KnowledgeGraphFeature
    {
        public override KnowledgeGraphNodeType Type => KnowledgeGraphNodeType.Entity;
        public override string NodeName => ("e:" + Language.ToBcp47Alpha3String() + ":" + Name);
        public LanguageCode Language = LanguageCode.UNDETERMINED;
        public string Name = string.Empty;
    }
}
