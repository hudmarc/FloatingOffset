using FloatingOffset.Runtime.Example;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    public class BasicOffsetManager : OffsetManager
    {
        void Awake()
        {
            if (!enabled)
                return;

            if (handler == null)
                handler = gameObject.AddComponent<BasicOffsetSceneHandler>();

            universe.InitializeWithHandler(handler);
        }
        void Start()
        {
            Physics.simulationMode = SimulationMode.Script;
        }

        void LateUpdate()
        {
            if (universe.ServerActive)
                Process();
        }
        protected void FixedUpdate()
        {
            if (!updateScenePhysicsInternally)
                return;

            handler.PhysicsProcess(Time.fixedDeltaTime);
        }
        
    }
}
