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
        public SerializableVector3d realPosition { get; private set; }
        private Scene scene = default;
        void Awake()
        {
            OffsetManager.OnOffsetProcessorStateChange += OnOffsetProcessorStateChange;
        }

        void Start()
        {
            if(manager.IsServerActive())
                OnOffsetProcessorStateChange(true);
        }

        void OnOffsetProcessorStateChange(bool hasProcessor)
        {
            OffsetManager.OnOffsetProcessorStateChange -= OnOffsetProcessorStateChange;
            if (!hasProcessor)
                return;

            scene = gameObject.scene;
            manager.RegisterOffsettable(this, this.GetSceneKey());


            Vector3d current_scene_offset = manager.GetLocalOffset(scene);
            transform.position = OffsetUtils.ToVector3(realPosition.ToVector3d() - current_scene_offset);
        }
        void OnDestroy()
        {
            Debug.Log($"Destroyed OffsetAnchor on {gameObject.name}");
            manager.UnregisterOffsettable(this, this.GetSceneKey());
        }
        public void OnOffset(Vector3d old_offset, Vector3d new_offset, Scene scene)
        {
            if (this == null)
            {
                Debug.LogWarning("Tried to call Offset on non-existent Anchor");
                return;
            }
            Debug.Log($"Moved {gameObject.name} from {old_offset} to {new_offset} at position {realPosition.ToVector3d()}"); //why does this return

            transform.position = OffsetUtils.ToVector3(realPosition.ToVector3d() - new_offset);
        }
        public void SetRealPosition(Vector3d new_position)
        {
            transform.position = OffsetUtils.ToVector3(new_position - realPosition.ToVector3d());
            realPosition = new SerializableVector3d(new_position);
        }

        public Scene GetSceneKey() => scene;
        public bool IsValid() => this != null;

        public void OnPreOffset(Vector3d old_offset, Vector3d new_offset, Scene scene)
        {
        }
    }
}
