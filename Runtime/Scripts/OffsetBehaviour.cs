using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    public class OffsetBehaviour : MonoBehaviour
    {
        public static OffsetManager manager { get; private set; }
        protected void RegisterManager(OffsetManager manager) => OffsetBehaviour.manager = manager;
    }
}