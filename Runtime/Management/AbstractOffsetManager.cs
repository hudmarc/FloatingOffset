using UnityEngine.SceneManagement;
using UnityEngine;
using FloatingOffset.Runtime.Types;
using System;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The offset manager bootstraps the OffsetServer. Disable it on network clients.
    /// </summary>
    public abstract class AbstractOffsetManager : OffsetBehaviour
    {
        [SerializeField]
        protected AbstractOffsetSceneHandler handler;
        [SerializeField]
        protected OffsetStateManager state;

        /// <summary>
        /// Set false to disable physics processing on stacked scenes.
        /// </summary>
        public bool updateScenePhysicsInternally = true;
        /// <summary>
        /// Runs the Process loop on the OffsetUniverse.
        /// </summary>
        protected void Process() => universe.server.Process();

        /// <summary>
        /// Called immediately after RegisterView is called.
        /// </summary>
        /// <param name="view"></param>
        public virtual void OnViewRegistered(OffsetView view)
        {
            // This space left intentionally blank
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
        public int CountRegisteredViews() => universe.server.RegisteredViewCount();

        public int CountViews() => universe.server.ActualViewCount();
    }
}
