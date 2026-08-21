using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// Offset Anchors ensure that the object they are attached to is always at the exact position specified in the OffsetAnchor's target position.<br/>
    /// This also means that they may exist in more than one scene at a time on the server, unless they are paired with an OffsetView
    /// </summary>
    public class OffsetAnchor : OffsetBehaviour, IOffsettable<Scene>
    {
        [field: SerializeField]
        public Vector3d realPosition { get; private set; }
        private Scene scene = default;
        private bool initialized = false;
        void Awake()
        {
            if (!universe.ServerActive)
                return;

            initialized = true;
            scene = gameObject.scene;
            universe.manager.RegisterOffsettable(this);
        }
        void Start()
        {
            if (initialized)
            {
                return;
            }
            scene = gameObject.scene;
            universe.manager.RegisterOffsettable(this);


            Vector3d current_scene_offset = universe.manager.GetOffset(scene);
            transform.position = UnityFunctions.toVector3(realPosition - current_scene_offset);

        }
        void OnDestroy()
        {
            Debug.Log($"Destroyed OffsetAnchor on {gameObject.name}");
            universe.manager.UnregisterOffsettable(this);
        }
        public void OnOffset(Vector3d old_offset, Vector3d new_offset, Scene scene)
        {
            if (this == null)
            {
                Debug.LogWarning("Tried to call Offset on non-existent Anchor");
                return;
            }
            Debug.Log($"Moved {gameObject.name} from {old_offset} to {new_offset} at position {realPosition}"); //why does this return

            transform.position = UnityFunctions.toVector3(realPosition - new_offset);
        }
        public void SetRealPosition(Vector3d new_position)
        {
            transform.position = UnityFunctions.toVector3(new_position - realPosition);
            realPosition = new_position;
        }

        public Scene GetSceneKey()
        {
            return scene;
        }
    }
}
