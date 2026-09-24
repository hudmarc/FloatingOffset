using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime.Example
{
    public class OffsetPredictedWheels : OffsetBehaviour, IOffsettable<Scene>
    {
        private const int HISTORY_SIZE = 30;

        private OffsetView view;
        private Rigidbody[] rigidbodies = new Rigidbody[0];
        private WheelCollider[] wheels = new WheelCollider[0];

        private Vector3[,] velocityHistory;
        private Vector3[] restoredVelocities = new Vector3[0];
        private int bufferIndex = 0;

        private int restore_ticks = 0;

        void Awake()
        {
            view = GetComponent<OffsetView>();
            rigidbodies = GetComponentsInChildren<Rigidbody>();
            wheels = GetComponentsInChildren<WheelCollider>();

            velocityHistory = new Vector3[rigidbodies.Length, HISTORY_SIZE];
            restoredVelocities = new Vector3[rigidbodies.Length];
        }

        void Start()
        {
            universe.RegisterOffsettable(this);
        }

        // Use FixedUpdate so this runs in step with PhysX / FishNet simulation ticks
        void FixedUpdate()
        {
            if (restore_ticks < 1)
            {
                // Record current velocities into the ring buffer
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    velocityHistory[i, bufferIndex] = rigidbodies[i].velocity;
                }

                bufferIndex = (bufferIndex + 1) % HISTORY_SIZE;
            }
            else
            {
                // Re-apply velocity and synchronize wheel rotation on EVERY physics tick
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    Rigidbody rb = rigidbodies[i];
                    Vector3 targetVel = restoredVelocities[i];

                    rb.velocity = targetVel;

                    // Match wheel spinning speed to forward chassis velocity
                    ApplyWheelVelocitySync(rb, targetVel);
                }

                restore_ticks--;
            }
        }

        private void ApplyWheelVelocitySync(Rigidbody rb, Vector3 targetVelocity)
        {
            float forwardSpeed = Vector3.Dot(targetVelocity, rb.transform.forward);

            foreach (var wheel in wheels)
            {
                // Calculate required RPM from linear forward speed
                float circumference = 2f * Mathf.PI * wheel.radius;
                float targetRpm = (forwardSpeed / circumference) * 60f;

                // Unity WheelCollider.rotationSpeed is in degrees/second (1 RPM = 6 deg/sec)
                wheel.rotationSpeed = targetRpm * 6f;

                // Clear any residual brake torque that would fight the restoration
                wheel.brakeTorque = 0f;
            }
        }

        public void OnOffset(Vector3d old_offset, Vector3d new_offset, Scene scene)
        {
            // Pick peak velocity from ring buffer
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Vector3 maxVel = Vector3.zero;
                float maxSqrMag = -1f;

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
            }

            restore_ticks = HISTORY_SIZE;
        }

        public Scene GetSceneKey()
        {
            return gameObject.scene;
        }
    }
}