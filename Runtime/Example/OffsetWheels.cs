using System.Collections;
using FloatingOffset.Runtime;
using UnityEngine;

public class OffsetWheels : MonoBehaviour
{
    private OffsetTransform offset_transform;
    private WheelCollider[] wheels = new WheelCollider[0];

    // Cache original wheel settings per wheel
    private WheelFrictionCurve[] origForwardFriction;
    private WheelFrictionCurve[] origSidewaysFriction;
    private JointSpring[] origSuspensionSpring;

    // Keep track of the active coroutine so we don't stack them
    private Coroutine ghostCoroutine;

    void Awake()
    {
        offset_transform = GetComponent<OffsetTransform>();
        offset_transform.OnOffset += FixWheels;
        wheels = GetComponentsInChildren<WheelCollider>();

        int wheelCount = wheels.Length;
        origForwardFriction = new WheelFrictionCurve[wheelCount];
        origSidewaysFriction = new WheelFrictionCurve[wheelCount];
        origSuspensionSpring = new JointSpring[wheelCount];

        // Cache the original properties for every individual wheel
        for (int i = 0; i < wheelCount; i++)
        {
            origForwardFriction[i] = wheels[i].forwardFriction;
            origSidewaysFriction[i] = wheels[i].sidewaysFriction;
            origSuspensionSpring[i] = wheels[i].suspensionSpring;
        }
    }

    void OnDestroy()
    {
        if (offset_transform != null)
        {
            offset_transform.OnOffset -= FixWheels;
        }
    }

    void FixWheels()
    {
        if (wheels.Length == 0) return;

        // If a transition happens while we are already ghosting, reset the timer
        if (ghostCoroutine != null)
        {
            StopCoroutine(ghostCoroutine);
        }

        ghostCoroutine = StartCoroutine(GhostWheelsRoutine());
    }

    private IEnumerator GhostWheelsRoutine()
    {
        // 1. Ghost all wheels (0 stiffness and spring)
        SetWheelsGhosted(true);

        // 2. Wait for exactly 2 physics steps
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // 3. Restore the original wheel forces
        SetWheelsGhosted(false);
        ghostCoroutine = null;
    }

    private void SetWheelsGhosted(bool isGhosted)
    {
        for (int i = 0; i < wheels.Length; i++)
        {
            // Handle Forward Friction
            WheelFrictionCurve fFriction = wheels[i].forwardFriction;
            fFriction.stiffness = isGhosted ? 0f : origForwardFriction[i].stiffness;
            wheels[i].forwardFriction = fFriction;

            // Handle Sideways Friction
            WheelFrictionCurve sFriction = wheels[i].sidewaysFriction;
            sFriction.stiffness = isGhosted ? 0f : origSidewaysFriction[i].stiffness;
            wheels[i].sidewaysFriction = sFriction;

            // Handle Suspension
            JointSpring spring = wheels[i].suspensionSpring;
            spring.spring = isGhosted ? 0f : origSuspensionSpring[i].spring;
            spring.damper = isGhosted ? 0f : origSuspensionSpring[i].damper;
            wheels[i].suspensionSpring = spring;
        }
    }
}