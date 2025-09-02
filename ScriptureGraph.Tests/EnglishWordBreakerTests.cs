using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Training;
using ScriptureGraph.Core.Training.Extractors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Tests
{
    [TestClass]
    public class EnglishWordBreakerTests
    {
        [TestMethod]
        [DataRow("", "")]
        [DataRow("   ", "   ")]
        [DataRow("Test", "Test")]
        [DataRow("Jesus Christ", "Jesus Christ")]
        [DataRow("Jesus Christ, Appearances", "Appearances Jesus Christ")]
        [DataRow("Jesus Christ,      Appearances", "Appearances Jesus Christ")]
        [DataRow("Jesus Christ, Appearances, Antemortal", "Appearances Jesus Christ, Antemortal")]
        [DataRow("Jesus Christ, Appearances, Antemortal, Excellent", "Appearances Jesus Christ, Antemortal, Excellent")]
        public void TestCommaInversion(string input, string expectedOutput)
        {
            bool expectChange = !string.Equals(input, expectedOutput, StringComparison.Ordinal);
            Assert.AreEqual(expectChange, EnglishWordFeatureExtractor.PerformCommaInversion(ref input));
            Assert.AreEqual(expectedOutput, input);
        }

        [TestMethod]
        public void TestCharLevelWordBreaker()
        {
            string input = "This is a TEST";
            KnowledgeGraphNodeId[] ngrams = EnglishWordFeatureExtractor.ExtractCharLevelNGrams(input).ToArray();

            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.Word("this", LanguageCode.ENGLISH)));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.Word("is", LanguageCode.ENGLISH)));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.Word("a", LanguageCode.ENGLISH)));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.Word("test", LanguageCode.ENGLISH)));

            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('[', 't')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('t', 'h')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('h', 'i')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('i', 's')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('s', ']')));

            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('[', 'i')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('i', 's')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('s', ']')));

            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('[', 'a')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('a', ']')));

            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('[', 't')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('t', 'e')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('e', 's')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('s', 't')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('t', ']')));

            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('[', 't', 'h')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('t', 'h', 'i')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('h', 'i', 's')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('i', 's', ']')));

            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('[', 'i', 's')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('i', 's', ']')));

            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('[', 'a', ']')));

            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('[', 't', 'e')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('t', 'e', 's')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('e', 's', 't')));
            Assert.IsTrue(ngrams.Contains(FeatureToNodeMapping.CharNGram('s', 't', ']')));
        }
    }
}
