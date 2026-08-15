using FloatingOffset.Runtime;
using NUnit.Framework;
using UnityEngine;
namespace FloatingOffset.Editor.Tests
{
    public class Functional
    {
        //this number is way bigger than the radius of the solar system
        private readonly float HUGE_NUMBER = Mathf.Pow(2, 52);
        [Test]
        public void TestZeroPositiveUnity()
        {
            Vector3 unityPosition = new Vector3(10000, 10000, 10000);
            Vector3d offset = (UnityFunctions.toVector3d(Vector3.zero));
            Vector3d realPosition = UnityFunctions.toVector3d(unityPosition) + offset;

            Assert.AreEqual(realPosition, UnityFunctions.UnityToReal(unityPosition, offset));
            Assert.AreNotEqual(realPosition, offset);
        }
        [Test]
        public void MaxDistanceBenchmark()
        {
            for (int i = 0; i < 100000; i++)
            {
                UnityFunctions.MaxLengthScalar(new Vector3d(i, i, i));
            }
        }
        [Test]
        public void Vector3dDistanceBenchmark()
        {
            for (int i = 0; i < 100000; i++)
            {
                Vector3d.Distance(new Vector3d(i, i, i), Vector3d.zero);
            }
        }

        [Test]
        public void TestZeroNegativeUnity()
        {
            Vector3 unityPosition = new Vector3(-10000, -10000, -10000);
            Vector3d offset = UnityFunctions.toVector3d(Vector3.zero);
            Vector3d realPosition = UnityFunctions.toVector3d(unityPosition) + offset;

            Assert.AreEqual(realPosition, UnityFunctions.UnityToReal(unityPosition, offset));
            Assert.AreNotEqual(realPosition, offset);
        }

        [Test]
        public void TestMaxPositiveUnity()
        {
            Vector3 unityPosition = new Vector3(10000, 10000, 10000);
            Vector3d offset = UnityFunctions.toVector3d(new Vector3(HUGE_NUMBER, HUGE_NUMBER, HUGE_NUMBER));
            Vector3d realPosition = UnityFunctions.toVector3d(unityPosition) + offset;

            Assert.AreEqual(realPosition, UnityFunctions.UnityToReal(unityPosition, offset));
            Assert.AreNotEqual(realPosition, offset);
        }

        [Test]
        public void TestMaxNegativeUnity()
        {
            Vector3 unityPosition = new Vector3(-10000, -10000, -10000);
            Vector3d offset = UnityFunctions.toVector3d(new Vector3(HUGE_NUMBER, HUGE_NUMBER, HUGE_NUMBER));
            Vector3d realPosition = UnityFunctions.toVector3d(unityPosition) + offset;

            Assert.AreEqual(realPosition, UnityFunctions.UnityToReal(unityPosition, offset));
            Assert.AreNotEqual(realPosition, offset);
        }

        [Test]
        public void TestMinPositiveUnity()
        {
            Vector3 unityPosition = new Vector3(10000, 10000, 10000);
            Vector3d offset = UnityFunctions.toVector3d(new Vector3(-HUGE_NUMBER, -HUGE_NUMBER, -HUGE_NUMBER));
            Vector3d realPosition = UnityFunctions.toVector3d(unityPosition) + offset;

            Assert.AreEqual(realPosition, UnityFunctions.UnityToReal(unityPosition, offset));
            Assert.AreNotEqual(realPosition, offset);
        }

        [Test]
        public void TestMinNegativeUnity()
        {
            Vector3 unityPosition = new Vector3(-10000, -10000, -10000);
            Vector3d offset = UnityFunctions.toVector3d(new Vector3(-HUGE_NUMBER, -HUGE_NUMBER, -HUGE_NUMBER));
            Vector3d realPosition = UnityFunctions.toVector3d(unityPosition) + offset;

            Assert.AreEqual(realPosition, UnityFunctions.UnityToReal(unityPosition, offset));
            Assert.AreNotEqual(realPosition, offset);
        }



        [Test]
        public void TestZeroPositiveUnityRealToUnity()
        {
            Vector3 unityPosition = new Vector3(10000, 10000, 10000);
            Vector3d offset = UnityFunctions.toVector3d(Vector3.zero);
            Vector3d realPosition = UnityFunctions.toVector3d(unityPosition) + offset;

            Assert.AreEqual(unityPosition, UnityFunctions.RealToUnity(realPosition, offset));
            Assert.AreNotEqual(realPosition, offset);
        }

        [Test]
        public void TestZeroNegativeUnityRealToUnity()
        {
            Vector3 unityPosition = new Vector3(-10000, -10000, -10000);
            Vector3d offset = UnityFunctions.toVector3d(Vector3.zero);
            Vector3d realPosition = UnityFunctions.toVector3d(unityPosition) + offset;

            Assert.AreEqual(unityPosition, UnityFunctions.RealToUnity(realPosition, offset));
            Assert.AreNotEqual(realPosition, offset);
        }

        [Test]
        public void TestMaxPositiveUnityRealToUnity()
        {
            Vector3 unityPosition = new Vector3(10000, 10000, 10000);
            Vector3d offset = UnityFunctions.toVector3d(new Vector3(HUGE_NUMBER, HUGE_NUMBER, HUGE_NUMBER));
            Vector3d realPosition = UnityFunctions.toVector3d(unityPosition) + offset;

            Assert.AreEqual(unityPosition, UnityFunctions.RealToUnity(realPosition, offset));
            Assert.AreNotEqual(realPosition, offset);
        }

        [Test]
        public void TestMaxNegativeUnityRealToUnity()
        {
            Vector3 unityPosition = new Vector3(-10000, -10000, -10000);
            Vector3d offset = UnityFunctions.toVector3d(new Vector3(HUGE_NUMBER, HUGE_NUMBER, HUGE_NUMBER));
            Vector3d realPosition = UnityFunctions.toVector3d(unityPosition) + offset;

            Assert.AreEqual(unityPosition, UnityFunctions.RealToUnity(realPosition, offset));
            Assert.AreNotEqual(realPosition, offset);
        }

        [Test]
        public void TestMinPositiveUnityRealToUnity()
        {
            Vector3 unityPosition = new Vector3(10000, 10000, 10000);
            Vector3d offset = UnityFunctions.toVector3d(new Vector3(-HUGE_NUMBER, -HUGE_NUMBER, -HUGE_NUMBER));
            Vector3d realPosition = UnityFunctions.toVector3d(unityPosition) + offset;

            Assert.AreEqual(unityPosition, UnityFunctions.RealToUnity(realPosition, offset));
            Assert.AreNotEqual(realPosition, offset);
        }

        [Test]
        public void TestMinNegativeUnityRealToUnity()
        {
            Vector3 unityPosition = new Vector3(-10000, -10000, -10000);
            Vector3d offset = UnityFunctions.toVector3d(new Vector3(-HUGE_NUMBER, -HUGE_NUMBER, -HUGE_NUMBER));
            Vector3d realPosition = UnityFunctions.toVector3d(unityPosition) + offset;

            Assert.AreEqual(unityPosition, UnityFunctions.RealToUnity(realPosition, offset));
            Assert.AreNotEqual(realPosition, offset);
        }

    }
}