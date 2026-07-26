using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FloatingOffset.Runtime.Example
{
    public class OffsetRigidbody : MonoBehaviour
    {
        private OffsetTransform offset_transform;
        private Rigidbody[] rigidbodies = new Rigidbody[0];
        private Vector3[] velocities = new Vector3[0];
        // Start is called before the first frame update
        void Awake()
        {
            offset_transform = GetComponent<OffsetTransform>();
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

        // Update is called once per frame
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
