using Durandal.Common.MathExt;
using Durandal.Common.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    public class GraphEdgeList
    {
        private readonly int _maxCapacity;
        private int _currentCapacity;
        private int _currentLength;
        private KnowledgeGraphEdge[] _list;
        private float _totalMass;

        public GraphEdgeList(int maxCapacity)
        {
            _maxCapacity = Math.Max(4, maxCapacity);
            _currentCapacity = 4;
            _currentLength = 0;
            _list = new KnowledgeGraphEdge[_currentCapacity];
            _totalMass = 0;
        }

        /// <summary>
        /// Use this constructor only when deserializing
        /// </summary>
        /// <param name="maxCapacity"></param>
        /// <param name="numEdges"></param>
        internal GraphEdgeList(int maxCapacity, int numEdges)
        {
            _maxCapacity = maxCapacity;
            _currentCapacity = (int)BitOperations.RoundUpToPowerOf2((uint)numEdges);
            _currentLength = 0;
            _list = new KnowledgeGraphEdge[_currentCapacity];
            _totalMass = 0;
        }

        public int NumEdges => _currentLength;
        public float TotalMass => _totalMass;
        public int MaxCapacity => _maxCapacity;
        internal int CurrentCapacity => _currentCapacity;

        public void Increment(in KnowledgeGraphNodeId nodeRef, float massIncrease)
        {
            int idx = 0;
            while (idx < _currentLength)
            {
                ref KnowledgeGraphEdge cur = ref _list[idx];
                if (cur.Target.Equals(nodeRef))
                {
                    // Found the right node. Increase its mass
                    cur.Mass += massIncrease;

                    // Did this increment move it higher in the list?
                    // OPT figure out how to do this with ref copies or whatever
                    while (idx > 0 && _list[idx].Mass > _list[idx - 1].Mass)
                    {
                        // Bubble sort it
                        KnowledgeGraphEdge swap = _list[idx - 1];
                        _list[idx - 1] = _list[idx];
                        _list[idx] = swap;
                        idx--;
                    }

                    _totalMass += massIncrease;
                    return;
                }

                idx++;
            }

            // Didn't find it in the list. Need to add an edge.
            // First, make room
            if (_currentLength == _currentCapacity)
            {
                IncreaseCapacity();
            }

            // Then make a new edge
            _list[_currentLength++] = new KnowledgeGraphEdge(nodeRef, massIncrease);

            // And bubble sort it up if needed
            idx = _currentLength - 1;
            while (idx > 0 && _list[idx].Mass > _list[idx - 1].Mass)
            {
                // Bubble sort it
                KnowledgeGraphEdge swap = _list[idx - 1];
                _list[idx - 1] = _list[idx];
                _list[idx] = swap;
                idx--;
            }

            _totalMass += massIncrease;
        }

        /// <summary>
        /// Should only be called when deserializing a model
        /// </summary>
        /// <param name="nodeRef"></param>
        /// <param name="initialMass"></param>
        internal void AddForDeserialization(in KnowledgeGraphNodeId nodeRef, float initialMass)
        {
            // Make a new edge
            _list[_currentLength++] = new KnowledgeGraphEdge(nodeRef, initialMass);
            _totalMass += initialMass;
        }

        private void IncreaseCapacity()
        {
            int newCapacity = Math.Min(_maxCapacity, _currentCapacity * 2);
            if (newCapacity == _currentCapacity)
            {
                // Can't expand. Prune down the existing list.
                int elementsToPrune = Math.Max(1, _currentLength / 4);
                // Calculate the mass we lost from all the pruning
                for (int element = _currentLength - elementsToPrune; element < _currentLength; element++)
                {
                    _totalMass -= _list[element].Mass;
                }
#if DEBUG
                // Technically we don't have to clear any memory, just shorten the list length
                _list.AsSpan(_currentLength - elementsToPrune, elementsToPrune).Clear();
#endif
                _currentLength -= elementsToPrune;
            }
            else
            {
                // Make the list bigger.
                KnowledgeGraphEdge[] newList = new KnowledgeGraphEdge[newCapacity];
                _list.AsSpan(0, _currentLength).CopyTo(newList.AsSpan());
                _list = newList;
                _currentCapacity = newCapacity;
            }
        }

        /// <summary>
        /// Not a regular IEnumerator because it iterates items by reference instead of by copy
        /// </summary>
        /// <returns></returns>
        public EdgeRefEnumerator GetEnumerator()
        {
            return new EdgeRefEnumerator(this);
        }

        private ref KnowledgeGraphEdge ElementAt(int index)
        {
            return ref _list[index];
        }

        public class EdgeRefEnumerator
        {
            private readonly GraphEdgeList _parent;
            private int _current = -1;

            public EdgeRefEnumerator(GraphEdgeList parent)
            {
                _parent = parent;
            }

            public void Reset()
            {
                _current = -1;
            }

            public bool MoveNext()
            {
                return ++_current < _parent._currentLength;
            }

            public ref KnowledgeGraphEdge CurrentByRef()
            {
                return ref _parent.ElementAt(_current);
            }

            public KnowledgeGraphEdge Current()
            {
                return _parent.ElementAt(_current);
            }
        }
    }
}
