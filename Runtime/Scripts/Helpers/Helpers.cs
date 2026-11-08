using System;
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
        public static string HandleToHex(this Scene scene) => scene.handle.GetHashCode().ToHex();
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
        public Vector3d ToVector3d() => new Vector3d(x, y, z);
    }
}
