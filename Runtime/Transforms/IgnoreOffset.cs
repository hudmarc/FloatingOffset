using FloatingOffset.Runtime.Types;
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
            manager.RegisterKeyed(this, true);
        }
        void OnDestroy()
        {
            manager.UnregisterKeyed(this);
        }
        public object GetEngineObject() => gameObject;

        public Scene GetSceneKey() => gameObject.scene;
        public bool IsValid() => this != null;

        public string GetName() => gameObject.name;
    }
}
