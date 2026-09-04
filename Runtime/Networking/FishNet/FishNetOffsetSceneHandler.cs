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
    public class FishNetOffsetSceneHandler : AbstractOffsetSceneHandler, IOffsetHandler<Scene>
    {
        private Scene last_scene = default;
        public void UpdateOffset(OffsetScene<Scene> scene)
        {
            var key = scene.key;
            if (state.TryAddOffset(key))
            {
                if (scene.offset == state.GetOffset(scene.key))
                    return;
            }
            if (universe.logging)
                Debug.Log($"OFFSET: [{scene.key.handle.ToHex()}]\n{state.GetOffset(scene.key):#.#}->{scene.offset:#.#} ");
            Vector3d old_offset = state.GetOffset(key);
            state.SetOffset(key, scene.offset);

            if (state.TryGetOffsettable(scene.key, out List<IOffsettable<Scene>> list))
            {
                offsetter.Offset(old_offset, state.GetOffset(key), scene.key, list.ToArray());
            }
            else
            {
                offsetter.Offset(old_offset, state.GetOffset(key), scene.key);
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
            Vector3d absoluteRealPos = state.GetOffset(from) + offsetObject.GetEnginePosition();

            MonoBehaviour offsetMono = (MonoBehaviour)offsetObject;

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(offsetMono.gameObject, to);

            // Calculate the exact local Unity position required for the new scene
            // Because Real = Unity + Offset, therefore Unity = Real - Offset
            if (reposition)
            {
                Vector3d newUnityPos = absoluteRealPos - state.GetOffset(to);
                offsetObject.SetEnginePosition(newUnityPos);
            }

            Scene main_scene = state.GetMainSceneKey();

            if (state.IsMainView(offsetObject))
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
                    MovedNetworkObjects = new NetworkObject[] { nob }
                };
                // load on target client
                InstanceFinder.SceneManager.LoadConnectionScenes(nob.Owner, sld);

                if (!nob.IsOwner)
                {
                    var offset = state.GetOffset(to);
                    ReceiveOffsetBroadcast to_msg = new ReceiveOffsetBroadcast
                    {
                        OffsetX = offset.x,
                        OffsetY = offset.y,
                        OffsetZ = offset.z
                    };

                    if (nob.Owner.IsValid)
                        nob.Owner.Broadcast(to_msg);
                }
            }
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
