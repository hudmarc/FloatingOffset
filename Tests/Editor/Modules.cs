using System;
using FloatingOffset.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace FloatingOffset.Editor.Tests
{
    // this test partially machine-generated based off spec in HashGrid3d.cs
    public class Modules
    {
        private const bool SKIP_BENCH = false;

        // Grid squares are 10 units wide in these tests.
        private const int SEARCH_RADIUS = 10;
        private const int MAX_NEIGHBOURHOOD_RADIUS = 100;

        private static HashGrid NewGrid() => new HashGrid(SEARCH_RADIUS, MAX_NEIGHBOURHOOD_RADIUS);

        [Test]
        public void HashGridInitialization()
        {
            HashGrid test = NewGrid();
            Assert.NotNull(test);
            Assert.AreEqual(0, test.Count);
        }

        [Test]
        public void EmptyGrid_HasNoRepresentatives()
        {
            HashGrid grid = NewGrid();
            Assert.IsFalse(grid.HasRepresentative(new Vector3d(5, 5, 5)));
            Assert.AreEqual(-1, grid.FindRepresentative(new Vector3d(5, 5, 5)));
        }

        [Test]
        public void TryAdd_NewSquare_ReturnsFalse_AndIncrementsCount()
        {
            HashGrid grid = NewGrid();

            bool joinedExisting = grid.AddToRepresentative(new Vector3d(5, 5, 5), 1) != 1;

            Assert.IsFalse(joinedExisting, "First add creates a new representative.");
            Assert.AreEqual(1, grid.Count);
            Assert.IsTrue(grid.HasRepresentative(new Vector3d(5, 5, 5)));
        }

        [Test]
        public void TryAdd_SameSquare_ReturnsTrue_AndDoesNotIncrementCount()
        {
            HashGrid grid = NewGrid();
            grid.AddToRepresentative(new Vector3d(1, 1, 1), 1);

            bool joinedExisting = grid.AddToRepresentative(new Vector3d(9, 9, 9), 2) != 2;

            Assert.IsTrue(joinedExisting, "Same grid square should join the existing representative.");
            Assert.AreEqual(1, grid.Count);
        }

        [Test]
        public void TryAdd_DifferentSquares_CreatesSeparateRepresentatives()
        {
            HashGrid grid = NewGrid();

            Debug.Log(grid.Quantize(new Vector3d(5, 5, 5)));
            Debug.Log(grid.Quantize(new Vector3d(15, 5, 5)));
            Debug.Log(grid.Quantize(new Vector3d(5, 15, 5)));
            Debug.Log(grid.Quantize(new Vector3d(5, 5, 15)));

            Debug.Log(Math.Floor(15d * (1d / SEARCH_RADIUS)));


            Assert.AreEqual(grid.AddToRepresentative(new Vector3d(5, 5, 5), 1), 1);
            Debug.Log(grid.FindRepresentative(new Vector3d(15, 5, 5)));
            Assert.IsFalse(grid.HasRepresentative(new Vector3d(15, 5, 5)));
            Assert.AreEqual(grid.AddToRepresentative(new Vector3d(15, 5, 5), 2), 2);
            Assert.AreEqual(grid.AddToRepresentative(new Vector3d(5, 15, 5), 3), 3);
            Assert.AreEqual(grid.AddToRepresentative(new Vector3d(5, 5, 15), 4), 4);

            Assert.AreEqual(4, grid.Count);
        }

        [Test]
        public void TryAdd_SameId_SamePosition_DoesNotThrow()
        {
            HashGrid grid = NewGrid();
            grid.AddToRepresentative(new Vector3d(5, 5, 5), 1);
            Assert.DoesNotThrow(() => grid.AddToRepresentative(new Vector3d(5, 5, 5), 1));
            Assert.AreEqual(1, grid.Count);
        }

        [Test]
        public void HasRepresentative_ChecksOnlyTheExactSquare()
        {
            HashGrid grid = NewGrid();
            grid.AddToRepresentative(new Vector3d(5, 5, 5), 1);

            Assert.IsTrue(grid.HasRepresentative(new Vector3d(0.1, 0.1, 0.1)));
            Assert.IsTrue(grid.HasRepresentative(new Vector3d(9.9, 9.9, 9.9)));
            Assert.IsFalse(grid.HasRepresentative(new Vector3d(10.1, 5, 5)),
                "Adjacent square has no representative of its own.");
        }

        [Test]
        public void FindRepresentative_SameSquare_ReturnsRepresentativeId()
        {
            HashGrid grid = NewGrid();
            grid.AddToRepresentative(new Vector3d(5, 5, 5), 42);

            Assert.AreEqual(42, grid.FindRepresentative(new Vector3d(6, 6, 6)));
        }

        [Test]
        public void FindRepresentative_SecondAddInSameSquare_KeepsFirstId()
        {
            // Assumes the first value added to a square stays its representative.
            HashGrid grid = NewGrid();
            grid.AddToRepresentative(new Vector3d(1, 1, 1), 7);
            grid.AddToRepresentative(new Vector3d(2, 2, 2), 8);

            Assert.AreEqual(7, grid.FindRepresentative(new Vector3d(5, 5, 5)));
        }

        [Test]
        public void FindRepresentative_FindsFaceEdgeAndCornerNeighbors()
        {
            HashGrid grid = NewGrid();
            grid.AddToRepresentative(new Vector3d(5, 5, 5), 1); // square (0,0,0)

            Debug.Log(grid.Quantize(new Vector3d(5, 5, 5)));
            Debug.Log(grid.Quantize(new Vector3d(15, 5, 5)));
            Debug.Log(grid.Quantize(new Vector3d(15, 15, 5)));
            Debug.Log(grid.Quantize(new Vector3d(15, 15, 15)));

            float largest_relative_radius_squared = (float)Vector3d.SquaredMagnitude(new Vector3d(5, 5, 5) - new Vector3d(15, 5, 5));

            float c = 2 * SEARCH_RADIUS * MathF.Sqrt(largest_relative_radius_squared);

            Debug.Log($"Search Radius: {(SEARCH_RADIUS * SEARCH_RADIUS) + largest_relative_radius_squared + c}");

            Debug.Log(Vector3d.SquaredMagnitude(new Vector3d(5, 5, 5) - new Vector3d(15, 5, 5)));
            Debug.Log(Vector3d.SquaredMagnitude(new Vector3d(5, 5, 5) - new Vector3d(15, 15, 5)));
            Debug.Log(Vector3d.SquaredMagnitude(new Vector3d(5, 5, 5) - new Vector3d(15, 15, 15)));

            Assert.AreEqual(1, grid.FindRepresentative(new Vector3d(15, 5, 5)), "face neighbor");
            Assert.AreEqual(-1, grid.FindRepresentative(new Vector3d(15, 15, 5)), "edge neighbor");
            Assert.AreEqual(-1, grid.FindRepresentative(new Vector3d(15, 15, 15)), "corner neighbor");

            grid.AddToRepresentative(new Vector3d(15, 5, 5), 1);

            Assert.AreEqual(1, grid.FindRepresentative(new Vector3d(15, 15, 5)), "edge neighbor");
            Assert.AreEqual(-1, grid.FindRepresentative(new Vector3d(15, 15, 15)), "corner neighbor");

            grid.AddToRepresentative(new Vector3d(15, 15, 5), 1);

            Assert.AreEqual(1, grid.FindRepresentative(new Vector3d(15, 15, 5)), "edge neighbor");
            Assert.AreEqual(1, grid.FindRepresentative(new Vector3d(15, 15, 15)), "corner neighbor");
        }

        [Test]
        public void FindRepresentative_NegativeNeighbors_AreFound()
        {
            HashGrid grid = NewGrid();
            grid.AddToRepresentative(new Vector3d(2, 2, 2), 1);

            Debug.Log(grid.Quantize(new Vector3d(2, 2, 2)));
            Debug.Log(grid.Quantize(new Vector3d(-2, -2, -2)));

            Debug.Log(Vector3d.Distance(new Vector3d(-2, -2, -2), new Vector3d(2, 2, 2)));

            Assert.AreEqual(1, grid.FindRepresentative(new Vector3d(-2, -2, -2)));
        }

        [Test]
        public void FindRepresentative_OutsideNeighborhood_ReturnsMinusOne()
        {
            HashGrid grid = NewGrid();
            grid.AddToRepresentative(new Vector3d(5, 5, 5), 1);

            Assert.AreEqual(-1, grid.FindRepresentative(new Vector3d(35, 5, 5)));
            Assert.AreEqual(-1, grid.FindRepresentative(new Vector3d(5, -25, 5)));
            Assert.AreEqual(-1, grid.FindRepresentative(new Vector3d(100, 100, 100)));
        }

        [Test]
        public void SquareBoundaries_AreConsistent()
        {
            HashGrid grid = NewGrid();
            grid.AddToRepresentative(new Vector3d(0, 0, 0), 1);

            Assert.AreEqual(grid.AddToRepresentative(new Vector3d(9.999, 9.999, 9.999), 2), 1, "still in square (0,0,0)");
            Assert.AreEqual(grid.AddToRepresentative(new Vector3d(10, 0, 0), 3), 3, "x = 10 starts square (1,0,0)");
            Assert.AreEqual(2, grid.Count);
        }

        [Test]
        public void LargeCoordinates_DoNotCollideIncorrectly()
        {
            HashGrid grid = NewGrid();
            grid.AddToRepresentative(new Vector3d(1e9, 1e9, 1e9), 1);
            grid.AddToRepresentative(new Vector3d(-1e9, -1e9, -1e9), 2);

            Assert.AreEqual(2, grid.Count);

            Debug.Log(grid.Quantize(new Vector3d(1e9, 1e9, 1e9)));
            Debug.Log(grid.Quantize(new Vector3d(-1e9, -1e9, -1e9)));

            Assert.IsTrue(grid.HasRepresentative(new Vector3d(1e9, 1e9, 1e9)));
            Assert.IsTrue(grid.HasRepresentative(new Vector3d(-1e9, -1e9, -1e9)));

            Assert.AreNotEqual(grid.Quantize(new Vector3d(1e9, 1e9, 1e9)), grid.Quantize(new Vector3d(-1e9, -1e9, -1e9)));

            Assert.AreEqual(1, grid.FindRepresentative(new Vector3d(1e9, 1e9, 1e9)));
            Assert.AreEqual(2, grid.FindRepresentative(new Vector3d(-1e9, -1e9, -1e9)));
        }

        [Test]
        public void Count_TracksUniqueSquaresOnly()
        {
            HashGrid grid = NewGrid();
            for (int i = 0; i < 100; i++)
                grid.AddToRepresentative(new Vector3d(i % 10, (i / 10) % 10, 0), i); // all in square (0,0,0)

            Assert.AreEqual(1, grid.Count);

            for (int i = 0; i < 10; i++)
                grid.AddToRepresentative(new Vector3d(i * SEARCH_RADIUS + 5, 0, 0), 1000 + i); // squares (0..9, 0, 0)

            Assert.AreEqual(10, grid.Count);
        }

        [Test]
        public void Bench_InsertAndQuery()
        {
            if (SKIP_BENCH) Assert.Ignore("Benchmarks skipped.");

            const int N = 100_000;
            HashGrid grid = NewGrid();
            var rng = new System.Random(12345);

            var points = new Vector3d[N];
            for (int i = 0; i < N; i++)
                points[i] = new Vector3d(
                    rng.NextDouble() * 10_000 - 5_000,
                    rng.NextDouble() * 10_000 - 5_000,
                    rng.NextDouble() * 10_000 - 5_000);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int unique = 0;
            for (int i = 0; i < N; i++)
            {
                if (grid.AddToRepresentative(points[i], i) == i)
                {
                    unique++;
                }
            }

            sw.Stop();
            UnityEngine.Debug.Log($"HashGrid: {N} TryAdd, {unique} unique in {sw.ElapsedMilliseconds} ms (Count={grid.Count})");

            sw.Restart();
            int found = 0;
            for (int i = 0; i < N; i++)
                if (grid.FindRepresentative(points[i]) != -1) found++;
            sw.Stop();
            UnityEngine.Debug.Log($"HashGrid: {N} FindRepresentative in {sw.ElapsedMilliseconds} ms");

            // Every inserted point's own square has a representative.
            Assert.AreEqual(N, found);
        }
    }
}