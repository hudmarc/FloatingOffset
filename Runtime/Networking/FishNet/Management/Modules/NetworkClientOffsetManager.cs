using FishNet;
using FloatingOffset.Runtime.Example.Types;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    public class NetworkClientOffsetManager
    {
        private Offsetter offsetter;
        private bool logging;
        private LocalOffsetState localOffsetState;
        private OffsettableRegistry offsettableRegistry;

        public NetworkClientOffsetManager(bool logging, LocalOffsetState localOffsetState, OffsettableRegistry offsettableRegistry, Offsetter offsetter)
        {
            this.logging = logging;
            this.localOffsetState = localOffsetState;
            this.offsettableRegistry = offsettableRegistry;
            this.offsetter = offsetter;
            if (logging)
                UnityEngine.Debug.Log("(Client) Instantiated NetworkClientOffsetManager");
        }

        public void OffsetClient(ReceiveOffsetBroadcast msg)
        {
            var new_offset = new Vector3d(msg.OffsetX, msg.OffsetY, msg.OffsetZ);
            if (logging)
                Debug.Log($"({InstanceFinder.TimeManager.Tick}) OFFSET CLIENT: [Local Scene]\n{localOffsetState.current_offset}->{new_offset} ]");
            if (offsettableRegistry.GetOffsettablesInScene(msg.ViewNob.gameObject.scene, out var offsettablesInScene))
            {
                offsetter.Offset(localOffsetState.current_offset, new_offset, msg.ViewNob.gameObject.scene, offsettablesInScene);
            }
            else
            {
                offsetter.Offset(localOffsetState.current_offset, new_offset, msg.ViewNob.gameObject.scene);
            }

            localOffsetState.current_offset = new_offset;
        }
    }
}
