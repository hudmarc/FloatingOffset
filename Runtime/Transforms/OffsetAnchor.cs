using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// Offset Anchors ensure that the object they are attached to is always at the exact position specified in the OffsetAnchor's target position.<br/>
    /// This also means that they may exist in more than one scene at a time on the server.
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
            universe.RegisterOffsettable(this, scene);

        }
        void Start()
        {
            if (initialized)
            {
                return;
            }
            scene = gameObject.scene;
            universe.RegisterOffsettable(this, scene);

            Vector3d current_scene_offset = universe.GetSceneOffset(scene);
            transform.position = Mathd.toVector3(realPosition - current_scene_offset);

        }
        public void OnOffset(Vector3d old_offset, Vector3d new_offset, Scene scene)
        {
            Debug.Log($"Moved {gameObject.name} from {old_offset} to {new_offset} at position {realPosition}");

            transform.position = Mathd.toVector3(realPosition - new_offset);
        }
        public void SetRealPosition(Vector3d new_position)
        {
            transform.position = Mathd.toVector3(new_position - realPosition);
            realPosition = new_position;
        }

        public Scene GetSceneKey()
        {
            return scene;
        }
    }
}
