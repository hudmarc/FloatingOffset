using FishNet;
using FishNet.Component.Transforming;
using FishNet.Managing.Client;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace FloatingOffset.Runtime.Example
{
    public class OffsetPredictedNetworkTransform : MonoBehaviour
    {
        private OffsetView view;
        [SerializeField]
        private NetworkTransform networkTransform;
        private Rigidbody rb;
        private NetworkObject nob;

        private ClientManager clientManager;

        void Start()
        {
            view = GetComponent<OffsetView>();
            networkTransform = GetComponent<NetworkTransform>();
            rb = GetComponent<Rigidbody>();
            view.OnPreSceneTransfer += OnPreTransfer;
            clientManager = InstanceFinder.NetworkManager.ClientManager;
            clientManager.OnClientConnectionState += OnStartClient;
        }
        void OnDestroy()
        {
            view.OnPreSceneTransfer -= OnPreTransfer;
            if (clientManager != null)
                clientManager.OnClientConnectionState -= OnStartClient;
        }

        void OnStartClient(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started && !InstanceFinder.IsServerStarted)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
        }

        public void OnPreTransfer()
        {
            networkTransform.Teleport();
            networkTransform.ClearReplicateCache();
        }
    }
}
