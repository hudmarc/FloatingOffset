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
        public OffsetManager manager { get; private set; }

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
        public void InitializeWithHandler(OffsetManager manager, IOffsetHandler<Scene> handler)
        {
            if (server == null)
                this.server = new OffsetServer<Scene>(handler, MinimumJoinDistance, MaxScenes, Hysteresis); // Ensure the OffsetServer is ready before the manager is registered

            if (manager == null)
                this.manager = manager;
        }

        public void RegisterManager(OffsetManager manager)
        {
            this.manager = manager;
        }
        /// <summary>
        /// Alias for <code>manager.RegisterOffsettable(offsettable);</code>
        /// </summary>
        public void RegisterOffsettable(IOffsettable<Scene> offsettable) => manager.RegisterOffsettable(offsettable);

        /// <summary>
        /// Alias for <code>manager.GetOffset(scene);</code>
        /// </summary>
        public Vector3d GetOffset(Scene scene) => manager.GetOffset(scene);

        /// <summary>
        /// Alias for <code>manager.HasScene(scene);</code>
        /// </summary>
        public virtual bool HasScene(Scene scene) => manager.HasScene(scene);

        /// <summary>
        /// Alias for <code>manager.TeleportTo(view, position);</code>
        /// </summary>
        public void TeleportTo(OffsetView view, Vector3d position) => manager.TeleportTo(view, position);
    }
}
