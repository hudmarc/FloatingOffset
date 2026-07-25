using UnityEngine;

namespace FloatingOffset.Runtime.Example
{
    public class OffsetPredictedRigidbody : MonoBehaviour
    {
        private OffsetView offset_transform;
        private Rigidbody[] rigidbodies = new Rigidbody[0];
        private Vector3[] velocities = new Vector3[0];
        void Awake()
        {
            offset_transform = GetComponent<OffsetView>();
            offset_transform.OnPreOffset += GatherVelocities;
            offset_transform.OnOffset += ApplyVelocities;
            rigidbodies = GetComponentsInChildren<Rigidbody>();
            velocities = new Vector3[rigidbodies.Length];

            int rb_count = rigidbodies.Length;
        }
        void OnDestroy()
        {
            if (offset_transform != null)
            {
                offset_transform.OnPreOffset -= GatherVelocities;
                offset_transform.OnOffset -= ApplyVelocities;
            }
        }
        void GatherVelocities()
        {
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                velocities[i] = rigidbodies[i].velocity;
            }
        }
        void ApplyVelocities()
        {
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                rigidbodies[i].velocity = velocities[i];
            }
        }
    }
}
