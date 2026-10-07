using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace FloatingOffset.Runtime
{
    /// <summary>
    /// Fast HashGrid for finding grid cell representatives.
    /// </summary>
    public class HashGrid
    {
        struct Neighbourhood
        {
            public int representative_value;
            public float largest_relative_radius_squared;
            public Vector3d representative_position;
        }
        private const uint PrimeX = 73856093;
        private const uint PrimeY = 19349663;
        private const uint PrimeZ = 83492791;
        private double resolution_inverse;
        private int max_neighbourhood_radius_squared;
        private int max_neighbourhood_radius;
        private int search_radius;
        private int search_radius_squared;
        // Key is quantized grid position, value is representative
        private readonly Dictionary<Vector3d, Neighbourhood> dict = new Dictionary<Vector3d, Neighbourhood>();
        /// <summary>
        /// Create a HashGrid with the given resolution and maximum approximate neighbor radius.
        /// </summary>
        /// <param name="search_radius"> The maximum radius under which two points are considered neighbours. In other words, the edge length of the grid cubes.</param>
        /// <param name="max_neighbourhood_radius">The maximum approximate radius (+/- 1 edge length in practice) of a single neighbourhood.</param>
        public HashGrid(int search_radius, int max_neighbourhood_radius)
        {
            this.search_radius = search_radius;
            this.search_radius_squared = search_radius * search_radius;
            this.resolution_inverse = 1d / search_radius;
            this.max_neighbourhood_radius_squared = max_neighbourhood_radius * max_neighbourhood_radius;
            this.max_neighbourhood_radius = max_neighbourhood_radius;
        }
        public static readonly Vector3d[] SEARCH_PATTERN = {
                new Vector3d(0,0,0),
                new Vector3d(0,0,1),
                new Vector3d(1,0,1),

                new Vector3d(1,0,0),
                new Vector3d(1,0,-1),
                new Vector3d(0,0,-1),

                new Vector3d(-1,0,-1),
                new Vector3d(-1,0,0),
                new Vector3d(-1,0,1),


                new Vector3d(-1,1,1),
                new Vector3d(0,1,1),
                new Vector3d(1,1,1),

                new Vector3d(1,1,0),
                new Vector3d(1,1,-1),
                new Vector3d(0,1,-1),

                new Vector3d(-1,1,-1),
                new Vector3d(-1,1,0),
                new Vector3d(0,1,0),


                new Vector3d(0,-1,0),
                new Vector3d(0,-1,1),
                new Vector3d(1,-1,1),

                new Vector3d(1,-1,0),
                new Vector3d(1,-1,-1),
                new Vector3d(0,-1,-1),

                new Vector3d(-1,-1,-1),
                new Vector3d(-1,-1,0),
                new Vector3d(-1,-1,1),

             };

        /// <summary>
        /// Get the count of unique grid squares with representatives.
        /// </summary>
        public int Count { get => dict.Count; }
        /// <summary>
        /// Add the given value at the given vector as a representative. <code>O(1)</code>
        /// Returns <code>true</code> if the value would be added to an existing representative, updating its radius. Returns <code>false</code> otherwise.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="value"></param>
        public int AddToRepresentative(Vector3d vector, int value)
        {
            Vector3d key = Quantize(vector);
            if (dict.TryGetValue(key, out Neighbourhood found))
            {
                float squared_distance = (float)Vector3d.SquaredMagnitude(vector - found.representative_position);

                if (squared_distance > max_neighbourhood_radius_squared)
                    return value;

                if (squared_distance > found.largest_relative_radius_squared)
                {
                    Neighbourhood mutated = new Neighbourhood()
                    {
                        representative_value = found.representative_value,
                        largest_relative_radius_squared = squared_distance,
                        representative_position = found.representative_position
                    };
                    dict[key] = mutated;
                }
                return found.representative_value;
            }

            Neighbourhood neighbourhood = new Neighbourhood()
            {
                representative_value = value,
                largest_relative_radius_squared = 0,
                representative_position = vector
            };
            dict.Add(key, neighbourhood);

            return value;
        }

        /// <summary>
        /// Does a representative exist at the given vector? <code>O(1)</code>
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public bool HasRepresentative(Vector3d vector) => dict.ContainsKey(Quantize(vector));

        public void Clear() => dict.Clear();
        /// <summary>
        /// Returns first found representative starting at the given grid square, proceeding to its 27 neighbors. If nothing was found, returns -1.
        /// </summary>
        /// <param name="center">Where to search.</param>
        /// <returns></returns>
        public int FindRepresentative(Vector3d center)
        {
            Vector3d key = Quantize(center);

            for (int i = 0; i < SEARCH_PATTERN.Length; i++)
            {
                if (dict.TryGetValue(key + SEARCH_PATTERN[i], out Neighbourhood neighbourhood))
                {
                    float c = 2 * search_radius * MathF.Sqrt(neighbourhood.largest_relative_radius_squared);

                    if (Vector3d.SquaredMagnitude(center - neighbourhood.representative_position) <= search_radius_squared + neighbourhood.largest_relative_radius_squared + c) // (a+b)^2 = a^2 + b^2 + 2ab
                        return neighbourhood.representative_value;
                }
            }
            return -1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3d Quantize(Vector3d vector)
        {
            return new Vector3d(SymmetricFloor(vector.x * resolution_inverse), SymmetricFloor(vector.y * resolution_inverse), SymmetricFloor(vector.z * resolution_inverse));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d Quantize(Vector3d vector, double resolution)
        {
            double resolution_inverse = 1 / resolution;
            return new Vector3d(SymmetricFloor(vector.x * resolution_inverse), SymmetricFloor(vector.y * resolution_inverse), SymmetricFloor(vector.z * resolution_inverse));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SymmetricFloor(double t)
        {
            double val = Math.Floor(Math.Abs(t));
            return t < 0 ? -val : val;
        }
    }
}
