using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// Marks an object as ignored by the Offset system, when a scene is rebased this object will not be moved.
    /// </summary>
    public class IgnoreOffset : OffsetBehaviour, ISceneKeyedObject<Scene>
    {
        void Start()
        {
            manager.RegisterKeyed(this);
        }
        void OnDestroy()
        {
            manager.UnregisterKeyed(this);
        }
        public object GetObject() => gameObject;

        public Scene GetSceneKey() => gameObject.scene;
        public bool IsValid() => this != null;
        public bool Ignore() => true;
    }
}
