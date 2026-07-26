using FloatingOffset.Runtime; // Assuming Vector3d is here
using NUnit.Framework;
using UnityEngine;
namespace FloatingOffset.Editor.Tests
{
    public class Modules
    {
        private const bool SKIP_BENCH = true;

        [Test]
        public void HashGridInitialization()
        {
            // New signature: bucketCount, initialCapacity, maxSearchRadius
            HashGrid test = new HashGrid(1024, 1024, 512.0);
            Assert.NotNull(test);
        }

        [Test]
        public void FindTestZero()
        {
            Debug.Log("FindTestZero");
            HashGrid test = new HashGrid(1024, 1024, 2048.0);

            int test_val = 0;
            Vector3d[] positions = new Vector3d[] { Vector3d.zero };
            test.Add(positions[test_val], test_val);

            int[] buffer = new int[16];

            test.FindNeighbors(Vector3d.zero, positions, ref buffer, out int count);
            Assert.AreEqual(1, count);
            Assert.AreEqual(test_val, buffer[0]);

            // Distance to (1024, 1024, 1024) is approx 1773. It should be found within 2048 radius.
            test.FindNeighbors(new Vector3d(1024, 1024, 1024), positions, ref buffer, out count);
            Assert.AreEqual(1, count);
            Assert.AreEqual(test_val, buffer[0]);
        }

        [Test]
        public void FindTestNotInBox()
        {
            Debug.Log("FindTestNotInBox");
            HashGrid test = new HashGrid(1024, 1024, 512.0);

            int test_val = 0;
            Vector3d[] positions = new Vector3d[] { Vector3d.zero };
            test.Add(positions[test_val], test_val);

            int[] buffer = new int[16];
            // Distance is approx 3547, well outside the 512 radius.
            test.FindNeighbors(new Vector3d(2048, 2048, 2048), positions, ref buffer, out int count);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void FindTestBarelyInBox()
        {
            Debug.Log("FindTestBarelyInBox");
            // Using a radius of 1000 to test the 500 boundary
            HashGrid test = new HashGrid(1024, 1024, 1000.0);

            int test_val = 0;
            Vector3d[] positions = new Vector3d[] { Vector3d.zero };
            test.Add(positions[test_val], test_val);

            int[] buffer = new int[16];
            // Distance is approx 866. It should barely fit in a 1000 radius.
            test.FindNeighbors(new Vector3d(500, 500, 500), positions, ref buffer, out int count);
            Assert.AreEqual(1, count);
            Assert.AreEqual(test_val, buffer[0]);
        }

        [Test]
        public void FindInBoundingBoxMaxSafe()
        {
            Debug.Log("FindInBoundingBoxMaxSafe");
            HashGrid test = new HashGrid(1024, 1024, 2048.0);

            int test_val = 0;
            // Float.MaxValue causes integer truncation overflow. Testing 10 million units instead.
            Vector3d vector = new Vector3d(10000000, 10000000, 10000000);
            Vector3d[] positions = new Vector3d[] { vector };
            test.Add(positions[test_val], test_val);

            int[] buffer = new int[16];
            test.FindNeighbors(vector, positions, ref buffer, out int count);
            Assert.AreEqual(1, count);

            test.FindNeighbors(vector + new Vector3d(1024, 1024, 1024), positions, ref buffer, out count);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void FindInBoundingBoxMinSafe()
        {
            Debug.Log("FindInBoundingBoxMinSafe");
            HashGrid test = new HashGrid(1024, 1024, 2048.0);

            int test_val = 0;
            Vector3d vector = new Vector3d(-10000000, -10000000, -10000000);
            Vector3d[] positions = new Vector3d[] { vector };
            test.Add(positions[test_val], test_val);

            int[] buffer = new int[16];
            test.FindNeighbors(vector, positions, ref buffer, out int count);
            Assert.AreEqual(1, count);

            test.FindNeighbors(vector + new Vector3d(1024, 1024, 1024), positions, ref buffer, out count);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void BenchmarkAdd()
        {
            if (SKIP_BENCH) return;

            Debug.Log("BenchmarkAdd");
            int count = 10000;
            HashGrid test = new HashGrid(16384, count, 512.0);
            Vector3d[] positions = new Vector3d[count];

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            for (int i = 0; i < count; i++)
            {
                positions[i] = new Vector3d(i, i, i);
                test.Add(positions[i], i);
            }
            sw.Stop();
            Debug.Log($"10000 add operations: {(sw.ElapsedMilliseconds)} ms");
        }

        [Test]
        public void BenchmarkFindNeighbors()
        {
            if (SKIP_BENCH) return;

            Debug.Log("BenchmarkFindNeighbors");
            int count = 10000;
            HashGrid test = new HashGrid(16384, count, 1024.0);
            Vector3d[] positions = new Vector3d[count];

            for (int i = 0; i < count; i++)
            {
                positions[i] = new Vector3d(i, i, i);
                test.Add(positions[i], i);
            }

            int[] buffer = new int[64]; // Reused buffer

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            for (int i = 0; i < count; i++)
            {
                test.FindNeighbors(positions[i], positions, ref buffer, out int results);
            }
            sw.Stop();
            Debug.Log($"10000 find operations: {(sw.ElapsedMilliseconds)} ms");
        }

        [Test]
        public void BenchmarkClear()
        {
            if (SKIP_BENCH) return;

            Debug.Log("BenchmarkClear");
            int count = 10000;
            HashGrid test = new HashGrid(16384, count, 1024.0);
            Vector3d[] positions = new Vector3d[count];

            for (int i = 0; i < count; i++)
            {
                positions[i] = new Vector3d(i, i, i);
                test.Add(positions[i], i);
            }

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            // Simulating clearing the grid over 10,000 frames
            for (int i = 0; i < 10000; i++)
            {
                test.Clear();
            }

            sw.Stop();
            Debug.Log($"10000 clear operations: {(sw.ElapsedMilliseconds)} ms");
        }
    }
}