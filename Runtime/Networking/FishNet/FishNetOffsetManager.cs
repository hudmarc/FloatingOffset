using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Broadcast;
using FishNet.Connection;
using UnityEngine;
using FloatingOffset.Runtime.Types;
using UnityEngine.SceneManagement;
using FishNet;

namespace FloatingOffset.Runtime.Example
{
    public class FishNetOffsetManager : AbstractOffsetManager
    {
        private Vector3d old_offset = Vector3d.zero;
        private NetworkManager networkManager;
        private OffsetView localView;
        // Start is called before the first frame update
        void Awake()
        {
            if (!enabled)
                return;

            if (handler == null)
                handler = gameObject.AddComponent<FishNetOffsetSceneHandler>();

            if (state == null)
                state = gameObject.AddComponent<OffsetStateManager>();

            if (TryGetComponent(out networkManager))
            {
                networkManager.TimeManager.SetPhysicsMode(FishNet.Managing.Timing.PhysicsMode.TimeManager);
                networkManager.ServerManager.OnServerConnectionState += OnServerStateChange;
            }

            universe.RegisterManager(this); //this way clients can still query their real offsets
        }

        private void Physics(float delta) => handler.PhysicsProcess(delta);


        // Called on server
        private void OnServerStateChange(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                Debug.Log("Initialized universe");
                universe.InitializeWithHandler(this, state, handler as IOffsetHandler<Scene>);

                // Register Server and Client broadcast listeners
                networkManager.ServerManager.RegisterBroadcast<RequestOffsetBroadcast>(OnServerReceivedRequest);
                networkManager.ClientManager.RegisterBroadcast<ReceiveOffsetBroadcast>(OnClientReceivedOffset);

                // Subscribe to the client connection state to replace OnStartClient()
                networkManager.TimeManager.OnPreTick += Process;
                networkManager.TimeManager.OnPrePhysicsSimulation += Physics;

                InstanceFinder.SceneManager.OnLoadEnd += OnLoadEnd;
            }
            else if (args.ConnectionState == LocalConnectionState.Stopping)
            {
                // Always unregister to prevent memory leaks!
                networkManager.ServerManager.UnregisterBroadcast<RequestOffsetBroadcast>(OnServerReceivedRequest);
                networkManager.ClientManager.UnregisterBroadcast<ReceiveOffsetBroadcast>(OnClientReceivedOffset);

                networkManager.TimeManager.OnPreTick -= Process;
                networkManager.TimeManager.OnPrePhysicsSimulation -= Physics;

                InstanceFinder.SceneManager.OnLoadEnd -= OnLoadEnd;
            }
        }

        private void OnLoadEnd(FishNet.Managing.Scened.SceneLoadEndEventArgs data)
        {
            Debug.Log($"Scene loaded (FishNet)\n{data.ToString()}");
            handler.OnLoadEnd(data.LoadedScenes);
        }

        override public void OnViewRegistered(OffsetView view)
        {
            if (networkManager.IsClientOnlyStarted && !networkManager.IsServerStarted)
            {
                var nob = transform.GetComponent<NetworkObject>();
                if (nob != null && nob.IsOwner)
                {
                    if (localView == null)
                        localView = view;

                    RequestOffsetBroadcast offset_broadcast = new RequestOffsetBroadcast
                    {
                        offset_transform_object = nob
                    };
                    networkManager.ClientManager.Broadcast(offset_broadcast); //will call OnServerReceivedRequest on the server
                }
            }
        }

        /// <summary>
        /// Executes server-side 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="msg"></param>
        /// <param name="channel"></param>
        private void OnServerReceivedRequest(NetworkConnection conn, RequestOffsetBroadcast msg, Channel channel)
        {
            if (conn.IsLocalClient)
                return;
            // Executes server-side. 'conn' is automatically the client who sent it.

            Vector3d initial_offset = state.GetOffset(msg.offset_transform_object.gameObject.scene);

            // Send the response broadcast back strictly to the connection that asked
            ReceiveOffsetBroadcast responseMsg = new ReceiveOffsetBroadcast
            {
                OffsetX = initial_offset.x,
                OffsetY = initial_offset.y,
                OffsetZ = initial_offset.z,
            };

            conn.Broadcast(responseMsg);
        }

        /// <summary>
        /// This runs only on the client that originally made the request.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="channel"></param>
        private void OnClientReceivedOffset(ReceiveOffsetBroadcast msg, Channel channel)
        {
            if (localView == null)
                return;
            // Executes client-side

            var new_offset = new Vector3d(msg.OffsetX, msg.OffsetY, msg.OffsetZ);
            if (universe.logging)
                Debug.Log($"OFFSET CLIENT: [Local Scene]\n{old_offset}->{new_offset} ]");
            handler.offsetter.Offset(old_offset, new_offset, localView.gameObject.scene);
            old_offset = new_offset;
        }
    }
    public struct RequestOffsetBroadcast : IBroadcast { public NetworkObject offset_transform_object; }

    public struct ReceiveOffsetBroadcast : IBroadcast
    {
        public double OffsetX, OffsetY, OffsetZ;
    }
}
