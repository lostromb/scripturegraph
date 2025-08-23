using Durandal.Common.Utils;
using ScriptureGraph.Core.Graph;

namespace ScriptureGraph.Core.Training
{
    public class TrainingLockArray
    {
        private int _numLocks;
        private object[] _locks;

        public TrainingLockArray(int numLocks)
        {
            _numLocks = numLocks.AssertPositive(nameof(numLocks));
            _locks = new object[numLocks];
        }

        public void GetLocks(ref KnowledgeGraphNodeId nodeA, ref KnowledgeGraphNodeId nodeB)
        {
            int bin1 = nodeA.GetHashCode() % _numLocks;
            int bin2 = nodeB.GetHashCode() % _numLocks;
            if (bin1 == bin2)
            {
                // Shared lock for both nodes.
                Monitor.Enter(_locks[bin1]);
                return;
            }

            // Enforce resource ordering to prevent deadlock
            if (bin1 > bin2)
            {
                int swap = bin1;
                bin1 = bin2;
                bin2 = swap;
            }

            Monitor.Enter(_locks[bin1]);
            Monitor.Enter(_locks[bin2]);
        }

        public void ReleaseLock(ref KnowledgeGraphNodeId nodeA, ref KnowledgeGraphNodeId nodeB)
        {
            int bin1 = nodeA.GetHashCode() % _numLocks;
            int bin2 = nodeB.GetHashCode() % _numLocks;
            if (bin1 == bin2)
            {
                // Shared lock for both nodes.
                Monitor.Exit(_locks[bin1]);
                return;
            }

            // Enforce resource ordering to prevent deadlock
            if (bin1 > bin2)
            {
                int swap = bin1;
                bin1 = bin2;
                bin2 = swap;
            }

            Monitor.Exit(_locks[bin1]);
            Monitor.Exit(_locks[bin2]);
        }
    }
}
