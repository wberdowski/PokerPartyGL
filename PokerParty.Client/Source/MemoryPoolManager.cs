using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PokerParty.Client
{
    public interface IMemoryAllocable
    {
        int MemorySizeBytes { get; }
        Array MemoryData { get; }
    }

    public class MemoryPoolManager<T> where T : class, IMemoryAllocable
    {
        public List<MemoryFragment<T>> fragments = new List<MemoryFragment<T>>();
        public int BufferSizeInBytes { get; private set; }
        public int FreeBufferSizeInBytes { get; private set; }

        public delegate void ReallocateCopy(int oldSize, int newSize);
        public delegate void ConsolidateCopy(int oldOffset, int newOffset, int size);
        public delegate void ConsolidateSetupBuffer(int newSize);

        public MemoryPoolManager(int freeBufferSizeInBytes)
        {
            FreeBufferSizeInBytes = freeBufferSizeInBytes;
        }

        public void MemoryInit(IEnumerable<T> data)
        {
            // Debug.WriteLine("> MemoryInit");
            // Debug.WriteLine("\tFragments #: " + data.Count());
            int offset = 0;

            foreach (var d in data)
            {
                int size = d.MemorySizeBytes;
                fragments.Add(new MemoryFragment<T>(offset, size, d));
                offset += size;
            }

            fragments.Add(new MemoryFragment<T>(offset, FreeBufferSizeInBytes, null));

            BufferSizeInBytes = offset + FreeBufferSizeInBytes;

            // Debug.WriteLine("\tBUFFER SIZE: {0:n0}", BufferSizeInBytes);
            // Debug.WriteLine();
        }

        public byte[] GenData()
        {
            var data = new byte[BufferSizeInBytes];

            foreach (var f in fragments)
            {
                if (f.Pointer != null)
                {
                    System.Buffer.BlockCopy(f.Pointer.MemoryData, 0, data, f.OffsetBytes, f.Pointer.MemorySizeBytes);
                }
            }

            return data;
        }

        public void MemoryRemove(T data)
        {
            // Debug.WriteLine("> MemoryRemove: " + data.ToString());

            var fragment = fragments.Where(x => x.Pointer == data).DefaultIfEmpty(null).FirstOrDefault();

            if (fragment != null)
            {
                // Debug.WriteLine("\t Deallocating: " + fragment);
                fragment.Pointer = null;

                var idx = fragments.IndexOf(fragment);

                var leftFree = idx - 1 >= 0 && fragments[idx - 1].Pointer == null;
                var rightFree = idx + 1 < fragments.Count && fragments[idx + 1].Pointer == null;

                if (leftFree && rightFree)
                {
                    // Merge both
                    MergeFragments(idx - 1, idx);
                    MergeFragments(idx - 1, idx);
                }
                else if (leftFree)
                {
                    // Merge left
                    MergeFragments(idx - 1, idx);
                }
                else if (rightFree)
                {
                    // Merge right
                    MergeFragments(idx, idx + 1);
                }

                // Debug.WriteLine();
            }
        }

        public void MergeFragments(int idx1, int idx2)
        {
            // Debug.WriteLine("> Merge: " + idx1 + "+" + idx2);

            var s2 = fragments[idx2].SizeInBytes;
            fragments.RemoveAt(idx2);
            fragments[idx1].SizeInBytes += s2;

        }

        public MemoryFragment<T> MemoryAdd(T data, ReallocateCopy reallocAction)
        {
            // Debug.WriteLine("> MemoryAdd: " + data);

            int size = data.MemorySizeBytes;
            var fragment = MemoryAlloc(size, reallocAction);
            int oldSize = fragment.SizeInBytes;

            fragment.SizeInBytes = size;
            fragment.Pointer = data;

            if (oldSize > size)
            {
                int before = fragments.IndexOf(fragment);
                fragments.Insert(before + 1, new MemoryFragment<T>(fragment.OffsetBytes + size, oldSize - size, null));
            }

            return fragment;
        }

        public void MemoryConsolidate(ConsolidateSetupBuffer setupBuffer, ConsolidateCopy consolidateAction)
        {
            Debug.WriteLine("> Consolidate memory");
            var oldSize = BufferSizeInBytes;
            var empty = fragments.Where(x => x.Pointer == null).ToList();

            // Remove empty fragments
            foreach (var f in empty)
            {
                fragments.Remove(f);
                BufferSizeInBytes -= f.SizeInBytes;
            }

            BufferSizeInBytes += FreeBufferSizeInBytes;

            setupBuffer(BufferSizeInBytes);

            // Recalculate offsets
            int offset = 0;
            foreach (var f in fragments)
            {
                var oldOffset = f.OffsetBytes;
                f.OffsetBytes = offset;

                consolidateAction(oldOffset, offset, f.SizeInBytes);

                offset += f.SizeInBytes;
            }

            // Add free buffer at the end
            fragments.Add(new MemoryFragment<T>(offset, FreeBufferSizeInBytes, null));

            Debug.WriteLine("\tBUFFER SIZE: {0:n0} -> {1:n0} (Saved {2:n0} bytes))", oldSize, BufferSizeInBytes, oldSize - BufferSizeInBytes);
            Debug.WriteLine("");
        }

        public MemoryFragment<T> MemoryAlloc(int allocSize, ReallocateCopy reallocAction)
        {
            // Debug.WriteLine("> MemoryAlloc: Size: {0:n0}", allocSize);

            bool found = false;
            MemoryFragment<T> fragment = null;

            foreach (var s in fragments)
            {
                if (s.Pointer == null && s.SizeInBytes >= allocSize && (fragment == null || s.SizeInBytes < fragment.SizeInBytes))
                {
                    fragment = s;
                    found = true;
                }
            }

            if (found)
            {
                // Allocate in fragment
                // Debug.WriteLine("\tFound: Off: " + fragment.OffsetBytes);
                return fragment;
            }

            // Debug.WriteLine("No free fragments. Reallocating buffer...");
            // Debug.WriteLine();

            MemoryReallocate(BufferSizeInBytes + allocSize, reallocAction);

            return fragments.Last();
        }

        public void MemoryReallocate(int newSize, ReallocateCopy reallocAction)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Debug.WriteLine($"> Reallocate: {BufferSizeInBytes:n0} -> {newSize:n0}");

            var oldSize = BufferSizeInBytes;

            if (newSize < BufferSizeInBytes)
            {
                throw new NotSupportedException("New buffer size cannot be smaller than old size.");
            }

            var last = fragments.DefaultIfEmpty(null).LastOrDefault();

            if (last == null)
            {
                BufferSizeInBytes = newSize + FreeBufferSizeInBytes;
            }
            else
            {
                if (last.Pointer == null)
                {
                    // RESIZE
                    var oldFragSize = last.SizeInBytes;
                    var newFragSize = last.SizeInBytes = Math.Max(FreeBufferSizeInBytes, newSize - BufferSizeInBytes + fragments.Last().SizeInBytes);
                    BufferSizeInBytes += newFragSize - oldFragSize;
                }
                else
                {
                    // ADD
                    var size = FreeBufferSizeInBytes + newSize - BufferSizeInBytes;
                    fragments.Add(new MemoryFragment<T>(last.OffsetBytes + last.SizeInBytes, size, null));
                    BufferSizeInBytes += size;
                }
            }

            reallocAction(oldSize, BufferSizeInBytes);

            Debug.WriteLine("\tNew buffer size: {0:n0}", BufferSizeInBytes);
            Debug.WriteLine("");
            Console.ResetColor();
        }

        public void PrintMemoryFragments()
        {
            int idx = 0;
            // Debug.WriteLine("> Memory fragments:");
            foreach (var s in fragments)
            {
                // Debug.WriteLine($"\t[{idx}] {s}");
                idx++;
            }
            // Debug.WriteLine();

            //foreach (var s in fragments)
            //{
            //    // Debug.WriteLine("    " + new string(' ', s.OffsetBytes) + s.OffsetBytes.ToString().PadRight(s.SizeBytes, '/'));
            //}

            //// Debug.WriteLine();
            //// Console.Write("    ");

            //bool even = true;

            //foreach (var s in fragments)
            //{
            //    if (s.Pointer != null)
            //    {
            //        // Console.BackgroundColor = (even ? ConsoleColor.Red : ConsoleColor.DarkRed);
            //    } else
            //    {
            //        // Console.BackgroundColor = (even ? ConsoleColor.Green : ConsoleColor.DarkGreen);
            //    }

            //    // Console.Write(new string(' ', s.SizeBytes));

            //    even = !even;
            //}

            //// Console.ResetColor();
            //// Debug.WriteLine();
            // Debug.WriteLine();
        }

        public class MemoryFragment<T>
        {
            public int OffsetBytes { get; set; }
            public int SizeInBytes { get; set; }
            public T Pointer { get; set; }

            public MemoryFragment(int offset, int size, T pointer)
            {
                OffsetBytes = offset;
                SizeInBytes = size;
                Pointer = pointer;
            }

            public override string ToString()
            {
                return $"Off: {OffsetBytes}, Size: {SizeInBytes:n0}, Ptr: {(Pointer != null ? Pointer.ToString() : "<null>")}";
            }
        }
    }
}
