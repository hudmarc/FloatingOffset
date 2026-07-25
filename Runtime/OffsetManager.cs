using UnityEngine.SceneManagement;
using UnityEngine;
using FloatingOffset.Runtime.Types;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The offset manager bootstraps the OffsetServer. Disable it on network clients.
    /// </summary>
    public class OffsetManager : OffsetBehaviour
    {
        [SerializeField]
        protected OffsetSceneHandler handler;
        /// <summary>
        /// Set false to disable physics processing on stacked scenes.
        /// </summary>
        public bool updateScenePhysicsInternally = true;

        internal void RegisterView(OffsetView view)
        {
            handler.RegisterView(view);
            OnViewRegistered(view);
        }
        internal void UnregisterView(OffsetView view)
        {
            if (universe.ServerActive)
                universe.server.UnregisterView(view);
        }
        /// <summary>
        /// Runs the Process loop on the OffsetUniverse.
        /// </summary>
        protected void Process()
        {
            universe.server.Process();
        }
        /// <summary>
        /// Called immediately after RegisterView is called.
        /// </summary>
        /// <param name="view"></param>
        protected virtual void OnViewRegistered(OffsetView view)
        {
            // This space left intentionally blank
        }
        /// <summary>
        /// Register the given Offsettable. Use this if you want something in your game world to be notified when the scene it is in is offset (for example, if you want your terrain to apply a UV-offset based on the real offset of the game scene)
        /// </summary>
        /// <param name="offsettable">The Offsettable to register.</param>
        public void RegisterOffsettable(IOffsettable<Scene> offsettable)
        {
            handler.RegisterOffsettable(offsettable, offsettable.GetSceneKey());
        }
        /// <summary>
        /// Gets the offset of the corresponding scene. Only callable on the host/server.
        /// </summary>
        /// <param name="scene">The scene to get the offset for.</param>
        /// <returns>The offset of the given scene, or zero if the scene does not exist.</returns>
        public virtual Vector3d GetOffset(Scene scene)
        {
            return handler.GetOffset(scene);
        }
        public virtual bool HasScene(Scene scene)
        {
            return handler.HasScene(scene);
        }

        /// <summary>
        /// Teleport the given OffsetView view to the given position in space.
        /// </summary>
        /// <param name="view">The offset transform to teleport.</param>
        /// <param name="position">The destination where this offset transform will be teleported.</param>
        public void TeleportTo(OffsetView view, Vector3d position)
        {
            if (universe.ServerActive)
            {
                universe.server.TeleportTo(view, position);
                if (universe.logging)
                    Debug.Log($"Teleported {view.name} to {position}");
            }
        }
    }
}
