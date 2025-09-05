using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Graph
{
    public unsafe class NativeMemoryHeap : IDisposable
    {
        private int _disposed = 0;
        private List<nuint> _blocks = new List<nuint>();

        ~NativeMemoryHeap()
        {
            Dispose(disposing: false);
        }

        public void* Allocate(int bytesToAllocate)
        {
            void* ptr = NativeMemory.Alloc((nuint)bytesToAllocate);
            _blocks.Add((nuint)ptr);
            return ptr;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            {
                return;
            }

            foreach (nuint pointer in _blocks)
            {
                void* ptr = (void*)pointer;
                NativeMemory.Free(ptr);
            }
        }
    }
}
