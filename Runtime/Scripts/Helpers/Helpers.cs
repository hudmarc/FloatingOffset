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
}
