using UnityEngine;

namespace FloatingOffset.Runtime
{
    public static class UnityFunctions
    {
        public static PhysicsScene Physics(this GameObject gameObject) => gameObject.scene.GetPhysicsScene();

        public static Vector3d toVector3d(Vector3 vector) => new Vector3d(vector.x, vector.y, vector.z);
        public static Vector3 toVector3(Vector3d vector) => new Vector3((float)vector.x, (float)vector.y, (float)vector.z);

        // Custom extension methods for the Floating Offset package. Full test coverage under "Functional".

        /// <summary>
        /// Given a real position and an offset, subtracts the offset and returns a plain Vector3 scene position.
        /// </summary>
        /// <param name="realPosition">The real position</param>
        /// <param name="offset">The offset of the scene.</param>
        /// <returns></returns>
        public static Vector3 RealToUnity(Vector3d realPosition, Vector3d offset) => toVector3(realPosition - offset);
        /// <summary>
        /// Given a unity position as a Vector3 and an offset as a vector3d, returns the resulting real position.
        /// </summary>
        /// <param name="unityPosition">The unity scene position.</param>
        /// <param name="offset">The offset of the scene.</param>
        /// <returns></returns>
        public static Vector3d UnityToReal(Vector3 unityPosition, Vector3d offset) => toVector3d(unityPosition) + offset;
        /// <summary>
        /// Gets the longest scalar component from the given vector and returns its magnitude. This version operates on Vector3d's.
        /// </summary>
        /// <param name="vector">
        /// The vector whose scalars will be compared.
        /// </param>
        /// <returns>The longest scalar component of the vector.</returns>
        public static double MaxLengthScalar(Vector3d vector) => Mathd.Max(Mathd.Abs(vector.x), Mathd.Abs(vector.y), Mathd.Abs(vector.z));
    }
}
