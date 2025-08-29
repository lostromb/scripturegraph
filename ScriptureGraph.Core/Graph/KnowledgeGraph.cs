using Durandal.Common.Audio.Codecs.ILBC;
using Durandal.Common.Logger;
using Durandal.Common.MathExt;
using Durandal.Common.Time;
using Durandal.Common.Utils;
using Microsoft.VisualBasic;
using ScriptureGraph.Core.Training;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using static System.Formats.Asn1.AsnWriter;

#nullable disable
namespace ScriptureGraph.Core.Graph
{
    public class KnowledgeGraph
    {
        private const int BIN_LOAD_RATIO = 3; // average number of entries per bin until we consider increasing the table size
        private const int NUM_LOCKS = 128; // must be power of two
        private const int MAX_TABLE_SIZE = 0x3FF0000; // A little less than half of int.MaxValue. Above this amount, we stop allocating new tables

        private readonly TrainingLockArray _locks;
        private HashTableLinkedListNode[] _bins;
        private volatile int _numItemsInDictionary;
        private readonly int _edgeCapacity;

        public KnowledgeGraph(int edgeCapacity = 256)
        {
            _locks = new TrainingLockArray(NUM_LOCKS);
            _numItemsInDictionary = 0;
            _bins = new HashTableLinkedListNode[NUM_LOCKS];
            _edgeCapacity = edgeCapacity.AssertPositive(nameof(edgeCapacity));
        }

        public int Count => _numItemsInDictionary;

        public void Add(KnowledgeGraphNodeId key, KnowledgeGraphNode value)
        {
            Add(new KeyValuePair<KnowledgeGraphNodeId, KnowledgeGraphNode>(key, value));
        }

        public void Add(KeyValuePair<KnowledgeGraphNodeId, KnowledgeGraphNode> item)
        {
            ExpandTableIfNeeded();
            HashTableLinkedListNode[] bins;
            KnowledgeGraphNodeId nodeId = item.Key;
            AcquireLockToStableHashBin(nodeId, out bins);

            try
            {
                uint keyHash = (uint)nodeId.GetHashCode();
                uint bin = keyHash % (uint)bins.Length;

                if (bins[bin] == null)
                {
                    // Bin is empty; fill bin with new entry
                    bins[bin] = new HashTableLinkedListNode(item);
                    Interlocked.Increment(ref _numItemsInDictionary);
                }
                else
                {
                    // Bin is full; see if a value already exists
                    HashTableLinkedListNode iter = bins[bin];
                    HashTableLinkedListNode endOfBin = iter;
                    while (iter != null)
                    {
                        // Does an entry already exist with this same key?
                        if (item.Key.Equals(iter.Kvp.Key))
                        {
                            // Update the value of the existing item
                            iter.Kvp = item;
                            return;
                        }

                        iter = iter.Next;
                        if (iter != null)
                        {
                            endOfBin = iter;
                        }
                    }

                    // Key was not found after iterating the bin. Append a new entry to the end of the bin
                    endOfBin.Next = new HashTableLinkedListNode(item);
                    Interlocked.Increment(ref _numItemsInDictionary);
                }
            }
            finally
            {
                _locks.ReleaseLock(nodeId);
            }
        }

        // Enumeration cannot proceed while the graph is being modified.
        public IEnumerator<KeyValuePair<KnowledgeGraphNodeId, KnowledgeGraphNode>> GetUnsafeEnumerator()
        {
            return new KnowledgeGraphUnsafeEnumerator(this);
        }

        public void Train(TrainingFeature feature)
        {
            Train(feature.NodeA, feature.NodeB, feature.EdgeWeight);
            Train(feature.NodeB, feature.NodeA, feature.EdgeWeight);
        }

