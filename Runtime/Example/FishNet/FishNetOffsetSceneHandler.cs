using FishNet.Object;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime.Example
{
    public class FishNetOffsetSceneHandler : OffsetSceneHandler
    {
        private Vector3d old_offset = Vector3d.zero;
        public override void UpdateOffset(OffsetScene<Scene> scene)
        {
            var key = scene.key;
            if (!current_offsets.ContainsKey(key))
                current_offsets.Add(key, Vector3d.zero);
            else if (scene.offset == current_offsets[scene.key])
                return;

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
            if (((OffsetView)offsetObject).TryGetComponent(out NetworkObject nob))
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
