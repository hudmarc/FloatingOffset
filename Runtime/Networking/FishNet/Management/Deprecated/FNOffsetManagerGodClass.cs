using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FishNet;
using FishNet.Broadcast;
using FishNet.Component.Transforming;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using FloatingOffset.Runtime.Example.Types;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime.Example
{
    /// <summary>
    /// Bundles all the functionality of the FishNetOffsetManager in one class.
    /// </summary>
    [Obsolete("Semi-broken. For reference only. Use FishNetOffsetManager")]
    public class FNOffsetManagerGodClass : OffsetManager, IOffsetHandler<Scene>
    {
        #region Fields (manager)
        [SerializeField]
        private OffsetConfiguration configuration;
        private OffsetServer<Scene> server;

        private Vector3d current_offset = Vector3d.zero;
        private NetworkManager networkManager;
        public Offsetter offsetter;

        public Action OnStartServer;
        protected List<IOffsettable<Scene>> offsettables = new List<IOffsettable<Scene>>();
        List<IOffsettable<Scene>> temp = new List<IOffsettable<Scene>>();

        #endregion

        #region Fields (state)

        // Replaces the old "state == null" checks: true once the server has started and the universe is initialized.
        private bool stateInitialized = false;
        protected Dictionary<Scene, Vector3d> current_offsets = new Dictionary<Scene, Vector3d>();
        public IOffsetObject<Scene> mainView = null;
        public override bool IsServerActive() => stateInitialized;
        #endregion

        #region Fields (scene handler)

        protected Scene last_scene = default;
        protected readonly LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        Queue<Action<Scene>> readyActions = new Queue<Action<Scene>>();

        #endregion

        #region Lifecycle

        void Awake()
        {
            if (!enabled)
                return;

            if (offsetter == null)
                offsetter = gameObject.GetComponent<Offsetter>();

            if (TryGetComponent(out networkManager))
            {
                networkManager.TimeManager.SetPhysicsMode(FishNet.Managing.Timing.PhysicsMode.TimeManager);
                networkManager.ServerManager.OnServerConnectionState += OnServerStateChange;
                networkManager.ClientManager.OnClientConnectionState += OnClientStateChange;
            }

            RegisterManager(this);
        }

        void OnDestroy()
        {
            networkManager.ServerManager.OnServerConnectionState -= OnServerStateChange;
            networkManager.ClientManager.OnClientConnectionState -= OnClientStateChange;
        }

        // Called on server
        private void OnServerStateChange(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                stateInitialized = true;

                server = new OffsetServer<Scene>(this, configuration.MinimumJoinDistance, configuration.MaxScenes, configuration.Hysteresis);

                if (configuration.logging)
                    Debug.Log("Initialized universe");

                networkManager.TimeManager.OnPreTick += Process;
                networkManager.TimeManager.OnPrePhysicsSimulation += PhysicsTick;

                InstanceFinder.SceneManager.OnLoadEnd += OnLoadEnd;
            }
            else if (args.ConnectionState == LocalConnectionState.Stopping)
            {
                networkManager.TimeManager.OnPreTick -= Process;
                networkManager.TimeManager.OnPrePhysicsSimulation -= PhysicsTick;

                InstanceFinder.SceneManager.OnLoadEnd -= OnLoadEnd;
            }
        }

        private void OnClientStateChange(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started && !stateInitialized)
            {
                networkManager.ClientManager.RegisterBroadcast<ReceiveOffsetBroadcast>(OnClientReceivedOffset);
            }
            else if (args.ConnectionState == LocalConnectionState.Stopping && !stateInitialized)
            {
                networkManager.ClientManager.UnregisterBroadcast<ReceiveOffsetBroadcast>(OnClientReceivedOffset);
            }
        }

        /// <summary>
        /// Runs the Process loop on the OffsetUniverse.
        /// </summary>
        protected void Process() => server.Process();

        // Renamed from "Physics" (it would shadow UnityEngine.Physics inside this merged class).
        private void PhysicsTick(float delta) => PhysicsProcess(delta);

        #endregion

        #region View registration / manager facade

        public override void RegisterView(OffsetView view)
        {
            if (configuration.logging)
                Debug.Log($"Registered View {view.GetName()}");

            SetupViewBeforeRegister(view);

            if (stateInitialized)
            {
                server.RegisterView(view);
                if (mainView == null)
                {
                    if (view.IsPlayer)
                    {
                        mainView = view;
                    }
                }

            }
        }

        public override void UnregisterView(OffsetView view)
        {
            if (stateInitialized)
                server.UnregisterView(view);
        }

        /// <summary>
        /// Called immediately before RegisterView is called.
        /// </summary>
        public void SetupViewBeforeRegister(IOffsetObject<Scene> view)
        {
            if (!networkManager.IsServerStarted)
                throw new System.Exception($"Attempted to register view on {view.GetName()} before NetworkServer was ready.");

            GameObject go = ((MonoBehaviour)view).gameObject;

            var nob = go.GetComponent<NetworkObject>();

            if (nob != null && !nob.IsOwner)
            {
                Vector3d initial_offset = GetOffset(go.scene);

                StartCoroutine(SendInitialOffsetWhenReady(nob, initial_offset));
            }
        }

        /// <summary>
        /// Teleport the given OffsetView view to the given position in space.
        /// </summary>
        public override void TeleportTo(OffsetView view, Vector3d position)
        {
            if (stateInitialized)
            {
                server.TeleportTo(view, position);
                if (configuration.logging)
                    Debug.Log($"Teleported {view.name} to {position}");
            }
        }

        public int CountRegisteredViews() => server.RegisteredViewCount();

        public int CountViews() => server.ActualViewCount();

        public override Vector3d GetLocalOffset(Scene scene)
        {
            return !stateInitialized ? current_offset : GetOffset(scene);
        }

        #endregion

        #region Offsettable registry

        public override void RegisterOffsettable(IOffsettable<Scene> offsettable, Scene scene) => offsettables.Add(offsettable);

        public override void UnregisterOffsettable(IOffsettable<Scene> offsettable, Scene scene) => offsettables.Remove(offsettable);

        public int OffsettableCount() => offsettables.Count;

        public bool GetOffsettablesInScene(Scene scene, out ReadOnlyCollection<IOffsettable<Scene>> found)
        {
            while (offsettables.Count > 0 && offsettables[offsettables.Count - 1] == null)
            {
                offsettables.RemoveAt(offsettables.Count - 1);
            }
            temp.Clear();

            for (int i = 0; i < offsettables.Count; i++)
            {
                var offsettable = offsettables[i];

                if (offsettable is UnityEngine.Object unityObj && unityObj == null)
                {
                    offsettables.RemoveAt(i);
                    continue;
                }

                if (!offsettable.IsValid())
                {
                    offsettables.RemoveAt(i);
                    continue;
                }

                if (offsettable.GetSceneKey() == scene)
                {
                    temp.Add(offsettable);
                }
            }

            found = temp.AsReadOnly();

            if (temp.Count < 1)
                return false;
            return true;
        }

        #endregion

        #region Offset state (scene -> offset, main view)

        public Vector3d GetOffset(Scene scene) => current_offsets.ContainsKey(scene) ? current_offsets[scene] : Vector3d.zero;

        public void SetOffset(Scene key, Vector3d offset)
        {
            current_offsets[key] = offset;
        }

        public override bool HasScene(Scene scene) => current_offsets.ContainsKey(scene);

        public bool TryAddOffset(Scene key)
        {
            if (current_offsets.ContainsKey(key))
                return false;
            else
            {
                current_offsets.Add(key, Vector3d.zero);
                return true;
            }
        }

        public Scene GetMainSceneKey() => mainView.GetSceneKey();

        public bool IsMainView(IOffsetObject<Scene> offsetObject) => offsetObject == mainView;

        #endregion

        #region Scene handling (physics, visibility, load callbacks)

        /// <summary>
        /// Process physics in stacked scenes
        /// </summary>
        /// <param name="delta"></param>
        virtual public void PhysicsProcess(float delta)
        {
            if (!stateInitialized)
                return;
            foreach (var scene in current_offsets.Keys)
            {
                if (scene.IsValid() && scene.GetPhysicsScene() != Physics.defaultPhysicsScene)
                    scene.GetPhysicsScene().Simulate(delta);
            }
        }

        protected void SetSceneVisibility(Scene scene, bool visible)
        {
            if (configuration.logging)
                Debug.Log($"Changed visibility on {scene.handle.ToHex()} to {visible}");

            var rootobjectsInScene = scene.GetRootGameObjects();
            for (int i = 0; i < rootobjectsInScene.Length; i++)
            {
                Renderer[] renderers = rootobjectsInScene[i].GetComponentsInChildren<Renderer>();

                for (int j = 0; j < renderers.Length; j++)
                {
                    renderers[j].enabled = visible;
                }

                if (rootobjectsInScene[i].TryGetComponent(out Terrain terrain))
                {
                    terrain.enabled = visible;
                }
            }
        }

        public void OnLoadEnd(Scene[] loadedScenes)
        {
            foreach (Scene scene in loadedScenes)
            {
                Debug.Log($"Loaded scene {scene.GetHashCode().ToHex()}");
                if (readyActions.Count > 0)
                    readyActions.Dequeue()(scene);
            }
            last_scene = default;
        }

        // FishNet event handler (overload of OnLoadEnd above)
        private void OnLoadEnd(SceneLoadEndEventArgs data)
        {
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
            if (TryAddOffset(key))
            {
                if (scene.offset == GetOffset(scene.key))
                    return;
            }

            if (Vector3d.SquaredMagnitude(GetOffset(key) - scene.offset) < 1.0d)
                return;

            if (configuration.logging)
                Debug.Log($"({InstanceFinder.TimeManager.Tick}) OFFSET: [{scene.key.handle.ToHex()}]\n{GetOffset(scene.key):#.#}->{scene.offset:#.#} ");
            Vector3d old_offset = GetOffset(key);
            SetOffset(key, scene.offset);

            if (GetOffsettablesInScene(scene.key, out var offsettablesInScene))
            {
                offsetter.Offset(old_offset, GetOffset(key), scene.key, offsettablesInScene);
            }
            else
            {
                offsetter.Offset(old_offset, GetOffset(key), scene.key);
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
                        OffsetZ = scene.offset.z,
                        ViewNob = nob,
                        Tick = InstanceFinder.TimeManager.Tick
                    };
                    if (configuration.logging)
                        Debug.Log("Sent broadcast to client");
                    if (nob.Owner.IsValid)
                        nob.Owner.Broadcast(responseMsg);
                }
            }
        }

        // OLD Runs on the server
        public void TransferTo(IOffsetObject<Scene> offsetObject, Scene from, Scene to)
        {
            Vector3d absoluteRealPos = GetOffset(from) + offsetObject.GetEnginePosition();

            MonoBehaviour offsetMono = (MonoBehaviour)offsetObject;

            offsetObject.OnPreSceneTransfer();

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(offsetMono.gameObject, to);

            Vector3d newUnityPos = absoluteRealPos - GetOffset(to);
            offsetObject.SetEnginePosition(newUnityPos);

            offsetObject.OnSceneTransfer();

            Scene main_scene = GetMainSceneKey();

            if (IsMainView(offsetObject))
            {
                SetSceneVisibility(from, false);
                SetSceneVisibility(to, true);
            }
            else
            {
                SetSceneVisibility(from, from == main_scene);
                SetSceneVisibility(to, to == main_scene);
            }

            if (configuration.logging)
                Debug.Log($"Transferred {offsetMono.name} from {from.handle.ToHex()} {GetOffset(from)} to {to.handle.ToHex()} {GetOffset(to)} ");

            if (offsetMono.TryGetComponent(out NetworkObject nob))
            {
                if (nob.Owner != null && !nob.IsOwner) //maybe this should also check if the view is a player?
                {
                    InstanceFinder.SceneManager.RemoveConnectionsFromScene(new FishNet.Connection.NetworkConnection[] { nob.Owner }, from);
                    InstanceFinder.SceneManager.AddConnectionToScene(nob.Owner, to);
                }
                InstanceFinder.ServerManager.Objects.RebuildObservers();

                if (nob.TryGetComponent(out NetworkTransform nt))
                    nt.Teleport();

                if (!nob.IsOwner)
                {
                    var offset = GetOffset(to);
                    ReceiveOffsetBroadcast to_msg = new ReceiveOffsetBroadcast
                    {
                        OffsetX = offset.x,
                        OffsetY = offset.y,
                        OffsetZ = offset.z,
                        ViewNob = nob,
                        Tick = InstanceFinder.TimeManager.Tick
                    };

                    if (nob.Owner.IsValid)
                        nob.Owner.Broadcast(to_msg);
                }
            }
        }

        /// <summary>
        /// OLD Clone the given scene and clears it of OffsetViews. Calls the callback when done.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="onSceneReady"></param>
        public void Clone(Scene scene, Action<Scene> onSceneReady)
        {
            Debug.Log($"Attempting to clone {scene.name}");

            float start_time = Time.time;
            if (!stateInitialized)
            {
                Debug.LogError("Scene cloning must be executed on the server");
                return;
            }

            if (last_scene == scene)
            {
                if (configuration.logging)
                    Debug.LogWarning($"Prevented double execution of completed callback by SceneManager LoadSceneAsync on scene {scene.handle.ToHex()}");
                return;
            }
            last_scene = scene;

            // this is called twice if the editor is unfocused. seems to be a Unity bug.
            // we load the scene with the Unity scene manager on the server first (since we are creating a clone of an existing scene)
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene.buildIndex, parameters).completed += (arg) =>
            {
                Scene loaded_scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(UnityEngine.SceneManagement.SceneManager.sceneCount - 1);
                OnLoadEnd(new Scene[] { loaded_scene });

                //then we register the scene with FishNet
                SceneLoadData sld = new SceneLoadData(scene)
                {
                    Options = new LoadOptions
                    {
                        AllowStacking = true,
                        AutomaticallyUnload = false,
                        LocalPhysics = LocalPhysicsMode.Physics3D,
                    }
                };

                InstanceFinder.SceneManager.LoadConnectionScenes(sld);
            };
            QueueSceneLoadCallback(onSceneReady);
        }

        public void Unload(Scene scene)
        {
            Debug.Log($"Unloading {scene.name}");

            if (!stateInitialized)
            {
                Debug.LogWarning("Scene unloading must be executed on the server");
                return;
            }

            SceneUnloadData sud = new SceneUnloadData(scene);
            InstanceFinder.SceneManager.UnloadGlobalScenes(sud);
        }

        #endregion

        #region Networking (initial offset / client receive)

        private IEnumerator SendInitialOffsetWhenReady(NetworkObject nob, Vector3d initial_offset)
        {
            int timeout = 240;
            while (!nob.Owner.IsActive && timeout > 0)
            {
                yield return null;
                timeout--;
            }
            if (!nob.Owner.IsValid)
            {
                yield break;
            }
            if (timeout < 1)
                Debug.LogWarning("Network object owner did not resolve within 240 ticks, initial scene offset may be incorrect.");
            ReceiveOffsetBroadcast responseMsg = new ReceiveOffsetBroadcast
            {
                OffsetX = initial_offset.x,
                OffsetY = initial_offset.y,
                OffsetZ = initial_offset.z,
                ViewNob = nob,
                Tick = InstanceFinder.TimeManager.Tick
            };

            Debug.Log($"({InstanceFinder.TimeManager.Tick}) Sending initial offset {initial_offset} to client {nob.Owner}");

            nob.Owner.Broadcast(responseMsg);

            OnStartServer?.Invoke();
        }

        private void OnClientReceivedOffset(ReceiveOffsetBroadcast msg, Channel channel)
        {
            StartCoroutine(OnClientReceivedOffsetRoutine(msg, channel));
        }

        private IEnumerator OnClientReceivedOffsetRoutine(ReceiveOffsetBroadcast msg, Channel channel)
        {
            int timeout = 240;
            while (msg.Tick > InstanceFinder.TimeManager.LocalTick && timeout > 0)
            {
                yield return null;
                timeout--;
            }
            if (timeout < 1)
                Debug.LogWarning("Server tick more than 240 frames behind client, undefined behavior may occur with offset system.");
            var new_offset = new Vector3d(msg.OffsetX, msg.OffsetY, msg.OffsetZ);
            if (configuration.logging)
                Debug.Log($"({InstanceFinder.TimeManager.Tick}) OFFSET CLIENT: [Local Scene]\n{current_offset}->{new_offset} ]");
            if (GetOffsettablesInScene(msg.ViewNob.gameObject.scene, out var offsettablesInScene))
            {
                offsetter.Offset(current_offset, new_offset, msg.ViewNob.gameObject.scene, offsettablesInScene);
            }
            else
            {
                offsetter.Offset(current_offset, new_offset, msg.ViewNob.gameObject.scene);
            }

            current_offset = new_offset;
        }

        public override bool IsLogging() => configuration.logging;


        #endregion
    }
}