        public void Train(KnowledgeGraphNodeId nodeA, KnowledgeGraphNodeId nodeB, float increment)
        {
            HashTableLinkedListNode[] bins;
            AcquireLockToStableHashBin(nodeA, out bins);
            try
            {
                uint keyHash = (uint)nodeA.GetHashCode();
                uint bin = keyHash % (uint)bins.Length;

                
                if (bins[bin] == null)
                {
                    // Create a new value
                    KnowledgeGraphNode newNode = new KnowledgeGraphNode(_edgeCapacity);
                    newNode.Edges.Increment(nodeB, increment);
                    bins[bin] = new HashTableLinkedListNode(
                        new KeyValuePair<KnowledgeGraphNodeId, KnowledgeGraphNode>(nodeA, newNode));
                    Interlocked.Increment(ref _numItemsInDictionary);
                }
                else
                {
                    // Bin is full; see if a value already exists
                    HashTableLinkedListNode iter = bins[bin];
                    HashTableLinkedListNode endOfBin = iter;
                    while (iter != null)
                    {
                        if (nodeA.Equals(iter.Kvp.Key))
                        {
                            // Found it!
                            iter.Kvp.Value.Edges.Increment(nodeB, increment);
                            return;
                        }

                        iter = iter.Next;
                        if (iter != null)
                        {
                            endOfBin = iter;
                        }
                    }

                    // If value is not already there, append a new entry to the end of the bin
                    KnowledgeGraphNode newNode = new KnowledgeGraphNode(_edgeCapacity);
                    newNode.Edges.Increment(nodeB, increment);
                    endOfBin.Next = new HashTableLinkedListNode(
                        new KeyValuePair<KnowledgeGraphNodeId, KnowledgeGraphNode>(nodeA, newNode));
                    Interlocked.Increment(ref _numItemsInDictionary);
                }
            }
            finally
            {
                _locks.ReleaseLock(nodeA);
            }
        }

        public bool TryGet(KnowledgeGraphNodeId key, out KnowledgeGraphNode node)
        {
            return TryGetInternal(key, out node);
        }

        private bool TryGetInternal(KnowledgeGraphNodeId key, out KnowledgeGraphNode node)
        {
            uint keyHash = (uint)key.GetHashCode();
            HashTableLinkedListNode[] bins;
            AcquireLockToStableHashBin(key, out bins);
            try
            {
                uint bin = keyHash % (uint)bins.Length;
                if (bins[bin] == null)
                {
                    // Bin is empty.
                    node = null;
                    return false;
                }
                else
                {
                    // Bin is full; see if a value already exists
                    HashTableLinkedListNode iter = bins[bin];
                    while (iter != null)
                    {
                        if (key.Equals(iter.Kvp.Key))
                        {
                            // Found it!
                            node = iter.Kvp.Value;
                            return true;
                        }

                        iter = iter.Next;
                    }

                    node = null;
                    return false;
                }
            }
            finally
            {
                _locks.ReleaseLock(key);
            }
        }

        private void AcquireLockToStableHashBin(
            KnowledgeGraphNodeId node,
            out HashTableLinkedListNode[] hashTable)
        {
            // Copy a local reference of the table in case another thread tries to resize it while we are accessing
            hashTable = _bins;

            // Acquire locks
            _locks.GetLock(node);

            // Detect if the table was resized while we were getting the lock
            while (_bins != hashTable)
            {
                // If so, reacquire a handle to the table and try getting lock again
                _locks.ReleaseLock(node);
                hashTable = _bins;
                _locks.GetLock(node);
            }
        }

        private void ExpandTableIfNeeded()
        {
            if (_numItemsInDictionary < _bins.Length * BIN_LOAD_RATIO ||
                _numItemsInDictionary >= MAX_TABLE_SIZE)
            {
                return;
            }

            // Acquire all bin locks in order
            _locks.GetAllLocks();

            try
            {
                // Create a new table and copy all existing values to it
                uint moduloFactor = (uint)_bins.Length;
                uint newTableLength = moduloFactor * 2;
                HashTableLinkedListNode[] newTable = new HashTableLinkedListNode[newTableLength];

                // Since we expand the table by exactly double each time, we can take advantage of modulo arithmetic
                // and the fact that each bin on the old table corresponds to at most 2 bins on the new table.
                // So all we have to do is iterate through each source bin and "unzip" it into two new bins.
                // This prevents having to reallocate all of the nodes again, but we have to be careful about dangling pointers.
                foreach (HashTableLinkedListNode sourceBin in _bins)
                {
                    HashTableLinkedListNode sourceIter = sourceBin;
                    HashTableLinkedListNode sourceIterNext;
                    HashTableLinkedListNode targetLow = null;
                    HashTableLinkedListNode targetHigh = null;

                    while (sourceIter != null)
                    {
                        sourceIterNext = sourceIter.Next;
                        sourceIter.Next = null;

                        uint targetBin = ((uint)sourceIter.Kvp.Key.GetHashCode()) % newTableLength;
                        if (targetBin < moduloFactor)
                        {
                            // Sort to low bin
                            if (targetLow == null)
                            {
                                // Bin is empty
                                newTable[targetBin] = sourceIter;
                            }
                            else
                            {
                                targetLow.Next = sourceIter;
                            }

                            targetLow = sourceIter;
                        }
                        else
                        {
                            // Sort to high bin
                            if (targetHigh == null)
                            {
                                // Bin is empty
                                newTable[targetBin] = sourceIter;
                            }
                            else
                            {
                                targetHigh.Next = sourceIter;
                            }

                            targetHigh = sourceIter;
                        }

                        sourceIter = sourceIterNext;
                    }
                }

                _bins = newTable;
            }
            finally
            {
                _locks.ReleaseAllLocks();
            }
        }

