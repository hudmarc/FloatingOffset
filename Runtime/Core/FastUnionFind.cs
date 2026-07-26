using System;
using System.Runtime.CompilerServices;

namespace FloatingOffset.Runtime
{
    public sealed class FastUnionFind
    {
        public int[] unions;

        public ref int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref unions[index];
        }
        public FastUnionFind(int initialCapacity)
        {
            unions = new int[0];
            EnsureCapacity(initialCapacity);
        }

        public void EnsureCapacity(int count)
        {
            if (unions.Length < count)
            {
                int oldLength = unions.Length;
                int newSize = count * 2;
                Array.Resize(ref unions, newSize);

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
        /// Merges j's union into i's union. Representative will be union i's representative.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Union(int i, int j)
        {
            int rootI = Find(i);
            int rootJ = Find(j);
            if (rootI != rootJ)
            {
                unions[rootJ] = rootI; // Attach J's tree to I
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
