using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    public class KnowledgeGraphQuery
    {
        private readonly Dictionary<int, Dictionary<KnowledgeGraphNodeId, float>> _scopes;

        public KnowledgeGraphQuery()
        {
            _scopes = new Dictionary<int, Dictionary<KnowledgeGraphNodeId, float>>();
        }

        private TimeSpan _maxSearchTime = TimeSpan.FromSeconds(1);

        public TimeSpan MaxSearchTime
        {
            get
            {
                return _maxSearchTime;
            }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("Search time must be a positive timespan");
                }

                _maxSearchTime = value;
            }
        }

        private float _minActivation = 0.00001f;

        public float MinActivation
        {
            get
            {
                return _minActivation;
            }
            set
            {
                if (value < 0 || value > 1.0f)
                {
                    throw new ArgumentOutOfRangeException("Activation threshold must be between 0 and 1");
                }

                _minActivation = value;
            }
        }


        public IEnumerable<int> SearchScopes => _scopes.Keys;

        public IReadOnlyDictionary<KnowledgeGraphNodeId, float> GetRoots(int searchScope)
        {
            return _scopes[searchScope];
        }

        public void AddRootNode(KnowledgeGraphNodeId nodeId, int searchScope, float weight = 1.0f)
        {
            Dictionary<KnowledgeGraphNodeId, float>? scopeDict;
            if (!_scopes.TryGetValue(searchScope, out scopeDict))
            {
                scopeDict = new Dictionary<KnowledgeGraphNodeId, float>();
                _scopes.Add(searchScope, scopeDict);
            }

            scopeDict.Add(nodeId, weight);
        }
    }
}
