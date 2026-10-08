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

        public abstract bool IsLogging();

        public abstract bool IsServerActive();
        public abstract void TeleportTo(OffsetView view, Vector3d position);
        public abstract Vector3d GetLocalOffset(Scene scene);
        public abstract void RegisterView(OffsetView view);
        public abstract void UnregisterView(OffsetView view);
        public abstract void RegisterOffsettable(IOffsettable<Scene> offsettable, Scene scene);
        public abstract void UnregisterOffsettable(IOffsettable<Scene> offsettable, Scene scene);
        public abstract bool HasScene(Scene scene);

        public abstract int OffsettableCount();
        public abstract int CountRegisteredViews();
        public abstract int CountViews();
    }
}
