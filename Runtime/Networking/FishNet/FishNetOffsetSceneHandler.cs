using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing.Scened;
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
            {
                AddOffset(key);
            }
            else if (scene.offset == current_offsets[scene.key])
                return;
            if (universe.logging)
                Debug.Log($"OFFSET: [{scene.key.handle.ToHex()}]\n{current_offsets[key]:#.#}->{scene.offset:#.#} ");
            Vector3d old_offset = current_offsets[key];
            current_offsets[key] = scene.offset;

            if (offsettables.TryGetValue(scene.key, out List<IOffsettable<Scene>> list))
            {
                offsetter.Offset(old_offset, current_offsets[key], scene.key, list.ToArray());
            }
            else
            {
                offsetter.Offset(old_offset, current_offsets[key], scene.key);
            }

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
                    if (nob.Owner.IsValid)
                        nob.Owner.Broadcast(responseMsg);
                }
            }
        }
        // Runs on the server
        public void TransferTo(IOffsetObject<Scene> offsetObject, Scene from, Scene to, bool reposition = false)
        {
            Vector3d absoluteRealPos = current_offsets[from] + offsetObject.GetEnginePosition();

            MonoBehaviour offsetMono = (MonoBehaviour)offsetObject;

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(offsetMono.gameObject, to);

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
                Debug.Log($"Transferred {offsetMono.name} from {from.handle.ToHex()} to {to.handle.ToHex()}");

            if (offsetMono.TryGetComponent(out NetworkObject nob))
            {
                SceneLoadData sld = new SceneLoadData(to)
                {
                    Options = new LoadOptions
                    {
                        AllowStacking = true,
                        AutomaticallyUnload = false,
                        LocalPhysics = LocalPhysicsMode.Physics3D,
                    },
                    MovedNetworkObjects = new NetworkObject[]{nob}
                };
                // load on target client
                InstanceFinder.SceneManager.LoadConnectionScenes(nob.Owner, sld);

                if (!nob.IsOwner)
                {
                    ReceiveOffsetBroadcast to_msg = new ReceiveOffsetBroadcast
                    {
                        OffsetX = current_offsets[to].x,
                        OffsetY = current_offsets[to].y,
                        OffsetZ = current_offsets[to].z
                    };

                    if (nob.Owner.IsValid)
                        nob.Owner.Broadcast(to_msg);
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
            if (!universe.ServerActive)
            {
                Debug.LogError("Scene cloning must be executed on the server");
                return;
            }

            if (last_scene == scene)
            {
                if (universe.logging)
                    Debug.LogWarning($"Prevented double execution of completed callback by SceneManager LoadSceneAsync on scene {scene.handle.ToHex()}");
                return;
            }

            SceneLoadData sld = new SceneLoadData(scene.name)
            {
                Options = new LoadOptions
                {
                    AllowStacking = true,
                    AutomaticallyUnload = false,
                    LocalPhysics = LocalPhysicsMode.Physics3D
                }
            };

            InstanceFinder.SceneManager.LoadConnectionScenes(sld);
            QueueSceneLoadCallback(onSceneReady);

            last_scene = scene;
        }

        public void Unload(Scene scene)
        {
            Debug.Log($"Unloading {scene.name}");

            if (!universe.ServerActive)
            {
                Debug.LogWarning("Scene unloading must be executed on the server");
                return;
            }

            SceneUnloadData sud = new SceneUnloadData(scene);
            InstanceFinder.SceneManager.UnloadConnectionScenes(sud);
        }
    }
}
