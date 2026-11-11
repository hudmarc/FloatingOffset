using FloatingOffset.Runtime.Types;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    public class LocalUniverseState<T>
    {
        private IOffsetObject<T> mainView = null;
        public Vector3d current_offset = Vector3d.zero;

        public LocalUniverseState(bool logging)
        {
            if (logging)
                UnityEngine.Debug.Log("(Host/Client) Instantiated LocalOffsetState");
        }

        public IOffsetObject<T> MainView => mainView;

        public void TrySetMainView(IOffsetObject<T> view)
        {
            if (mainView == null)
            {
                if (view.IsPlayer())
                {
                    mainView = view;
                }
            }
        }
        public T GetMainSceneKey() => mainView == null ? default : mainView.GetSceneKey();

        public bool IsMainView(IOffsetObject<Scene> offsetObject) => offsetObject == mainView;
    }
}
