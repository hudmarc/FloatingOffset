using System;
using FishNet.Object;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime.Example
{
    public class FishNetOffsetSceneHandler : OffsetSceneHandler, IOffsetHandler<Scene>
    {
        private Scene last_scene = default;
        private Vector3d old_offset = Vector3d.zero;
        public void UpdateOffset(OffsetScene<Scene> scene)
        {
            var key = scene.key;
            if (!current_offsets.ContainsKey(key))
                current_offsets.Add(key, Vector3d.zero);
            else if (scene.offset == current_offsets[scene.key])
                return;

            var objects = scene.key.GetRootGameObjects();

            foreach (var obj in objects)
            {
                if (obj.TryGetComponent(out OffsetView trf) && obj.TryGetComponent(out NetworkObject nob))
                {
                    if (nob.IsOwner) //don't send to server's client
                        break;

                    ReceiveOffsetBroadcast responseMsg = new ReceiveOffsetBroadcast
                    {
                        OffsetX = scene.offset.x,
                        OffsetY = scene.offset.y,
                        OffsetZ = scene.offset.z
                    };
                    if (universe.logging)
                        Debug.Log("Sent broadcast to client");

                    nob.Owner.Broadcast(responseMsg);
                }
            }
        }
        // Runs on the server
        public void TransferTo(IOffsetObject<Scene> offsetObject, Scene from, Scene to, bool reposition = false)
        {
            Vector3d absoluteRealPos = current_offsets[from] + offsetObject.GetEnginePosition();

            offsetObject.SetSceneKey(to);

            // Calculate the exact local Unity position required for the new scene
            // Because Real = Unity + Offset, therefore Unity = Real - Offset
            if (reposition)
            {
                Vector3d newUnityPos = absoluteRealPos - current_offsets[to];

                offsetObject.SetEnginePosition(newUnityPos);
            }
            Scene main_scene = mainView.GetSceneKey();

            if (offsetObject == mainView)
            {
                SetSceneVisibility(from, false);
                SetSceneVisibility(to, true);
            }
            else
            {
                SetSceneVisibility(from, from == main_scene);
                SetSceneVisibility(to, to == main_scene);
            }



            if (universe.logging)
                Debug.Log($"Transferred {((MonoBehaviour)offsetObject).name} from {from.handle.ToHex()} to {to.handle.ToHex()}");
            if (((OffsetView)offsetObject).TryGetComponent(out NetworkObject nob))
            {
                if (!nob.IsOwner)
                {
                    ReceiveOffsetBroadcast to_msg = new ReceiveOffsetBroadcast
                    {
                        OffsetX = current_offsets[to].x,
                        OffsetY = current_offsets[to].y,
                        OffsetZ = current_offsets[to].z
                    };

                    nob.Owner.Broadcast(to_msg); //instruct the owner to offset
                }
            }
        }

        public new Vector3d GetOffset(Scene scene)
        {
            if (universe.ServerActive)
                if (current_offsets.TryGetValue(scene, out Vector3d offset))
                {
                    return offset;
                }
                else return Vector3d.zero;
            return old_offset;
        }

        /// <summary>
        /// Clone the given scene and clears it of OffsetViews. Calls the callback when done.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="onSceneReady"></param>
        public void Clone(Scene scene, Action<Scene> onSceneReady)
        {
            float start_time = Time.time;
            if (last_scene == scene)
            {
                if (universe.logging)
                    Debug.LogWarning($"Prevented double execution of completed callback by SceneManager LoadSceneAsync on scene {scene.handle.ToHex()}");
                return;
            }
            last_scene = scene;
            // this is called twice if the editor is unfocused. seems to be a Unity bug.
            SceneManager.LoadSceneAsync(scene.buildIndex, parameters).completed += (arg) => SetupScene(onSceneReady, start_time);
        }

        public void Unload(Scene scene)
        {
            SceneManager.UnloadSceneAsync(scene);
        }
    }
}
