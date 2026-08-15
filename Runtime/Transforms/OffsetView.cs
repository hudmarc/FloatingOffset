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
        private bool registered = false;
        private bool isValid = false;
        public Action OnPreOffset;
        public Action OnOffset;
        void Start()
        {
            if (enabled && !registered && transform.parent == null)
            {
                universe.manager.RegisterView(this);
                registered = true;
            }
            isValid = true;

        }
        void OnDestroy()
        {
            if (registered && universe.ServerActive)
                universe.manager.UnregisterView(this);
            isValid = false;
        }
        [Obsolete("Use TeleportTo on the OffsetUniverse")]
        public void SetRealPositionApproximate(Vector3d position) { }
        /// <summary>
        /// The real position of this OffsetView in its Offset Universe.
        /// </summary>
        /// <returns>The real position.</returns>
        public Vector3d GetRealPosition() => UnityFunctions.UnityToReal(transform.position, GetOffset());
        private Vector3d GetOffset() => universe.manager.GetOffset(gameObject.scene);
        public bool IsValid() => isValid;

        Vector3d IOffsetObject<Scene>.GetEnginePosition() => UnityFunctions.toVector3d(transform.position);
        Scene IOffsetObject<Scene>.GetSceneKey() => gameObject.scene;
        void IOffsetObject<Scene>.SetSceneKey(Scene key)
        {
            OnPreOffset?.Invoke();
            SceneManager.MoveGameObjectToScene(gameObject, key);
            OnOffset?.Invoke();
        }
        void IOffsetObject<Scene>.Destroy() => Destroy(gameObject);
        void IOffsetObject<Scene>.SetEnginePosition(Vector3d position) => transform.position = UnityFunctions.toVector3(position);

    }
}
