using System;
using System.Runtime.CompilerServices;

namespace FloatingOffset.Runtime
{
    public sealed class FastUnionFind
    {
        byte[] ranks;
        public int[] unions;

        public ref int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref unions[index];
        }
        public FastUnionFind(int initialCapacity)
        {
            unions = new int[0];
            ranks = new byte[0];
            EnsureCapacity(initialCapacity);
        }

        public void EnsureCapacity(int count)
        {
            if (unions.Length < count)
            {
                int oldLength = unions.Length;
                int newSize = count * 2;
                Array.Resize(ref unions, newSize);
                Array.Resize(ref ranks, newSize);

                for (int i = oldLength; i < newSize; i++)
                {
                    unions[i] = i;
                }
            }
        }

        // Force the compiler to paste this inside your loop
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int member_index)
        {
            unions[member_index] = Find(member_index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Find(int i)
        {
            int root = i;
            // Find the root
            while (root != unions[root])
                root = unions[root];

            // Path compression: make all nodes on the path point directly to root
            int current = i;
            while (current != root)
            {
                int next = unions[current];
                unions[current] = root;
                current = next;
            }
            return root;
        }

        /// <summary>
        /// Merges j's union into i's union using Union-by-Rank
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Union(int i, int j)
        {
            int rootI = Find(i);
            int rootJ = Find(j);

            if (rootI != rootJ)
            {
                // Attach the shorter tree to the taller tree
                if (ranks[rootI] < ranks[rootJ])
                {
                    unions[rootI] = rootJ;
                }
                else if (ranks[rootI] > ranks[rootJ])
                {
                    unions[rootJ] = rootI;
                }
                else
                {
                    // If they are the same height, pick one arbitrarily and increase its rank
                    unions[rootJ] = rootI;
                    ranks[rootI]++;
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i < unions.Length; i++)
            {
                unions[i] = i;
            }
        }
    }
}
