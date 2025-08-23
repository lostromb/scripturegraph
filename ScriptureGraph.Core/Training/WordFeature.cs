using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Graph;

namespace ScriptureGraph.Core.Training
{
    internal class WordFeature : KnowledgeGraphFeature
    {
        public override KnowledgeGraphNodeType Type => KnowledgeGraphNodeType.Entity;
        public override string NodeName => ("w:" + Language.ToBcp47Alpha3String() + ":" + Name);
        public LanguageCode Language = LanguageCode.UNDETERMINED;
        public string Name = string.Empty;
    }
}
