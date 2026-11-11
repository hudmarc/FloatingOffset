using UnityEngine;

namespace FloatingOffset.Runtime.Example
{
    public class OffsetSceneDebug : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            if (OffsetBehaviour.manager == null || !OffsetBehaviour.manager.IsServerActive())
                return;

            var scenes = OffsetBehaviour.manager.GetScenes();

            foreach (var scene in scenes)
            {
                Vector3d offset = OffsetBehaviour.manager.GetOffset(scene);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(OffsetUtils.RealToUnity(offset, OffsetBehaviour.manager.GetLocalScene()), OffsetBehaviour.manager.GetSceneRadius(scene) < OffsetBehaviour.manager.GetOffsetConfiguration().MinimumJoinDistance ? OffsetBehaviour.manager.GetOffsetConfiguration().MinimumJoinDistance : OffsetBehaviour.manager.GetSceneRadius(scene));

                Gizmos.DrawLine(OffsetUtils.RealToUnity(offset, OffsetBehaviour.manager.GetLocalScene()), Vector3.zero);
            }
        }
    }

}