        // ENUMERATION IS NOT THREAD SAFE!!! CAN'T DO IT DURING TRAINING!!!
        private class KnowledgeGraphUnsafeEnumerator : IEnumerator<KeyValuePair<KnowledgeGraphNodeId, KnowledgeGraphNode>>
        {
            private readonly HashTableLinkedListNode[] _localTableReference;
            private readonly KnowledgeGraph _owner;
            private bool _finished;
            private uint _currentBinIdx;
            private uint _currentBinListIdx;
            private uint _beginOffset;
            private KeyValuePair<KnowledgeGraphNodeId, KnowledgeGraphNode> _current;

            public KnowledgeGraphUnsafeEnumerator(KnowledgeGraph owner)
            {
                _owner = owner;
                _localTableReference = _owner._bins;
                Reset();
                _beginOffset = 0;
            }

            public KnowledgeGraphUnsafeEnumerator(KnowledgeGraph owner, IRandom rand)
            {
                _owner = owner;
                _localTableReference = _owner._bins;
                Reset();
                _beginOffset = (uint)rand.NextInt(0, _localTableReference.Length);
            }

            public KeyValuePair<KnowledgeGraphNodeId, KnowledgeGraphNode> Current => _current;

            object IEnumerator.Current => _current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_finished)
                {
                    return false;
                }

                while (_currentBinIdx < _localTableReference.Length)
                {
                    uint currentBinIdxWithOffset = (_currentBinIdx + _beginOffset) % (uint)_localTableReference.Length;

                    // Was the table resized during enumeration?
                    if (_owner._bins != _localTableReference)
                    {
                        // If so, just abort enumeration
                        _finished = true;
                        _current = default(KeyValuePair<KnowledgeGraphNodeId, KnowledgeGraphNode>);
                        return false;
                    }

                    HashTableLinkedListNode iter = _localTableReference[currentBinIdxWithOffset];
                    if (iter == null)
                    {
                        // Skip over empty bins
                        _currentBinIdx++;
                        _currentBinListIdx = 0;
                    }
                    else
                    {
                        uint iterIdx = 0;

                        // Iterate within a single bin
                        while (iter != null &&
                            iterIdx < _currentBinListIdx)
                        {
                            iter = iter.Next;
                            iterIdx++;
                        }

                        if (iter == null)
                        {
                            // Finished iterating this bin. Move on
                            _currentBinIdx++;
                            _currentBinListIdx = 0;
                        }
                        else
                        {
                            _current = iter.Kvp;
                            _currentBinListIdx = iterIdx + 1;
                            return true;
                        }
                    }
                }

                // Reached end of table
                _finished = true;
                _current = default(KeyValuePair<KnowledgeGraphNodeId, KnowledgeGraphNode>);
                return false;
            }

