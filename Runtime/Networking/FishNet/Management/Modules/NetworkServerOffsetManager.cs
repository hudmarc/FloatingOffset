using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FloatingOffset.Runtime.Example.Types;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime.Example
{
    public class NetworkServerOffsetManager : IOffsetHandler<Scene>
    {
        private OffsetServer<Scene> server;
        private Offsetter offsetter;
        private OffsetSceneState sceneState;
        private OffsettableRegistry offsettableRegistry;
        private LocalOffsetState localOffsetState;
        private readonly LoadOptions Options = new LoadOptions
        {
            AllowStacking = true,
            AutomaticallyUnload = false,
            LocalPhysics = LocalPhysicsMode.Physics3D,
        };
        private bool logging;

        #region Fields (scene handler)

        protected Scene last_scene = default;
        protected readonly LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        Queue<Action<Scene>> readyActions = new Queue<Action<Scene>>();

        public NetworkServerOffsetManager(OffsetSceneState state, OffsettableRegistry offsettableRegistry, NetworkManager networkManager, Offsetter offsetter, LocalOffsetState localOffsetState, bool logging)
        {
            this.logging = logging;
            this.sceneState = state;
            this.offsettableRegistry = offsettableRegistry;
            this.offsetter = offsetter;
            this.localOffsetState = localOffsetState;
            if (logging)
                UnityEngine.Debug.Log("(Server) Instantiated NetworkServerOffsetManager");
        }


        #endregion

        #region Lifecycle

        /// <summary>
        /// Runs the Process loop on the OffsetUniverse.
        /// </summary>
        internal void Process() => server.Process();

        /// <summary>
        /// Process physics in stacked scenes
        /// </summary>
        /// <param name="delta"></param>
        internal void PhysicsTick(float delta)
        {
            foreach (var scene in sceneState.offsetScenes)
            {
                if (scene.IsValid() && scene.GetPhysicsScene() != Physics.defaultPhysicsScene)
                    scene.GetPhysicsScene().Simulate(delta);
            }
        }
        internal void RegisterOffsetServer(OffsetServer<Scene> server) => this.server = server;


        #endregion



        #region Scene handling
        protected void SetSceneVisibility(Scene scene, bool visible)
        {
            if (logging)
                Debug.Log($"Changed visibility on {scene.handle.ToHex()} to {visible}");

            var rootobjectsInScene = scene.GetRootGameObjects();
            for (int i = 0; i < rootobjectsInScene.Length; i++)
            {
                SetGameObjectVisible(rootobjectsInScene[i], visible);
            }
        }

        private void SetGameObjectVisible(GameObject gameObject, bool visible)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();

            for (int j = 0; j < renderers.Length; j++)
            {
                renderers[j].enabled = visible;
            }

            if (gameObject.TryGetComponent(out Terrain terrain))
            {
                terrain.enabled = visible;
            }
        }

        private void OnLoadEnd(Scene[] loadedScenes)
        {
            foreach (Scene scene in loadedScenes)
            {
                if (logging)
                    Debug.Log($"Loaded scene {scene.GetHashCode().ToHex()}");
                if (readyActions.Count > 0)
                {
                    readyActions.Dequeue()(scene);
                    SetSceneVisibility(scene, false); // All scenes should start invisible on the host, they only become visible when the host moves into them.
                }
            }
            last_scene = default;
        }
        public void OnLoadEnd(SceneLoadEndEventArgs data)
        {
            if (logging)
                foreach (var scene in data.LoadedScenes)
                {
                    Debug.Log($"Scene loaded (FishNet) {scene.handle.GetHashCode()}");
                }

            OnLoadEnd(data.LoadedScenes);
        }

        public void QueueSceneLoadCallback(Action<Scene> onSceneReady) => readyActions.Enqueue(onSceneReady);

        #endregion

        #region IOffsetHandler<Scene>

        public void UpdateOffset(OffsetScene<Scene> scene)
        {
            var key = scene.key;
            if (sceneState.TryAddOffset(key))
            {
                if (scene.offset == sceneState.GetOffset(scene.key))
                    return;
            }

            if (Vector3d.SquaredMagnitude(sceneState.GetOffset(key) - scene.offset) < 1.0d)
                return;

            if (logging)
                Debug.Log($"({InstanceFinder.TimeManager.Tick}) OFFSET: [{scene.key.handle.ToHex()}]\n{sceneState.GetOffset(scene.key):#.#}->{scene.offset:#.#} ");
            Vector3d old_offset = sceneState.GetOffset(key);
            sceneState.SetOffset(key, scene.offset);

            if (offsettableRegistry.GetOffsettablesInScene(scene.key, out var offsettablesInScene))
            {
                offsetter.Offset(old_offset, sceneState.GetOffset(key), scene.key, offsettablesInScene);
            }
            else
            {
                offsetter.Offset(old_offset, sceneState.GetOffset(key), scene.key);
            }

            var objects = scene.key.GetRootGameObjects();

            foreach (var obj in objects)
            {
                if (obj.TryGetComponent(out OffsetView view) && obj.TryGetComponent(out NetworkObject nob))
                {
                    if (obj.TryGetComponent(out NetworkTransform nt))
                    {
                        if (logging)
                            Debug.Log($"Teleported NT {obj.name} on offset");
                        nt.Teleport();
                    }

                    if (view.IsPlayer && !nob.IsOwner) //only synchronize offsets to scenes observed by players
                    {
                        ReceiveOffsetBroadcast responseMsg = new ReceiveOffsetBroadcast
                        {
                            OffsetX = scene.offset.x,
                            OffsetY = scene.offset.y,
                            OffsetZ = scene.offset.z,
                            ViewNob = nob,
                            Tick = InstanceFinder.TimeManager.Tick
                        };

                        if (logging)
                            Debug.Log("Sent broadcast to client");
                        nob.Owner.Broadcast(responseMsg);
                    }
                }
            }
        }

        public void TransferTo(IOffsetObject<Scene> offsetObject, Scene from, Scene to)
        {
            Vector3d absoluteRealPos = sceneState.GetOffset(from) + offsetObject.GetEnginePosition();

            MonoBehaviour offsetMono = (MonoBehaviour)offsetObject;

            offsetObject.OnPreSceneTransfer();

            if (offsetMono.TryGetComponent(out NetworkObject nob))
            {
                if (!nob.IsOwner && offsetObject.IsPlayer())
                {
                    SceneLoadData to_sld = new SceneLoadData(to)
                    {
                        Options = Options
                    };
                    InstanceFinder.SceneManager.LoadConnectionScenes(nob.Owner, to_sld); // Load the target scene on the client
                    InstanceFinder.SceneManager.RemoveConnectionsFromScene(new FishNet.Connection.NetworkConnection[] { nob.Owner }, from);

                    Vector3d toOffset = sceneState.GetOffset(to);

                    ReceiveOffsetBroadcast responseMsg = new ReceiveOffsetBroadcast
                        {
                            OffsetX = toOffset.x,
                            OffsetY = toOffset.y,
                            OffsetZ = toOffset.z,
                            ViewNob = nob,
                            Tick = InstanceFinder.TimeManager.Tick
                        };

                        if (logging)
                            Debug.Log("Sent broadcast to client");
                        nob.Owner.Broadcast(responseMsg);
                }

                
                InstanceFinder.ServerManager.Objects.RebuildObservers();
            }

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(offsetMono.gameObject, to);
            Vector3d newUnityPos = absoluteRealPos - sceneState.GetOffset(to);
            offsetObject.SetEnginePosition(newUnityPos);

            offsetObject.OnSceneTransfer();

            if (logging)
                Debug.Log($"Transferred {offsetMono.name} from {from.handle.ToHex()} {sceneState.GetOffset(from)} to {to.handle.ToHex()} {sceneState.GetOffset(to)} ");

            

            Scene main_scene = localOffsetState.GetMainSceneKey();

            if (localOffsetState.IsMainView(offsetObject))
            {
                // Only update the total visibility of scenes when the main view changes between scenes.
                SetSceneVisibility(from, false);
                SetSceneVisibility(to, true);
            }
            else
            {
                if (to == main_scene)
                    SetGameObjectVisible(offsetMono.gameObject, true); // Non-main views become visible when entering the main view's scene.
                else
                    SetGameObjectVisible(offsetMono.gameObject, false); // Non-main views become invisible when leaving the main view's scene.
            }
        }

        /// <summary>
        /// Clone the given scene and clears it of OffsetViews. Calls the callback when done.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="onSceneReady"></param>
        public void Clone(Scene scene, Action<Scene> onSceneReady)
        {
            if (logging)
                Debug.Log($"Attempting to clone {scene.name}");

            float start_time = Time.time;

            if (last_scene == scene)
            {
                if (logging)
                    Debug.LogWarning($"Prevented double execution of completed callback by SceneManager LoadSceneAsync on scene {scene.handle.ToHex()}");
                return;
            }
            last_scene = scene;

            SceneLoadData sld = new SceneLoadData(scene.name)
            {
                Options = Options
            };

            InstanceFinder.SceneManager.LoadConnectionScenes(sld);
            QueueSceneLoadCallback(onSceneReady);
        }

        public void Unload(Scene scene)
        {
            if (logging)
                Debug.Log($"Unloading {scene.name}");

            SceneUnloadData sud = new SceneUnloadData(scene);
            InstanceFinder.SceneManager.UnloadGlobalScenes(sud);
        }

        #endregion
    }
}
