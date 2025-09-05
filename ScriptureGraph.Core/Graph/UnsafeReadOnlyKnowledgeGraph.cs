using Durandal.Common.IO;
using Durandal.Common.IO.Hashing;
using Durandal.Common.Logger;
using Durandal.Common.MathExt;
using Durandal.Common.Time;
using ScriptureGraph.Core.Training;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    public class UnsafeReadOnlyKnowledgeGraph : IKnowledgeGraph
    {
        private FrozenDictionary<UnsafeNodeId, UnsafeNode> _nodes;

        public List<KeyValuePair<KnowledgeGraphNodeId, float>> Query(KnowledgeGraphQuery query, ILogger logger)
        {
            HashSet<UnsafeNodeId> allRootNodeIds = new HashSet<UnsafeNodeId>();
            Counter<UnsafeNodeId> finalCumulativeResult = new Counter<UnsafeNodeId>();
            HashSet<BitVector32> scopesInitialized = new HashSet<BitVector32>();
            HashSet<BitVector32> scopesProcessed = new HashSet<BitVector32>();
            Dictionary<BitVector32, Counter<UnsafeNodeId>> initialActivationsPerScope = new Dictionary<BitVector32, Counter<UnsafeNodeId>>();

            // temporary counters per-scope
            Counter<UnsafeNodeId> thisStepActivation = new Counter<UnsafeNodeId>();
            Counter<UnsafeNodeId> nextStepActivation = new Counter<UnsafeNodeId>();

            // TODO: convert input ids onto temporary heap

            // Built accumulators for all scopes
            int scopeIdxNormalized = 0x1;
            foreach (var scope in query.SearchScopes)
            {
                BitVector32 thisScopeArray = new BitVector32(scopeIdxNormalized);
                scopeIdxNormalized = scopeIdxNormalized << 1;
                if (scopeIdxNormalized == 0x0)
                {
                    // if we have more than 32 scopes, just dump everything into the final scope
                    logger.Log("More than 32 input scopes in search query; some will be combined arbitrarily", LogLevel.Wrn);
                    scopeIdxNormalized = unchecked((int)0x80000000U);
                }

                Counter<UnsafeNodeId> thisScopeActivation = new Counter<UnsafeNodeId>();

                foreach (var initialActivation in query.GetRoots(scope))
                {
                    thisScopeActivation.Increment(initialActivation.Key, initialActivation.Value);

                    if (!allRootNodeIds.Contains(initialActivation.Key))
                    {
                        allRootNodeIds.Add(initialActivation.Key);
                    }
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
                    Dictionary<BitVector32, Counter<UnsafeNodeId>> newInitialActivations = new Dictionary<BitVector32, Counter<UnsafeNodeId>>();

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
                            HashSet<UnsafeNodeId> unionSet = scopeActivationsSrc.Value.ValueSet();
                            unionSet.IntersectWith(scopeActivationsDest.Value.ValueSet()); // OPT can just enumerate the values list

                            if (unionSet.Count > 0)
                            {
                                logger.LogFormat(LogLevel.Vrb, DataPrivacyClassification.SystemMetadata, "Merging scopes {0:x} and {1:x}", scopeActivationsSrc.Key.Data, scopeActivationsDest.Key.Data);
                                mergeHappened = true;
                                Counter<UnsafeNodeId> unionedCounter = new Counter<UnsafeNodeId>();
                                foreach (UnsafeNodeId overlappedNode in unionSet)
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

            // Remove original root nodes from the output
            foreach (UnsafeNodeId originalNode in allRootNodeIds)
            {
                finalCumulativeResult.Remove(originalNode);
            }

            List<KeyValuePair<UnsafeNodeId, float>> returnVal = new List<KeyValuePair<UnsafeNodeId, float>>(finalCumulativeResult);
            returnVal.Sort((a, b) => b.Value.CompareTo(a.Value));

            // Todo: convert results
            return returnVal;
        }

        private static float NeuronActivation(float currentActivation, float edgeWeight, float edgeTotalMass, int numEdges)
        {
            return currentActivation * FastMath.Sigmoid(((edgeWeight * numEdges / edgeTotalMass) - 1.0f) * 0.5f);
        }

        private void QuerySingleScope(
            KnowledgeGraphQuery query,
            Counter<UnsafeNodeId> thisStepActivation,
            Counter<UnsafeNodeId> nextStepActivation,
            Counter<UnsafeNodeId> cumulativeActivation)
        {
            Counter<UnsafeNodeId> swap = thisStepActivation;
            ValueStopwatch scopeStopwatch = ValueStopwatch.StartNew();

            // Continue the search until we reach the time limit defined in the query, or activations all become too low
            while (scopeStopwatch.Elapsed < query.MaxSearchTime)
            {
                // Single iteration
                foreach (var currentlyActivatedNode in thisStepActivation)
                {
                    UnsafeNode thisNode;
                    if (!_nodes.TryGetValue(currentlyActivatedNode.Key, out thisNode))
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
                        ref UnsafeEdge edge = ref enumerator.CurrentByRef();
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


        public static async Task<UnsafeReadOnlyKnowledgeGraph> Load(Stream inputStream, NativeMemoryHeap storageHeap)
        {
            ushort edgeCapacity;
            int numItemsInDict;
            long[] nodeOffsets;
            using (BinaryReader tempReader = new BinaryReader(inputStream, Encoding.UTF8, leaveOpen: true))
            {
                edgeCapacity = tempReader.ReadUInt16();
                numItemsInDict = tempReader.ReadInt32();
                nodeOffsets = new long[numItemsInDict];
                for (int offset = 0; offset < numItemsInDict; offset++)
                {
                    nodeOffsets[offset] = tempReader.ReadInt64();
                }
            }

            long lastAllocationOffset = 0;
            int lastAllocatedNodeIdx = -1;

            Dictionary<UnsafeNodeId, UnsafeNode> allNodes = new Dictionary<UnsafeNodeId, UnsafeNode>();

            PooledBuffer<byte> scratch = BufferPool<byte>.Rent();
            try
            {
                while (lastAllocatedNodeIdx < numItemsInDict - 1)
                {
                    // 1 MB is approximately 4096 nodes
                    int targetAllocationNodeIdx = Math.Min(numItemsInDict - 1, lastAllocatedNodeIdx + 4096);
                    int allocationSize = (int)(nodeOffsets[targetAllocationNodeIdx] - lastAllocationOffset);

                    if (scratch.Buffer.Length < allocationSize)
                    {
                        scratch.Dispose();
                        scratch = BufferPool<byte>.Rent(allocationSize);
                    }

                    // Read the raw data
                    await inputStream.ReadExactlyAsync(scratch.Buffer.AsMemory(0, allocationSize));

                    unsafe
                    {
                        byte* ptr = (byte*)storageHeap.Allocate(allocationSize);
                        new Span<byte>(scratch.Buffer, 0, allocationSize).CopyTo(new Span<byte>(ptr, allocationSize));

                        // Build pointers over it
                        while (lastAllocatedNodeIdx < targetAllocationNodeIdx)
                        {
                            int thisNodeOffset = lastAllocatedNodeIdx < 0 ? 0 : (int)(nodeOffsets[lastAllocatedNodeIdx] - lastAllocationOffset);
                            UnsafeNode node = new UnsafeNode(ptr + thisNodeOffset);
                            allNodes.Add(node.NodeId, node);
                            lastAllocatedNodeIdx++;
                        }

                        lastAllocationOffset = nodeOffsets[targetAllocationNodeIdx];
                    }
                }

                return new UnsafeReadOnlyKnowledgeGraph(allNodes);
            }
            finally
            {
                scratch.Dispose();
            }
        }

        private UnsafeReadOnlyKnowledgeGraph(Dictionary<UnsafeNodeId, UnsafeNode> nodes)
        {
            _nodes = nodes.ToFrozenDictionary();
        }

        private static UnsafeNodeId ConvertToUnsafeNodeId(KnowledgeGraphNodeId input, NativeMemoryHeap storageHeap)
        {
            // todo
            return new UnsafeNodeId();
        }


        // Graph file format:
        // uint16 edgeCapacity
        // int32 numItemsInDict
        // int64[numItemsInDict] structEndAddresses
        // Node[numItemsInDict]

        // Node:
        // NodeId id
        // EdgeList edges

        // NodeId:
        // uint16 type
        // uint8 nameLength
        // uint16? extendedNameLength
        // byte[nameLength] name

        // EdgeList:
        // float32 totalMass
        // uint16 numEdges
        // Edge[numEdges]

        // Edge:
        // float32 mass
        // NodeId target
        private unsafe struct UnsafeNode
        {
            public byte* _ptr;

            public UnsafeNode(byte* ptr)
            {
                _ptr = ptr;
            }

            public UnsafeNodeId NodeId => new UnsafeNodeId(_ptr);

            public UnsafeEdgeList Edges => new UnsafeEdgeList(_ptr + NodeId.SizeOf);
        }

        private unsafe struct UnsafeEdgeList
        {
            public byte* _ptr;

            public UnsafeEdgeList(byte* ptr)
            {
                _ptr = ptr;
            }

            public float TotalMass => *((float*)_ptr);

            public ushort NumEdges => *(ushort*)(&_ptr[4]);

            // todo implement edge enumeration
        }

        private unsafe struct UnsafeEdge
        {
            public byte* _ptr;

            public UnsafeEdge(byte* ptr)
            {
                _ptr = ptr;
            }

            public float Mass => *((float*)_ptr);

            public UnsafeNodeId Target => new UnsafeNodeId(_ptr + 4);
        }

        private unsafe struct UnsafeNodeId
        {
            public byte* _ptr;

            public UnsafeNodeId(byte* ptr)
            {
                _ptr = ptr;
            }

            public KnowledgeGraphNodeType Type => (KnowledgeGraphNodeType)((ushort*)_ptr)[0];

            public ReadOnlySpan<byte> Name
            {
                get
                {
                    int nameLength = NameLength;
                    return new ReadOnlySpan<byte>(&_ptr[nameLength > 254 ? 5 : 3], nameLength);
                }
            }

            public int NameLength
            {
                get
                {
                    int returnVal = _ptr[2];
                    if (returnVal == 255)
                    {
                        returnVal = *(ushort*)(&_ptr[3]);
                    }

                    return returnVal;
                }
            }

            public int SizeOf
            {
                get
                {
                    int returnVal = 3 + _ptr[2];
                    if (returnVal == 255)
                    {
                        returnVal = 5 + *(ushort*)(&_ptr[3]);
                    }

                    return returnVal;
                }
            }

            public override bool Equals(object? obj)
            {
                if (obj == null || GetType() != obj.GetType())
                {
                    return false;
                }

                UnsafeNodeId other = (UnsafeNodeId)obj;
                return Type == other.Type && Name.SequenceEqual(other.Name);
            }

            public override int GetHashCode()
            {
                // TODO don't know how to invoke the platform built-in hasher using ReadOnlySpan<byte>
                // since I recall that was explicitly disabled in runtime
                MurmurHash3_32 hasher = new MurmurHash3_32(5137);
                hasher.Ingest(Name);
                return (int)hasher.Finish();
            }
        }
    }
}