            public void Reset()
            {
                _currentBinIdx = 0;
                _currentBinListIdx = 0;
                _current = default(KeyValuePair<KnowledgeGraphNodeId, KnowledgeGraphNode>);
                _finished = false;
            }
        }

        private class HashTableLinkedListNode
        {
            public HashTableLinkedListNode(KeyValuePair<KnowledgeGraphNodeId, KnowledgeGraphNode> keyValuePair)
            {
                Kvp = keyValuePair;
            }

            public KeyValuePair<KnowledgeGraphNodeId, KnowledgeGraphNode> Kvp;
            public HashTableLinkedListNode Next;
        }

        public void Save(Stream outStream)
        {
            using (BinaryWriter writer = new BinaryWriter(outStream))
            {
                writer.Write(_edgeCapacity);
                writer.Write(NUM_LOCKS);
                writer.Write(_numItemsInDictionary);

                var enumerator = GetUnsafeEnumerator();
                while (enumerator.MoveNext())
                {
                    WriteNodeId(enumerator.Current.Key, writer);
                    WriteEdges(enumerator.Current.Value.Edges, writer);
                }
            }
        }

        private static void WriteNodeId(KnowledgeGraphNodeId nodeId, BinaryWriter writer)
        {
            writer.Write((ushort)nodeId.Type);
            writer.Write(nodeId.Name);
        }

        private static void WriteEdges(GraphEdgeList edges, BinaryWriter writer)
        {
            //writer.Write(edges.TotalMass);
            writer.Write(edges.NumEdges);
            writer.Write(edges.MaxCapacity);
            var enumerator = edges.GetEnumerator();
            while (enumerator.MoveNext())
            {
                ref KnowledgeGraphEdge edge = ref enumerator.CurrentByRef();
                writer.Write(edge.Mass);
                WriteNodeId(edge.Target, writer);
            }
        }

        public static KnowledgeGraph Load(Stream loadStream)
        {
            using (BinaryReader reader = new BinaryReader(loadStream))
            {
                int edgeCapacity = reader.ReadInt32();
                int numLocks = reader.ReadInt32();
                int numItemsInDictionary = reader.ReadInt32();
                KnowledgeGraph returnVal = new KnowledgeGraph(edgeCapacity);
                for (int nodeIdx = 0; nodeIdx < numItemsInDictionary; nodeIdx++)
                {
                    KnowledgeGraphNodeId thisNodeId = ReadNodeId(reader);
                    GraphEdgeList thisNodeEdges = ReadEdgeList(reader);
                    KnowledgeGraphNode node = new KnowledgeGraphNode(thisNodeEdges);
                    returnVal.Add(thisNodeId, node);
                }

                return returnVal;
            }
        }

        private static KnowledgeGraphNodeId ReadNodeId(BinaryReader reader)
        {
            KnowledgeGraphNodeType type = (KnowledgeGraphNodeType)reader.ReadUInt16();
            string name = reader.ReadString();
            return new KnowledgeGraphNodeId(type, name);
        }

        private static GraphEdgeList ReadEdgeList(BinaryReader reader)
        {
            //float totalMass = reader.ReadSingle();
            int numEdges = reader.ReadInt32();
            int maxCapacity = reader.ReadInt32();
            GraphEdgeList returnVal = new GraphEdgeList(maxCapacity, numEdges);
            for (int edgeIdx = 0; edgeIdx < numEdges; edgeIdx++)
            {
                float edgeMass = reader.ReadSingle();
                KnowledgeGraphNodeId edgeTarget = ReadNodeId(reader);
                returnVal.AddForDeserialization(edgeTarget, edgeMass);
            }

            return returnVal;
        }

        private void QuerySingleScope(
            KnowledgeGraphQuery query,
            Counter<KnowledgeGraphNodeId> thisStepActivation,
            Counter<KnowledgeGraphNodeId> nextStepActivation,
            Counter<KnowledgeGraphNodeId> cumulativeActivation)
        {
            Counter<KnowledgeGraphNodeId> swap = thisStepActivation;
            ValueStopwatch scopeStopwatch = ValueStopwatch.StartNew();

            // Continue the search until we reach the time limit defined in the query, or activations all become too low
            while (scopeStopwatch.Elapsed < query.MaxSearchTime)
            {
                // Single iteration
                foreach (var currentlyActivatedNode in thisStepActivation)
                {
                    KnowledgeGraphNode thisNode;
                    if (!TryGetInternal(currentlyActivatedNode.Key, out thisNode))
                    {
                        // Node not found in this graph
                        continue;
                    }

                    if (currentlyActivatedNode.Value < query.MinActivation)
                    {
                        // This node activation is not strong enough
                        // OPT: we could break the loop entirely if the input set was sorted descending
                        continue;
                    }

                    GraphEdgeList.EdgeRefEnumerator enumerator = thisNode.Edges.GetEnumerator();
                    float normalizedActivation = currentlyActivatedNode.Value / (float)thisNode.Edges.NumEdges;
                    while (enumerator.MoveNext())
                    {
                        ref KnowledgeGraphEdge edge = ref enumerator.CurrentByRef();
                        float activation = NeuronActivation(normalizedActivation, edge.Mass, thisNode.Edges.TotalMass, thisNode.Edges.NumEdges);
                        nextStepActivation.Increment(edge.Target, activation);
                        //logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata,
                        //    "{0} {1:F3} activates {1} {2:F3}",
                        //    currentlyActivatedNode.Key.ToString(),
                        //    currentlyActivatedNode.Value,
                        //    edge.Target.ToString(),
                        //    activation);

                        if (activation < query.MinActivation)
                        {
                            // This edge's activation is not high enough, stop iterating more edges because they can't get better
                            break;
                        }
                    }
                }

                if (nextStepActivation.NumItems == 0)
                {
                    // Nothing crossed the activation threshold. We're done.
                    break;
                }

                // Dump logger output
                //logger.Log("Iteration " + (totalIterations + 1));
                //List<KeyValuePair<KnowledgeGraphNodeId, float>> temp = new List<KeyValuePair<KnowledgeGraphNodeId, float>>(nextStepActivation);
                //temp.Sort((a, b) => b.Value.CompareTo(a.Value));
                //int displayLines = 10;
                //foreach (var line in temp)
                //{
                //    if (displayLines-- <= 0)
                //    {
                //        break;
                //    }

                //    logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:F3} : {1}", line.Value, line.Key.ToString());
                //}

                foreach (var activation in nextStepActivation)
                {
                    cumulativeActivation.Increment(activation.Key, activation.Value);
                }

                swap = thisStepActivation;
                thisStepActivation = nextStepActivation;
                nextStepActivation = swap;
                nextStepActivation.Clear();
            }
        }

        public List<KeyValuePair<KnowledgeGraphNodeId, float>> Query(KnowledgeGraphQuery query, ILogger logger)
        {
            Counter<KnowledgeGraphNodeId> finalCumulativeResult = new Counter<KnowledgeGraphNodeId>();
            HashSet<BitVector32> scopesInitialized = new HashSet<BitVector32>();
            HashSet<BitVector32> scopesProcessed = new HashSet<BitVector32>();
            Dictionary<BitVector32, Counter<KnowledgeGraphNodeId>> initialActivationsPerScope = new Dictionary<BitVector32, Counter<KnowledgeGraphNodeId>>();

            // temporary counters per-scope
            Counter<KnowledgeGraphNodeId> thisStepActivation = new Counter<KnowledgeGraphNodeId>();
            Counter<KnowledgeGraphNodeId> nextStepActivation = new Counter<KnowledgeGraphNodeId>();

            // Built accumulators for all scopes
            int scopeIdxNormalized = 0x1;
            foreach (var scope in query.SearchScopes)
            {
                BitVector32 thisScopeArray = new BitVector32(scopeIdxNormalized);
                scopeIdxNormalized = scopeIdxNormalized << 1;
                Counter<KnowledgeGraphNodeId> thisScopeActivation = new Counter<KnowledgeGraphNodeId>();

                foreach (var initialActivation in query.GetRoots(scope))
                {
                    thisScopeActivation.Increment(initialActivation.Key, initialActivation.Value);
                }

                initialActivationsPerScope.Add(thisScopeArray, thisScopeActivation);
                scopesInitialized.Add(thisScopeArray);
                logger.LogFormat(LogLevel.Vrb, DataPrivacyClassification.SystemMetadata, "Initializing scope {0:x}", thisScopeArray.Data);
            }

            // Loop for as long as we have unique activations per scope
            while (initialActivationsPerScope.Count > 0)
            {
                foreach (var scopeActivations in initialActivationsPerScope)
                {
                    logger.LogFormat(LogLevel.Vrb, DataPrivacyClassification.SystemMetadata, "Processing scope {0:x}", scopeActivations.Key.Data);
                    // Copy activations from scope to initializer
                    thisStepActivation.Clear();
                    nextStepActivation.Clear();
                    thisStepActivation.Increment(scopeActivations.Value);

                    QuerySingleScope(query, thisStepActivation, nextStepActivation, scopeActivations.Value);

                    // Copy from single scope to cumulative
                    finalCumulativeResult.Increment(scopeActivations.Value);
                    scopesProcessed.Add(scopeActivations.Key);
                }

                // Check for overlaps between all permutations of scopes, and turn those into new initial sets
                // Keep iterating for as long as we continue to find
                bool mergeHappened;
                do
                {
                    mergeHappened = false;
                    Dictionary<BitVector32, Counter<KnowledgeGraphNodeId>> newInitialActivations = new Dictionary<BitVector32, Counter<KnowledgeGraphNodeId>>();

                    foreach (var scopeActivationsSrc in initialActivationsPerScope)
                    {
                        foreach (var scopeActivationsDest in initialActivationsPerScope)
                        {
                            BitVector32 unionedArray = new BitVector32(scopeActivationsSrc.Key.Data | scopeActivationsDest.Key.Data);
                            if (scopesInitialized.Contains(unionedArray))
                            {
                                continue;
                            }

                            scopesInitialized.Add(unionedArray);

                            logger.LogFormat(LogLevel.Vrb, DataPrivacyClassification.SystemMetadata, "Initializing scope {0:x}", unionedArray.Data);

                            // Set intersection
                            HashSet<KnowledgeGraphNodeId> unionSet = scopeActivationsSrc.Value.ValueSet();
                            unionSet.IntersectWith(scopeActivationsDest.Value.ValueSet()); // OPT can just enumerate the values list

                            if (unionSet.Count > 0)
                            {
                                logger.LogFormat(LogLevel.Vrb, DataPrivacyClassification.SystemMetadata, "Merging scopes {0:x} and {1:x}", scopeActivationsSrc.Key.Data, scopeActivationsDest.Key.Data);
                                mergeHappened = true;
                                Counter<KnowledgeGraphNodeId> unionedCounter = new Counter<KnowledgeGraphNodeId>();
                                foreach (KnowledgeGraphNodeId overlappedNode in unionSet)
                                {
                                    float weight = query.ScopeOverlapMultiplier * 
                                        (scopeActivationsSrc.Value.GetCount(overlappedNode) + scopeActivationsDest.Value.GetCount(overlappedNode));
                                    unionedCounter.Increment(overlappedNode, weight);
                                    //logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "Boosting {0} with {1:F3}", overlappedNode.ToString(), weight);
                                }

                                // Handle cases where intermediate sets were created (i.e. we merged more than 2 sets into a single one
                                // over the course of several iterations)
                                // In this case, copy over the activations for the non-overlapped nodes that haven't
                                // been processed yet, and mark the "intermediate" half-merged sets as being processed.
                                if (!scopesProcessed.Contains(scopeActivationsSrc.Key))
                                {
                                    scopesProcessed.Add(scopeActivationsSrc.Key);
                                    unionedCounter.Increment(scopeActivationsSrc.Value);
                                    logger.LogFormat(LogLevel.Vrb, DataPrivacyClassification.SystemMetadata, "Marking intermediate scope {0:x} as processed", scopeActivationsSrc.Key.Data);
                                }

                                if (!scopesProcessed.Contains(scopeActivationsDest.Key))
                                {
                                    scopesProcessed.Add(scopeActivationsDest.Key);
                                    unionedCounter.Increment(scopeActivationsDest.Value);
                                    logger.LogFormat(LogLevel.Vrb, DataPrivacyClassification.SystemMetadata, "Marking intermediate scope {0:x} as processed", scopeActivationsDest.Key.Data);
                                }

                                newInitialActivations.Add(unionedArray, unionedCounter);
                            }
                        }
                    }

                    // Scopes that are initialized but not yet processed proceed to the next round
                    foreach (var activation in initialActivationsPerScope)
                    {
                        if (!scopesProcessed.Contains(activation.Key))
                        {
                            newInitialActivations.Add(activation.Key, activation.Value);
                        }
                    }

                    initialActivationsPerScope.Clear();
                    initialActivationsPerScope = newInitialActivations;
                } while (mergeHappened);
            }

            List<KeyValuePair<KnowledgeGraphNodeId, float>> returnVal = new List<KeyValuePair<KnowledgeGraphNodeId, float>>(finalCumulativeResult);
            returnVal.Sort((a, b) => b.Value.CompareTo(a.Value));
            return returnVal;
        }

        private static float NeuronActivation(float currentActivation, float edgeWeight, float edgeTotalMass, int numEdges)
        {
            return currentActivation * FastMath.Sigmoid(((edgeWeight * numEdges / edgeTotalMass) - 1.0f) * 0.5f);
        }
    }

    internal static class CounterExtensions
    {
        internal static void Increment<T>(this Counter<T> dest, Counter<T> src)
        {
            foreach (var kvp in src)
            {
                dest.Increment(kvp.Key, kvp.Value);
            }
        }

        internal static HashSet<T> ValueSet<T>(this Counter<T> counter)
        {
            HashSet<T> returnVal = new HashSet<T>(counter.NumItems);
            foreach (var value in counter)
            {
                returnVal.Add(value.Key);
            }

            return returnVal;
        }
    }
}

#nullable restore