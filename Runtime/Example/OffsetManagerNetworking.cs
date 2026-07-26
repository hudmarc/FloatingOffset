using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using FloatingOffset.Runtime.Types;
using UnityEngine.SceneManagement;
using FishNet.Broadcast;
using FishNet.Connection;
using UnityEngine;

namespace FloatingOffset.Runtime.Example
{
    public struct RequestOffsetBroadcast : IBroadcast { public NetworkObject offset_transform_object; }

    public struct ReceiveOffsetBroadcast : IBroadcast
    {
        public double OffsetX, OffsetY, OffsetZ;
    }
    public class OffsetManagerNetworking : OffsetManager
    {
        private Vector3d old_offset = Vector3d.zero;
        private NetworkManager networkManager;
        private OffsetTransform localView;
        // Start is called before the first frame update
        new void Awake()
        {
            universe.InitializeHandler(this);
            if (TryGetComponent(out networkManager))
            {
                networkManager.TimeManager.SetPhysicsMode(FishNet.Managing.Timing.PhysicsMode.Disabled);
                networkManager.ServerManager.OnServerConnectionState += OnStateChange;
            }
        }

        // Called on server
        private void OnStateChange(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                universe.InitializeServer();
            }
        }

        private void OnEnable()
        {
            if (networkManager != null)
            {
                // Register Server and Client broadcast listeners
                networkManager.ServerManager.RegisterBroadcast<RequestOffsetBroadcast>(OnServerReceivedRequest);
                networkManager.ClientManager.RegisterBroadcast<ReceiveOffsetBroadcast>(OnClientReceivedOffset);

                // Subscribe to the client connection state to replace OnStartClient()
                universe.onTransformRegistered += OnTransformRegistered;
                networkManager.TimeManager.OnTick += Physics;

            }
        }

        private void OnDisable()
        {
            if (networkManager != null)
            {
                // Always unregister to prevent memory leaks!
                networkManager.ServerManager.UnregisterBroadcast<RequestOffsetBroadcast>(OnServerReceivedRequest);
                networkManager.ClientManager.UnregisterBroadcast<ReceiveOffsetBroadcast>(OnClientReceivedOffset);

                universe.onTransformRegistered -= OnTransformRegistered;
                networkManager.TimeManager.OnTick -= Physics;

            }
        }

        void OnTransformRegistered(OffsetTransform transform)
        {
            if (networkManager.IsClientOnlyStarted && !networkManager.IsServerStarted)
            {
                var nob = transform.GetComponent<NetworkObject>();
                if (nob != null && nob.IsOwner)
                {
                    if (localView == null)
                        localView = transform;

                    RequestOffsetBroadcast offset_broadcast = new RequestOffsetBroadcast
                    {
                        offset_transform_object = nob
                    };
                    networkManager.ClientManager.Broadcast(offset_broadcast); //will call OnServerReceivedRequest on the server
                }

            }
        }

        /// <summary>
        /// executes server-side 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="msg"></param>
        /// <param name="channel"></param>
        private void OnServerReceivedRequest(NetworkConnection conn, RequestOffsetBroadcast msg, Channel channel)
        {
            if (conn.IsLocalClient)
                return;
            // Executes server-side. 'conn' is automatically the client who sent it.

            Vector3d initial_offset = universe.GetSceneOffset(msg.offset_transform_object.gameObject.scene);

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
            offsetter.Offset(old_offset, new_offset, localView.gameObject.scene);
            old_offset = new_offset;
        }

        private void Physics()
        {
            PhysicsProcess((float)networkManager.TimeManager.TickDelta);
        }

        public override void UpdateOffset(OffsetScene<Scene> scene)
        {
            if (!networkManager.IsServerStarted)
                return;

            var key = scene.key;
            if (!current_offsets.ContainsKey(key))
                current_offsets.Add(key, Vector3d.zero);
            else if (scene.offset == current_offsets[scene.key])
                return;




            var objects = scene.key.GetRootGameObjects();

            foreach (var obj in objects)
            {
                if (obj.TryGetComponent(out OffsetTransform trf) && obj.TryGetComponent(out NetworkObject nob))
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
            // This runs on the server!
            base.UpdateOffset(scene);
        }
        // Runs on the server
        public override void TransferTo(IOffsetObject<Scene> offsetObject, Scene from, Scene to, bool reposition = false)
        {
            base.TransferTo(offsetObject, from, to, reposition);
            if (((OffsetTransform)offsetObject).TryGetComponent(out NetworkObject nob))
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
        new protected void FixedUpdate()
        {
            //Physics is handled by the NetworkManager's PreTick
        }
        public override Vector3d GetOffset(Scene scene)
        {
            if (universe.ServerActive)
                if (current_offsets.TryGetValue(scene, out Vector3d offset))
                {
                    return offset;
                }
                else return Vector3d.zero;
            return old_offset;
        }
    }
}
