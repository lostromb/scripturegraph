using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;

namespace ScriptureGraph.Core.Training
{
    public class TrainingLockArray
    {
        private uint _numLocks;
        private object[] _locks;

        public TrainingLockArray(int numLocks)
        {
            _numLocks = (uint)numLocks.AssertPositive(nameof(numLocks));
            _locks = new object[numLocks];
            for (int c = 0; c < numLocks; c++)
            {
                _locks[c] = new object();
            }
        }

        public void GetAllLocks()
        {
            for (int c = 0; c < _numLocks; c++)
            {
                Monitor.Enter(_locks[c]);
            }
        }

        public void ReleaseAllLocks()
        {
            for (int c = 0; c < _numLocks; c++)
            {
                Monitor.Exit(_locks[c]);
            }
        }

        public void GetLock(ref KnowledgeGraphNodeId node)
        {
            uint bin = ((uint)node.GetHashCode()) % _numLocks;
            Monitor.Enter(_locks[bin]);
        }

        public void ReleaseLock(ref KnowledgeGraphNodeId nodeA)
        {
            uint bin = ((uint)nodeA.GetHashCode()) % _numLocks;
            Monitor.Exit(_locks[bin]);
        }

        //public void GetLocks(ref KnowledgeGraphNodeId nodeA, ref KnowledgeGraphNodeId nodeB)
        //{
        //    uint bin1 = ((uint)nodeA.GetHashCode()) % _numLocks;
        //    uint bin2 = ((uint)nodeB.GetHashCode()) % _numLocks;
        //    if (bin1 == bin2)
        //    {
        //        // Shared lock for both nodes.
        //        Monitor.Enter(_locks[bin1]);
        //        return;
        //    }

        //    // Enforce resource ordering to prevent deadlock
        //    if (bin1 > bin2)
        //    {
        //        uint swap = bin1;
        //        bin1 = bin2;
        //        bin2 = swap;
        //    }

        //    Monitor.Enter(_locks[bin1]);
        //    Monitor.Enter(_locks[bin2]);
        //}

        //public void ReleaseLocks(ref KnowledgeGraphNodeId nodeA, ref KnowledgeGraphNodeId nodeB)
        //{
        //    uint bin1 = ((uint)nodeA.GetHashCode()) % _numLocks;
        //    uint bin2 = ((uint)nodeB.GetHashCode()) % _numLocks;
        //    if (bin1 == bin2)
        //    {
        //        // Shared lock for both nodes.
        //        Monitor.Exit(_locks[bin1]);
        //        return;
        //    }

        //    // Enforce resource ordering to prevent deadlock
        //    if (bin1 > bin2)
        //    {
        //        uint swap = bin1;
        //        bin1 = bin2;
        //        bin2 = swap;
        //    }

        //    Monitor.Exit(_locks[bin1]);
        //    Monitor.Exit(_locks[bin2]);
        //}
    }
}
