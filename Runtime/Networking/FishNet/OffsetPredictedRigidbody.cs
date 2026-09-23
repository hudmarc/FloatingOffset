using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime.Example
{
    public class OffsetPredictedRigidbody : OffsetBehaviour, IOffsettable<Scene>
    {
        private OffsetView view;
        private Rigidbody[] rigidbodies = new Rigidbody[0];
        private Vector3[] velocities = new Vector3[0];
        [SerializeField] float acceleration_delta = 40;
        void Awake()
        {
            view = GetComponent<OffsetView>();

            rigidbodies = GetComponentsInChildren<Rigidbody>();
            velocities = new Vector3[rigidbodies.Length];
        }
        void Start()
        {
            universe.RegisterOffsettable(this);
        }
        void Update()
        {
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                if ((rigidbodies[i].velocity).sqrMagnitude > 0.01f)
                    velocities[i] = rigidbodies[i].velocity;
            }
        }
        public void OnOffset(Vector3d old_offset, Vector3d new_offset, Scene scene)
        {
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                rigidbodies[i].velocity = velocities[i];
                Debug.Log($"Restored velocity {rigidbodies[i].velocity} to {rigidbodies[i].gameObject.name}");
            }
        }

        public Scene GetSceneKey()
        {
            return gameObject.scene;
        }
    }
}
