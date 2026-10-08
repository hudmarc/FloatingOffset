using UnityEngine;

namespace FloatingOffset.Runtime.Example
{
    public class OffsetPredictedWheels : OffsetBehaviour
    {
        private OffsetView view;
        private WheelState[] cachedWheelStates;
        private WheelCollider[] wheels;
        private Rigidbody rb;
        private Vector3 cachedVelocity;
        private Vector3 cachedAngularVelocity;

        void Start()
        {
            wheels = GetComponentsInChildren<WheelCollider>();
            cachedWheelStates = new WheelState[wheels.Length];
            rb = GetComponent<Rigidbody>();
            view = GetComponent<OffsetView>();
            view.OnPreSceneTransfer += CacheState;
            view.OnSceneTransfer += ReapplyState;
        }
        void OnDestroy()
        {
            view.OnPreSceneTransfer -= CacheState;
            view.OnSceneTransfer -= ReapplyState;
        }
        private struct WheelState
        {
            public float motorTorque;
            public float brakeTorque;
            public float steerAngle;
        }

        public void CacheState()
        {
            cachedVelocity = rb.velocity;
            cachedAngularVelocity = rb.angularVelocity;

            for (int i = 0; i < wheels.Length; i++)
            {
                cachedWheelStates[i] = new WheelState
                {
                    motorTorque = wheels[i].motorTorque,
                    brakeTorque = wheels[i].brakeTorque,
                    steerAngle = wheels[i].steerAngle
                };
            }
        }

        public void ReapplyState()
        {
            rb.velocity = cachedVelocity;
            rb.angularVelocity = cachedAngularVelocity;

            for (int i = 0; i < wheels.Length; i++)
            {
                // Force PhysX object rebuild to fix the desync
                wheels[i].enabled = false;
                wheels[i].enabled = true;

                wheels[i].motorTorque = cachedWheelStates[i].motorTorque;
                wheels[i].brakeTorque = cachedWheelStates[i].brakeTorque;
                wheels[i].steerAngle = cachedWheelStates[i].steerAngle;
            }
        }
    }
}