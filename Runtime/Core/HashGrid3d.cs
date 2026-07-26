using System;
using System.Runtime.CompilerServices;
using UnityEngine;
namespace FloatingOffset.Runtime
{
    // Experimental fast hash grid. Implementation details may change.
    public class HashGrid
    {
        private const uint PrimeX = 73856093;
        private const uint PrimeY = 19349663;
        private const uint PrimeZ = 83492791;

        private int bucketMask;
        private int[] buckets;

        private int[] entries_value;
        private int[] entries_next;

        private int entry_count;

        // Unified variable for cell sizing
        private double invCellSize;
        private double searchRadiusSq;

        public HashGrid(int bucketCount, int initialCapacity, double maxSearchRadius)
        {
            buckets = new int[bucketCount];
            bucketMask = bucketCount - 1; // Requires bucketCount to be a power of 2

            entries_value = new int[initialCapacity];
            entries_next = new int[initialCapacity];

            Array.Fill(buckets, -1);

            // The cell size MUST be exactly double the search radius for the 8-neighbor logic to hold true
            invCellSize = 1.0 / (maxSearchRadius * 2.0);
            searchRadiusSq = maxSearchRadius * maxSearchRadius;
        }

        public void Clear()
        {
            Array.Fill(buckets, -1);
            entry_count = 0;
        }

        private void EnsureCapacity()
        {
            if (entry_count >= entries_value.Length)
            {
                int newSize = entries_value.Length * 2;
                Array.Resize(ref entries_value, newSize);
                Array.Resize(ref entries_next, newSize);
            }
        }

