using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    public static class Helpers
    {
        /// <summary>
        /// Converts the hash code of this scene's handle to hex for easy display. Compatible with Unity 6.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string HandleToHex(this Scene scene) => scene.handle.GetHashCode().ToHex();

        /// <summary>
        /// Shorthand for <code>gameObject.scene.GetPhysicsScene()</code>
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PhysicsScene Physics(this GameObject gameObject) => gameObject.scene.GetPhysicsScene();
        /// <summary>
        /// Converts the given Vector3 to a Vector3d
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d ToVector3d(this Vector3 vector) => new Vector3d(vector.x, vector.y, vector.z);
        /// <summary>
        /// Converts the given Vector3d to a Vector3
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVector3(this Vector3d vector) => new Vector3((float)vector.x, (float)vector.y, (float)vector.z);

        /// <summary>
        /// Converts the given Vector3d into a normalized Vector3.
        /// </summary>
        /// <param name="vector">The vector to convert into a normalized Vector3.</param>
        /// <returns>The vector, normalized, as a Vector3.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToNormalizedVector3(this Vector3d vector) => vector.Normalize().ToVector3();

        /// <summary>
        /// Element-wise scales the vector.
        /// </summary>
        /// <param name="vect">The vector that will be scaled.</param>
        /// <param name="scale">Each element of the vector will be multiplied by this.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d Scale(this Vector3d vect, Vector3 scale) => new Vector3d(vect.x * scale.x, vect.y * scale.y, vect.z * scale.z);

        /// <summary>
        /// Normalize the given Vector3d.
        /// </summary>
        /// <param name="vector">he vector to normalize.</param>
        /// <returns>The vector, normalized.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d Normalize(this Vector3d vector) => vector * (1/vector.Magnitude());

        /// <summary>
        /// Shortcut for <code>Vector3d.Magnitude(vector)</code>
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Magnitude(this Vector3d vector) => Vector3d.Magnitude(vector);

        /// <summary>
        /// Shortcut for <code>Vector3d.SquaredMagnitude(vector)</code>
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SquaredMagnitude(this Vector3d vector) => Vector3d.SquaredMagnitude(vector);
    }

    [Serializable]
    public class SerializableVector3d
    {
        [SerializeField] private double x;
        [SerializeField] private double y;
        [SerializeField] private double z;

        public SerializableVector3d(Vector3d vector)
        {
            this.x = vector.x;
            this.y = vector.y;
            this.z = vector.z;
        }

        public SerializableVector3d(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3d Value => new Vector3d(x, y, z);
    }
}
