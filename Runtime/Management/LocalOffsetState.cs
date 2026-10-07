using FloatingOffset.Runtime.Types;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    public class LocalOffsetState
    {
        private IOffsetObject<Scene> mainView = null;
        public Vector3d current_offset = Vector3d.zero;

        public LocalOffsetState(bool logging)
        {
            if (logging)
                UnityEngine.Debug.Log("(Host/Client) Instantiated LocalOffsetState");
        }

        public IOffsetObject<Scene> MainView => mainView;

        public void TrySetMainView(IOffsetObject<Scene> view)
        {
            if (mainView == null)
            {
                if (view.IsPlayer())
                {
                    mainView = view;
                }
            }
        }
        public Scene GetMainSceneKey() => mainView.GetSceneKey();

        public bool IsMainView(IOffsetObject<Scene> offsetObject) => offsetObject == mainView;
    }
}
