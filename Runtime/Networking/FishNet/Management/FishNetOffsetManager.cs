using System.Collections;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using FloatingOffset.Runtime.Example.Types;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime.Example
{
    /// <summary>
    /// Bootstraps the Offset System for use with the FishNet networking library.
    /// </summary>
    public class FishNetOffsetManager : OffsetManager
    {
        #region Fields
        [SerializeField]
        private OffsetConfiguration configuration = new OffsetConfiguration();
        #endregion

        #region Components
        /// <summary>
        /// (Server/Client) Stored reference to the FishNet network manager.
        /// </summary>
        private NetworkManager networkManager;
        /// <summary>
        /// (Server/Client) Handles scene offsetting.
        /// </summary>
        private Offsetter offsetter;
        #endregion

        #region Plain C# Classes
        /// <summary>
        /// (Server) The OffsetServer used for calculating offsets.
        /// </summary>
        private OffsetServer<Scene> server;
        /// <summary>
        /// (Host/Client) Keeps track of the local offset of the MainView's scene.
        /// </summary>
        private LocalOffsetState localOffsetState;
        /// <summary>
        /// (Server/Client) Tracks registered offsettables and associates them to scenes.
        /// </summary>
        private OffsettableRegistry offsettableRegistry;
        /// <summary>
        /// (Server/Client) Tracks registered views and associates them to scenes.
        /// </summary>
        private OffsetViewRegistry offsetViewRegistry;
        /// <summary>
        /// (Server) Handles server-side networking functionality.
        /// </summary>
        private NetworkServerOffsetManager networkServerOffsetManager;
        /// <summary>
        /// (Client) Handles client-side networking functionality.
        /// </summary>
        private NetworkClientOffsetManager networkClientOffsetManager;
        /// <summary>
        /// (Server) Tracks the state of all offset scenes on the server.
        /// </summary>
        private OffsetSceneState offsetSceneState;

        #endregion

        void Awake()
        {
            this.localOffsetState = new LocalOffsetState(configuration.logging);
            this.offsettableRegistry = new OffsettableRegistry(configuration.logging);
            this.offsetViewRegistry = new OffsetViewRegistry(localOffsetState, configuration.logging);

            if (offsetter == null)
                offsetter = gameObject.GetComponent<Offsetter>();

            if (TryGetComponent(out networkManager))
            {
                networkManager.TimeManager.SetPhysicsMode(FishNet.Managing.Timing.PhysicsMode.TimeManager);
                networkManager.ServerManager.OnServerConnectionState += OnServerStateChange;
                networkManager.ClientManager.OnClientConnectionState += OnClientStateChange;
            }

            RegisterManager(this); //this way clients can still query their real offsets
        }
        // Called on server
        private void OnServerStateChange(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                if (configuration.logging)
                    Debug.Log("Initialized universe");

                offsetSceneState = new OffsetSceneState();

                networkServerOffsetManager = new NetworkServerOffsetManager(offsetSceneState, offsettableRegistry, networkManager, offsetter, localOffsetState, configuration.logging);

                this.server = new OffsetServer<Scene>(networkServerOffsetManager, configuration.MinimumJoinDistance, configuration.MaxScenes, configuration.Hysteresis);

                networkServerOffsetManager.RegisterOffsetServer(this.server);


                networkManager.TimeManager.OnPreTick += networkServerOffsetManager.Process;
                networkManager.TimeManager.OnPrePhysicsSimulation += networkServerOffsetManager.PhysicsTick;
                InstanceFinder.SceneManager.OnLoadEnd += networkServerOffsetManager.OnLoadEnd;

                offsetViewRegistry.RegisterServer(server);

                if (configuration.logging)
                    Debug.Log("Offset server initialized as server");

                OnOffsetServerInitialized?.Invoke(); // Initialized as server
            }
            else if (args.ConnectionState == LocalConnectionState.Stopping)
            {
                networkManager.TimeManager.OnPreTick -= networkServerOffsetManager.Process;
                networkManager.TimeManager.OnPrePhysicsSimulation -= networkServerOffsetManager.PhysicsTick;
                InstanceFinder.SceneManager.OnLoadEnd -= networkServerOffsetManager.OnLoadEnd;
            }
        }
        // Called on server and client
        private void OnClientStateChange(ClientConnectionStateArgs args)
        {
            // Called on client only
            if (args.ConnectionState == LocalConnectionState.Started && networkServerOffsetManager == null)
            {
                networkClientOffsetManager = new NetworkClientOffsetManager(configuration.logging, localOffsetState, offsettableRegistry, offsetter);
                networkManager.ClientManager.RegisterBroadcast<ReceiveOffsetBroadcast>(OnClientReceivedOffset);
            }
            else if (args.ConnectionState == LocalConnectionState.Stopping && networkServerOffsetManager == null)
            {
                networkManager.ClientManager.UnregisterBroadcast<ReceiveOffsetBroadcast>(OnClientReceivedOffset);
            }
        }
        // client-side
        public void OnClientReceivedOffset(ReceiveOffsetBroadcast msg, Channel channel)
        {
            networkClientOffsetManager.OffsetClient(msg);
            if (msg.Tick == 0)
                OnInitialOffset?.Invoke();
        }
        void OnDestroy()
        {
            networkManager.ServerManager.OnServerConnectionState -= OnServerStateChange;
            networkManager.ClientManager.OnClientConnectionState -= OnClientStateChange;
        }

        /// <summary>
        /// Called immediately before RegisterView is called.
        /// </summary>
        public void SetupViewBeforeRegister(IOffsetObject<Scene> view)
        {
            GameObject go = ((MonoBehaviour)view).gameObject;
            var nob = go.GetComponent<NetworkObject>();

            if (nob != null && !nob.IsOwner)
            {
                Vector3d initial_offset = offsetSceneState.GetOffset(go.scene);

                StartCoroutine(SendInitialOffsetWhenReady(nob, initial_offset));
            }
        }

        private IEnumerator SendInitialOffsetWhenReady(NetworkObject nob, Vector3d initial_offset)
        {
            int timeout = 240;
            while (!nob.Owner.IsActive && timeout > 0)
            {
                yield return null;
                timeout--;
            }
            if (nob.Owner.IsLocalClient || !nob.Owner.IsValid)
                yield break;

            if (timeout < 1 && configuration.logging)
                Debug.LogWarning("Network object owner did not resolve within 240 ticks, initial scene offset may be incorrect.");

            ReceiveOffsetBroadcast responseMsg = new ReceiveOffsetBroadcast
            {
                OffsetX = initial_offset.x,
                OffsetY = initial_offset.y,
                OffsetZ = initial_offset.z,
                ViewNob = nob,
                Tick = 0
            };
            if (configuration.logging)
                Debug.Log($"({InstanceFinder.TimeManager.Tick}) Sending initial offset {initial_offset} to client {nob.Owner}");

            nob.Owner.Broadcast(responseMsg);

            OnInitialOffset?.Invoke();
        }

        public override void TeleportTo(OffsetView view, Vector3d position) => server.TeleportTo(view, position);
        public override Vector3d GetLocalOffset(Scene scene) => server == null ? localOffsetState.current_offset : offsetSceneState.GetOffset(scene);
        public override void RegisterOffsettable(IOffsettable<Scene> offsettable, Scene scene) => offsettableRegistry.RegisterOffsettable(offsettable, scene);
        public override void UnregisterOffsettable(IOffsettable<Scene> offsettable, Scene scene) => offsettableRegistry.UnregisterOffsettable(offsettable, scene);
        public override void RegisterView(OffsetView offsetView)
        {
            bool isOwner = offsetView.TryGetComponent(out NetworkObject nob) && nob.ClientManager.Connection != null ? nob.OwnerId == nob.ClientManager.Connection.ClientId : false;

            if (manager.IsLogging())
                Debug.Log($"{offsetView.gameObject.name} is owner: {isOwner} spawned: {nob.IsSpawned} owner: {nob.IsOwner} ownerID: {nob.OwnerId}");

            offsetViewRegistry.RegisterView(offsetView, SetupViewBeforeRegister, isOwner);
        }
        public override void UnregisterView(OffsetView offsetView) => offsetViewRegistry.UnregisterView(offsetView);
        public override bool HasScene(Scene scene) => offsetSceneState != null ? offsetSceneState.HasScene(scene) : localOffsetState.GetMainSceneKey() == scene;
        public override bool IsServerActive() => this.server != null;

        public override bool IsLogging() => configuration.logging;
    }
}