        // Mathematically correct floor that handles negative numbers safely
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FastFloor(double t)
        {
            int val = (int)t;
            return t < val ? val - 1 : val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int HashGridPosition(int gridX, int gridY, int gridZ)
        {
            uint h = (uint)(gridX * PrimeX ^ gridY * PrimeY ^ gridZ * PrimeZ);
            return (int)(h & bucketMask);
        }

        public Vector3Int ToGrid(Vector3d vector)
        {
            int gridX = FastFloor(vector.x * invCellSize);
            int gridY = FastFloor(vector.y * invCellSize);
            int gridZ = FastFloor(vector.z * invCellSize);
            return new Vector3Int(gridX, gridY, gridZ);
        }

        public void Add(Vector3d position, int viewIndex)
        {
            EnsureCapacity();

            // Calculate correct grid coordinates for negative/positive space
            int gridX = FastFloor(position.x * invCellSize);
            int gridY = FastFloor(position.y * invCellSize);
            int gridZ = FastFloor(position.z * invCellSize);

            int hash = HashGridPosition(gridX, gridY, gridZ);

            entries_value[entry_count] = viewIndex;
            entries_next[entry_count] = buckets[hash];
            buckets[hash] = entry_count;
            entry_count++;
        }

        // Passed view_positions array so TraverseBucket can do precise checks
        public void FindNeighbors(Vector3d position, Vector3d[] view_positions, ref int[] resultsBuffer, out int resultCount, int myRoot = -1, int[] union_reps = default)
        {
            resultCount = 0;

            // Scale the position
            double scaledX = position.x * invCellSize;
            double scaledY = position.y * invCellSize;
            double scaledZ = position.z * invCellSize;

            // Get the Home Cell (Grid Coordinates) using FastFloor
            int gridX = FastFloor(scaledX);
            int gridY = FastFloor(scaledY);
            int gridZ = FastFloor(scaledZ);

            // Find the fractional position (0.0 to 1.0) inside the home cell
            double fracX = scaledX - gridX;
            double fracY = scaledY - gridY;
            double fracZ = scaledZ - gridZ;

            int dx = fracX < 0.5 ? -1 : 1;
            int dy = fracY < 0.5 ? -1 : 1;
            int dz = fracZ < 0.5 ? -1 : 1;

            int x0 = gridX, x1 = gridX + dx;
            int y0 = gridY, y1 = gridY + dy;
            int z0 = gridZ, z1 = gridZ + dz;

            int hash0 = HashGridPosition(x0, y0, z0);
            int hash1 = HashGridPosition(x1, y0, z0);
            int hash2 = HashGridPosition(x0, y1, z0);
            int hash3 = HashGridPosition(x1, y1, z0);
            int hash4 = HashGridPosition(x0, y0, z1);
            int hash5 = HashGridPosition(x1, y0, z1);
            int hash6 = HashGridPosition(x0, y1, z1);
            int hash7 = HashGridPosition(x1, y1, z1);

            // Pass the view_positions array down the chain
            TraverseBucket(hash0, position, view_positions, myRoot, union_reps, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash1, position, view_positions, myRoot, union_reps, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash2, position, view_positions, myRoot, union_reps, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash3, position, view_positions, myRoot, union_reps, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash4, position, view_positions, myRoot, union_reps, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash5, position, view_positions, myRoot, union_reps, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash6, position, view_positions, myRoot, union_reps, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash7, position, view_positions, myRoot, union_reps, ref resultsBuffer, ref resultCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TraverseBucket(int bucketHash, Vector3d searchPos, Vector3d[] view_positions, int myRoot, int[] union_reps, ref int[] resultsBuffer, ref int resultCount)
        {
            int entryIndex = buckets[bucketHash];

            while (entryIndex != -1)
            {
                int viewIndex = entries_value[entryIndex];

                int neighborRoot = viewIndex;
                // traverse up union-find
                while (union_reps != null && neighborRoot != union_reps[neighborRoot])
                {
                    neighborRoot = union_reps[neighborRoot];
                }

                if (myRoot == neighborRoot)
                {
                    // O(1) Early Exit. We do not care how close they are.
                    entryIndex = entries_next[entryIndex];
                    continue;
                }

                // Precise distance check using the external positions array
                Vector3d targetPos = view_positions[viewIndex];

                // Manual sqrMagnitude (assuming you have this property on Vector3d, if not, write it out)
                double diffX = searchPos.x - targetPos.x;
                double diffY = searchPos.y - targetPos.y;
                double diffZ = searchPos.z - targetPos.z;
                double distSq = (diffX * diffX) + (diffY * diffY) + (diffZ * diffZ);

                if (distSq <= searchRadiusSq)
                {
                    // guard clause
                    if (resultCount >= resultsBuffer.Length)
                    {
                        Array.Resize(ref resultsBuffer, resultsBuffer.Length * 2);
                    }

                    resultsBuffer[resultCount++] = viewIndex;
                }

                entryIndex = entries_next[entryIndex];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int FindAnyInGrid(Vector3d position, Vector3d[] view_positions)
        {
            // Scale the position
            double scaledX = position.x * invCellSize;
            double scaledY = position.y * invCellSize;
            double scaledZ = position.z * invCellSize;

            // Get the Home Cell (Grid Coordinates) using FastFloor
            int gridX = FastFloor(scaledX);
            int gridY = FastFloor(scaledY);
            int gridZ = FastFloor(scaledZ);

            // Get the hash for the home cell
            int bucketHash = HashGridPosition(gridX, gridY, gridZ);

            int entryIndex = buckets[bucketHash];

            while (entryIndex != -1)
            {
                int viewIndex = entries_value[entryIndex];
                Vector3d targetPos = view_positions[viewIndex];

                double diffX = position.x - targetPos.x;
                double diffY = position.y - targetPos.y;
                double diffZ = position.z - targetPos.z;
                double distSq = (diffX * diffX) + (diffY * diffY) + (diffZ * diffZ);

                if (distSq <= searchRadiusSq)
                {
                    // Return the first found result
                    return viewIndex;
                }

                entryIndex = entries_next[entryIndex];
            }

            // Return -1 if no valid item is found in this grid cell
            return -1;
        }
    }
}
