using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Broadcast;
using UnityEngine;
using FloatingOffset.Runtime.Types;
using UnityEngine.SceneManagement;
using FishNet;
using System.Collections.Generic;
using FishNet.Managing.Timing;
using System.Collections;

namespace FloatingOffset.Runtime.Example
{
    public class FishNetOffsetManager : AbstractOffsetManager
    {
        private Vector3d current_offset = Vector3d.zero;
        private NetworkManager networkManager;
        public Offsetter offsetter;

        // Start is called before the first frame update
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

            universe.RegisterManager(this); //this way clients can still query their real offsets
        }

        void OnDestroy()
        {
            networkManager.ServerManager.OnServerConnectionState -= OnServerStateChange;
            networkManager.ClientManager.OnClientConnectionState -= OnClientStateChange;
        }

        private void Physics(float delta) => handler.PhysicsProcess(delta);


        // Called on server
        private void OnServerStateChange(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                if (state == null)
                    state = gameObject.AddComponent<OffsetStateManager>();

                if (handler == null)
                    handler = gameObject.AddComponent<FishNetOffsetSceneHandler>();

                universe.InitializeWithHandler(this, state, handler as IOffsetHandler<Scene>);

                Debug.Log("Initialized universe");

                networkManager.TimeManager.OnPreTick += Process;
                networkManager.TimeManager.OnPrePhysicsSimulation += Physics;

                InstanceFinder.SceneManager.OnLoadEnd += OnLoadEnd;
            }
            else if (args.ConnectionState == LocalConnectionState.Stopping)
            {
                networkManager.TimeManager.OnPreTick -= Process;
                networkManager.TimeManager.OnPrePhysicsSimulation -= Physics;

                InstanceFinder.SceneManager.OnLoadEnd -= OnLoadEnd;
            }
        }

        private void OnClientStateChange(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started && !universe.ServerActive)
            {
                networkManager.ClientManager.RegisterBroadcast<ReceiveOffsetBroadcast>(OnClientReceivedOffset);
            }
            else if (args.ConnectionState == LocalConnectionState.Stopping && !universe.ServerActive)
            {
                networkManager.ClientManager.UnregisterBroadcast<ReceiveOffsetBroadcast>(OnClientReceivedOffset);
            }
        }

        private void OnLoadEnd(FishNet.Managing.Scened.SceneLoadEndEventArgs data)
        {
            foreach (var scene in data.LoadedScenes)
            {
                Debug.Log($"Scene loaded (FishNet) {scene.handle.GetHashCode()}");
            }

            handler.OnLoadEnd(data.LoadedScenes);
        }

        override public void SetupViewBeforeRegister(OffsetView view)
        {
            if (!networkManager.IsServerStarted)
                throw new System.Exception($"Attempted to register view on {view.gameObject.name} before NetworkServer was ready.");

            var nob = view.GetComponent<NetworkObject>();

            if (nob != null && !nob.IsOwner)
            {
                Vector3d initial_offset = state.GetOffset(view.gameObject.scene);

                StartCoroutine(SendInitialOffsetWhenReady(nob, initial_offset));
            }
        }

        private IEnumerator SendInitialOffsetWhenReady(NetworkObject nob, Vector3d initial_offset)
        {
            while (!nob.Owner.IsActive)
            {
                yield return null;
            }
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
        }

        private void OnClientReceivedOffset(ReceiveOffsetBroadcast msg, Channel channel)
        {
            StartCoroutine(OnClientReceivedOffsetRoutine(msg, channel));
        }

        private IEnumerator OnClientReceivedOffsetRoutine(ReceiveOffsetBroadcast msg, Channel channel)
        {
            while (msg.Tick > InstanceFinder.TimeManager.LocalTick)
            {
                yield return null;
            }
            var new_offset = new Vector3d(msg.OffsetX, msg.OffsetY, msg.OffsetZ);
            if (universe.logging)
                Debug.Log($"({InstanceFinder.TimeManager.Tick}) OFFSET CLIENT: [Local Scene]\n{current_offset}->{new_offset} ]");
            if (universe.manager.GetOffsettablesInScene(msg.ViewNob.gameObject.scene, out var offsettables))
            {
                offsetter.Offset(current_offset, new_offset, msg.ViewNob.gameObject.scene, offsettables);
            }
            else
            {
                offsetter.Offset(current_offset, new_offset, msg.ViewNob.gameObject.scene);
            }

            current_offset = new_offset;
        }

        public override Vector3d GetLocalOffset(IOffsetObject<Scene> view)
        {
            return state == null ? current_offset : state.GetOffset(view.GetSceneKey());
        }
    }
    public struct ReceiveOffsetBroadcast : IBroadcast
    {
        public double OffsetX, OffsetY, OffsetZ;
        public NetworkObject ViewNob;
        public uint Tick;
    }
}
