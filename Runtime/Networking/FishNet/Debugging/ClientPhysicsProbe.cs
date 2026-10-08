using FishNet;
using UnityEngine;
public class ClientPhysicsProbe : MonoBehaviour
{
    [SerializeField] Rigidbody playerRb;
    int steps, ticks; uint lastTick; float t;

    void Start()
    {
        if (playerRb == null)
            playerRb = GetComponent<Rigidbody>();
        var tm = InstanceFinder.TimeManager;
        Debug.Log($"PhysicsMode={tm.PhysicsMode} TickRate={tm.TickRate} TickDelta={tm.TickDelta}");
        tm.OnPrePhysicsSimulation += _ =>
        {
            steps++;
            if (tm.LocalTick != lastTick) { ticks++; lastTick = tm.LocalTick; }
        };
    }

    void Update()
    {
        if ((t += Time.unscaledDeltaTime) < 1f) return;
        t = 0;
        Debug.Log($"steps/s={steps} distinctTicks/s={ticks} replaysPerTick={(ticks == 0 ? 0 : (float)steps / ticks - 1):F1}");
        steps = ticks = 0;
    }
}