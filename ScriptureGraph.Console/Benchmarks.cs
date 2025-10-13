using BenchmarkDotNet.Attributes;
using ScriptureGraph.Core.Graph;
using ScriptureGraph.Core.Training;
using ScriptureGraph.Core.Training.Extractors;

namespace ScriptureGraph.Console
{
    [IterationCount(200)]
    public class Benchmarks
    {
        private TrainingKnowledgeGraph? _freshGraph;
        private TrainingKnowledgeGraph? _staleGraph;
        private List<TrainingFeature> _training;

        public Benchmarks()
        {
            _training = new List<TrainingFeature>();
            EnglishWordFeatureExtractor.ExtractTrainingFeatures(
                "Far, far above, something large and old and cold rides the long dark, frosted with space dust, pocked by micrometeors. Solar panels give off a tired gleam, like dusty windows. Inside the armored hull a receiver listens patiently to the same wash of static that it has been hearing for millennia. But now something is changing: Inside the static, like flotsam washing",
                _training, FeatureToNodeMapping.Entity("1"));
            EnglishWordFeatureExtractor.ExtractTrainingFeatures(
                "ashore in the surf, comes a familiar message. The ancient computer brain detects it and responds. Many of its systems have been damaged over the long years, but it has others, fail-safes and backups. Power cells hum; glowing ribbons of light begin to weave through the coils of the weapon chamber; ice crystals tumble away in a bright, widening cloud as heavy shields slide open.\r\n ",
                _training, FeatureToNodeMapping.Entity("2"));
            EnglishWordFeatureExtractor.ExtractTrainingFeatures(
                "ODIN gazes down into the blue pool of the Earth and waits to be told what it must do.",
                _training, FeatureToNodeMapping.Entity("3"));
            EnglishWordFeatureExtractor.ExtractTrainingFeatures(
                "ODIN swung its gaze westward, pulling back, struggling to make sense of the incomprehensible world it had awoken to. Where were the sprawling cities of its masters, New York and San Angeles, that it had been put into orbit to defend? Where had the new mountain ranges come from? All those new seas? And what were those huge vehicles creeping across Europe, trailing their long sooty smears of exhaust smoke behind them?",
                _training, FeatureToNodeMapping.Entity("4"));
            EnglishWordFeatureExtractor.ExtractTrainingFeatures(
                "The old weapon clung to the one familiar thing that this changed world could offer it: the stream of coded data rising like a silken thread from somewhere in the uplands of central Asia.",
                _training, FeatureToNodeMapping.Entity("5"));
        }

        [IterationSetup]
        public void Setup()
        {
            _freshGraph = new TrainingKnowledgeGraph();
            _staleGraph = new TrainingKnowledgeGraph();
            Train(_staleGraph);
        }

        [Benchmark]
        public void TrainFreshGraph()
        {
            Train(_freshGraph!);
        }

        [Benchmark]
        public void TrainStaleGraph()
        {
            Train(_staleGraph!);
        }

        private void Train(TrainingKnowledgeGraph graph)
        {
            foreach (var feature in _training)
            {
                graph.Train(feature);
            }
        }
    }
}
