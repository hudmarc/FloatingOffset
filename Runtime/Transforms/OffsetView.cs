using System;

using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// Will always be within the merge area of the nearest scene. It will additionally be continuosly updated so that it never goes farther than SceneRadius from the center of its scene.
    /// </summary>
    public class OffsetView : OffsetBehaviour, IOffsetObject<Scene>
    {
        /// <summary>
        /// Invoked just before this view will be transferred between scenes.
        /// </summary>
        public Action OnPreSceneTransfer;
        /// <summary>
        /// Invoked immediately after this view will be transferred between scenes.
        /// </summary>
        public Action OnSceneTransfer;
        private bool registered = false;
        [Tooltip("If true, the first instance of this prefab spawned will be considered the host's main view.")]
        public bool IsPlayer = false;

        void Start()
        {
            if (manager.IsLogging())
                Debug.Log("Started offset view");
            if (!manager.IsServerActive())
            {
                manager.OnInitialOffset += OnInitialOffset;
            }
            else
            {
                Register(); // Register immediately if the server is already active. 
            }
        }
        void OnInitialOffset()
        {
            manager.OnOffsetServerInitialized -= OnInitialOffset;
            if (manager.IsLogging())
                Debug.Log($"Called OnInitialOffset");
            Register();
        }
        void Register()
        {
            if (!registered)
            {
                manager.RegisterView(this);
                registered = true;
            }
        }
        void OnDestroy()
        {
            if (manager.IsLogging())
                Debug.Log($"Destroyed view {gameObject.name}");
            if (registered && manager.IsServerActive())
            {
                manager.UnregisterView(this);
                registered = false;
                if (manager.IsLogging())
                    Debug.Log($"Unregistered view {gameObject.name}");
            }
        }
        [Obsolete("Use TeleportTo on the OffsetUniverse")]
        public void SetRealPositionApproximate(Vector3d position) { transform.position = new Vector3((float)position.x, (float)position.y, (float)position.z); }
        /// <summary>
        /// Alias for <code>universe.TeleportTo(view, position);</code>
        /// </summary>
        /// <param name="position"></param>
        public void TeleportTo(Vector3d position) => OffsetUtils.TeleportTo(this, position);
        Vector3d IOffsetObject<Scene>.GetEnginePosition() => OffsetUtils.ToVector3d(transform.position);
        Scene IOffsetObject<Scene>.GetSceneKey() => gameObject.scene;
        void IOffsetObject<Scene>.Destroy() => Destroy(gameObject);
        void IOffsetObject<Scene>.SetEnginePosition(Vector3d position) => transform.position = OffsetUtils.ToVector3(position);
        void IOffsetObject<Scene>.OnPreSceneTransfer() => OnPreSceneTransfer?.Invoke();
        void IOffsetObject<Scene>.OnSceneTransfer() => OnSceneTransfer?.Invoke();
        bool IOffsetObject<Scene>.IsPlayer() => IsPlayer;
        public bool IsValid() => registered;

        public string GetName() => gameObject.name;
    }
}
