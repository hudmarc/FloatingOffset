using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime.Example
{
    public class OffsetPredictedRigidbody : OffsetBehaviour, IOffsettable<Scene>
    {
        private const int HISTORY_SIZE = 15;

        private OffsetView view;
        private Rigidbody[] rigidbodies = new Rigidbody[0];

        // Ring buffer storing velocity history: [rigidbodyIndex, historyIndex]
        private Vector3[,] velocityHistory;
        private Vector3[] restoredVelocities = new Vector3[0];
        private int bufferIndex = 0;

        [SerializeField] int restoreFrames = 120;
        private int restore_frames = 0;

        void Awake()
        {
            view = GetComponent<OffsetView>();

            rigidbodies = GetComponentsInChildren<Rigidbody>();
            velocityHistory = new Vector3[rigidbodies.Length, HISTORY_SIZE];
            restoredVelocities = new Vector3[rigidbodies.Length];
        }

        void Start()
        {
            universe.RegisterOffsettable(this);
        }

        void Update()
        {
            if (restore_frames < 1)
            {
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    velocityHistory[i, bufferIndex] = rigidbodies[i].velocity;
                }

                bufferIndex = (bufferIndex + 1) % HISTORY_SIZE;
            }
            else
            {
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    float maxSqrMag = 1f;
                    if (rigidbodies[i].velocity.sqrMagnitude < maxSqrMag)
                    {
                        rigidbodies[i].velocity = restoredVelocities[i];
                    }
                    rigidbodies[i].WakeUp();
                }
                restore_frames--;
            }
        }

        public void OnOffset(Vector3d old_offset, Vector3d new_offset, Scene scene)
        {
            // Evaluate ring buffer history for each rigidbody to find highest velocity
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Vector3 maxVel = Vector3.zero;
                float maxSqrMag = 1f;

                for (int b = 0; b < HISTORY_SIZE; b++)
                {
                    float sqrMag = velocityHistory[i, b].sqrMagnitude;
                    if (sqrMag > maxSqrMag)
                    {
                        maxSqrMag = sqrMag;
                        maxVel = velocityHistory[i, b];
                    }
                }

                restoredVelocities[i] = maxVel;
                rigidbodies[i].Sleep();
            }

            restore_frames = HISTORY_SIZE;
        }

        public Scene GetSceneKey() => gameObject.scene;
        public bool IsValid() => this != null;

        public void OnPreOffset(Vector3d old_offset, Vector3d new_offset, Scene scene)
        {
            
        }
    }
}