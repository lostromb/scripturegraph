using Durandal.Common.Logger;
using Durandal.Common.NLP.Language;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Training;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Tests
{
    [TestClass]
    public class TrainingKnowledgeGraphTests
    {
        [TestMethod]
        public void TestTrainingGraphBasic()
        {
            TrainingKnowledgeGraph graph = new TrainingKnowledgeGraph(128);
            KnowledgeGraphNodeId rootNode = FeatureToNodeMapping.ScriptureVerse("1-ne", 1, 1);
            for (int c = 0; c < 10; c++)
            {
                graph.Train(rootNode, FeatureToNodeMapping.Word(c.ToString(), LanguageCode.ENGLISH), 1.0f + (c * 0.1f));
            }

            KnowledgeGraphNode node;
            Assert.IsTrue(graph.TryGet(rootNode, out node));
            Assert.AreEqual(10, node.Edges.NumEdges);
            var enumerator = node.Edges.GetEnumerator();
            int edgesEnumerated = 0;
            while (enumerator.MoveNext())
            {
                // Edges should be in order of weight
                Assert.AreEqual($"Word|{9 - edgesEnumerated}|en", enumerator.Current().Target.ToString());
                edgesEnumerated++;
            }

            Assert.AreEqual(edgesEnumerated, node.Edges.NumEdges);
        }


        [TestMethod]
        public void TestTrainingGraphSerializeAndContinue()
        {
            ILogger logger = new ConsoleLogger();
            TrainingKnowledgeGraph graph = new TrainingKnowledgeGraph(128);
            KnowledgeGraphNodeId rootNode = FeatureToNodeMapping.ScriptureVerse("1-ne", 1, 1);
            for (int c = 0; c < 5; c++)
            {
                graph.Train(rootNode, FeatureToNodeMapping.Word(c.ToString(), LanguageCode.ENGLISH), 1.0f + (c * 0.1f));
            }

            using (MemoryStream stream = new MemoryStream())
            {
                graph.Save(stream, logger);
                stream.Position = 0;
                graph = TrainingKnowledgeGraph.Load(stream);
            }

            for (int c = 5; c < 10; c++)
            {
                graph.Train(rootNode, FeatureToNodeMapping.Word(c.ToString(), LanguageCode.ENGLISH), 1.0f + (c * 0.1f));
            }

            KnowledgeGraphNode node;
            Assert.IsTrue(graph.TryGet(rootNode, out node));
            Assert.AreEqual(10, node.Edges.NumEdges);
            var enumerator = node.Edges.GetEnumerator();
            int edgesEnumerated = 0;
            while (enumerator.MoveNext())
            {
                // Edges should be in order of weight
                Assert.AreEqual($"Word|{9 - edgesEnumerated}|en", enumerator.Current().Target.ToString());
                edgesEnumerated++;
            }

            Assert.AreEqual(edgesEnumerated, node.Edges.NumEdges);
        }
    }
}
