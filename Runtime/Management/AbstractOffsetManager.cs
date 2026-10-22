using System;
using FloatingOffset.Runtime.Types;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The offset manager bootstraps the OffsetServer. Disable it on network clients.
    /// </summary>
    public abstract class OffsetManager : OffsetBehaviour
    {
        /// <summary>
        /// Set false to disable physics processing on stacked scenes.
        /// </summary>
        public bool updateScenePhysicsInternally = true;
        /// <summary>
        /// Invoked on the server when the initial offset is sent. Invoked on clients when the initial offset is received.
        /// </summary>
        public Action OnInitialOffset;
        /// <summary>
        /// Invoked on the server after the OffsetServer and all its dependencies have been correctly initialized.
        /// </summary>
        public Action OnOffsetServerInitialized;

        /// <summary>
        /// Is this Manager logging?
        /// </summary>
        /// <returns></returns>
        public abstract bool IsLogging();
        /// <summary>
        /// Is this Manager's server active?
        /// </summary>
        /// <returns></returns>
        public abstract bool IsServerActive();
        /// <summary>
        /// Is the given scene registered on this Manager?
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public abstract bool HasScene(Scene scene);
        /// <summary>
        /// Teleports the given View to the given position, in Vector3d space.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="position"></param>
        public abstract void TeleportTo(OffsetView view, Vector3d position);
        /// <summary>
        /// Gets the local (to the current game instance) offset of the given scene. On network clients with a single localOffset, this returns the same offset for any scene queried.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public abstract Vector3d GetLocalOffset(Scene scene);
        /// <summary>
        /// Registers the given view with the offset server.
        /// </summary>
        /// <param name="view"></param>
        public abstract void RegisterView(OffsetView view);
        /// <summary>
        /// Unregisters the given view with the offset server.
        /// </summary>
        /// <param name="view"></param>
        public abstract void UnregisterView(OffsetView view);
        /// <summary>
        /// Registers the given offsettable to the given scene.
        /// </summary>
        /// <param name="offsettable"></param>
        /// <param name="scene"></param>
        public abstract void RegisterOffsettable(IOffsettable<Scene> offsettable, Scene scene);
        /// <summary>
        /// Unregisters the given offsettable from the given scene.
        /// </summary>
        /// <param name="offsettable"></param>
        /// <param name="scene"></param>
        public abstract void UnregisterOffsettable(IOffsettable<Scene> offsettable, Scene scene);
        /// <summary>
        /// Counts how many offsettables are registered to this Manager.
        /// </summary>
        /// <returns></returns>
        public abstract int OffsettableCount();
        /// <summary>
        /// Counts how many Views are registered to this Manager, excluding views pending removal.
        /// </summary>
        /// <returns></returns>
        public abstract int CountRegisteredViews();
        /// <summary>
        /// Counts how many views are registered in total, including views pending removal.
        /// </summary>
        /// <returns></returns>
        public abstract int CountActualViews();
        /// <summary>
        /// Gets a scene near to the given position, based off the position of the scene's representative. If nothing was found, returns <code>TSceneKey.default</code>
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public abstract Scene GetNearbyScene(Vector3d position);
    }
}
