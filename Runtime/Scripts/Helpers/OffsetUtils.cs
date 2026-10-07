using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// Common utility functions for the FloatingOffset package.
    /// </summary>
    public static class OffsetUtils
    {
        /// <summary>
        /// Is the OffsetServer active on this game instance? (In a networked environment, this is true on the network server's instance, but false on all other instances)
        /// </summary>
        public static bool ServerActive => OffsetBehaviour.manager.IsServerActive();
        /// <summary>
        /// Teleport the given view to the given position in space. Can only be called if the server on this instance is active.
        /// </summary>
        public static void TeleportTo(OffsetView view, Vector3d position)
        {
            if (!ServerActive)
            {
                Debug.LogWarning("TeleportTo cannot be called on instances without an active OffsetServer. If this is a networked game, use an RPC to call this on the server. Otherwise, make sure you have added an OffsetManager and it is starting correctly.");
                return;
            }
            OffsetBehaviour.manager.TeleportTo(view, position);
        }
        /// <summary>
        /// Get the real position of the given transform relative to its scene's offset.
        /// </summary>
        /// <param name="trf"></param>
        /// <returns></returns>
        public static Vector3d GetRealPosition(Transform trf)
        {
            Scene scene = trf.gameObject.scene;

            if (!OffsetBehaviour.manager.HasScene(scene))
            {
                Debug.LogWarning($"The scene '{scene.name}' (from game object {trf.gameObject.name}) does not seem to be registered to the OffsetManager on this instance. Are you calling this method from an object not in an OffsetScene?");
                return Vector3d.zero;
            }

            return UnityToReal(trf.position, OffsetBehaviour.manager.GetLocalOffset(scene));
        }
        public static PhysicsScene Physics(this GameObject gameObject) => gameObject.scene.GetPhysicsScene();

        public static Vector3d ToVector3d(Vector3 vector) => new Vector3d(vector.x, vector.y, vector.z);
        public static Vector3 ToVector3(Vector3d vector) => new Vector3((float)vector.x, (float)vector.y, (float)vector.z);

        // Custom extension methods for the Floating Offset package. Full test coverage under "Functional".

        /// <summary>
        /// Given a real position and an offset, subtracts the offset and returns a plain Vector3 scene position.
        /// </summary>
        /// <param name="realPosition">The real position</param>
        /// <param name="offset">The offset of the scene.</param>
        /// <returns></returns>
        public static Vector3 RealToUnity(Vector3d realPosition, Vector3d offset) => ToVector3(realPosition - offset);
        /// <summary>
        /// Given a unity position as a Vector3 and an offset as a vector3d, returns the resulting real position.
        /// </summary>
        /// <param name="unityPosition">The unity scene position.</param>
        /// <param name="offset">The offset of the scene.</param>
        /// <returns></returns>
        public static Vector3d UnityToReal(Vector3 unityPosition, Vector3d offset) => ToVector3d(unityPosition) + offset;
    }
}
