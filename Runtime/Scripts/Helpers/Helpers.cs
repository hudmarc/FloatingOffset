using UnityEngine;

namespace FloatingOffset.Runtime
{
    public static class Helpers
    {
        public static PhysicsScene Physics(this GameObject gameObject) => gameObject.scene.GetPhysicsScene();
    }
}
