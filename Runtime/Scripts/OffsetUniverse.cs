using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The OffsetUniverse is used to interact with the OffsetServer in Unity.
    /// </summary>
    [CreateAssetMenu(fileName = "OffsetUniverse", menuName = "FloatingOffset/OffsetUniverse", order = 1)]
    public class OffsetUniverse : ScriptableObject
    {
        /// <summary>
        /// Reference to this game instance's OffsetServer. If in a multiplayer environment this is only initialized on the server/host.
        /// Check if this exists by checking `ServerActive == true`
        /// </summary>
        internal OffsetServer<Scene> server { get; private set; }
        public AbstractOffsetManager manager { get; private set; }
        public OffsetStateManager state { get; private set; }

        [field: SerializeField]
        public int MinimumJoinDistance { get; private set; } = 1000;
        [field: SerializeField]
        public int Hysteresis { get; private set; } = 1000;
        [field: SerializeField]
        public int MaxScenes { get; private set; } = 200;
        [field: SerializeField]
        public bool logging { get; private set; } = false;

        public bool ServerActive => server != null;
        /// <summary>
        /// Register the manager. Can only be called once. Subsequent calls will be ignored. Only call on the host.<br>
        /// Initializes the OffsetServer.
        /// </summary>
        /// <param name="manager">
        /// The OffsetManager component.
        /// </param>
        /// <param name="handler">
        /// The OffsetHandler component.
        /// </param>
        public void InitializeWithHandler(AbstractOffsetManager manager, OffsetStateManager state, IOffsetHandler<Scene> handler)
        {
            if (this.server == null)
                this.server = new OffsetServer<Scene>(handler, MinimumJoinDistance, MaxScenes, Hysteresis); // Ensure the OffsetServer is ready before the manager is registered

            if (this.manager == null)
                this.manager = manager;

            if (this.state == null)
                this.state = state;
        }

        public void RegisterManager(AbstractOffsetManager manager) => this.manager = manager;

        /// <summary>
        /// Alias for <code>manager.RegisterOffsettable(offsettable);</code>
        /// </summary>
        public void RegisterOffsettable(IOffsettable<Scene> offsettable) => state.RegisterOffsettable(offsettable, offsettable.GetSceneKey());

        /// <summary>
        /// Alias for <code>manager.GetOffset(scene);</code>
        /// </summary>
        public Vector3d GetOffset(Scene scene) => state.GetOffset(scene);

        /// <summary>
        /// Alias for <code>manager.HasScene(scene);</code>
        /// </summary>
        public virtual bool HasScene(Scene scene) => state.HasScene(scene);

        /// <summary>
        /// Alias for <code>manager.TeleportTo(view, position);</code>
        /// </summary>
        public void TeleportTo(OffsetView view, Vector3d position) => manager.TeleportTo(view, position);
    }
}
