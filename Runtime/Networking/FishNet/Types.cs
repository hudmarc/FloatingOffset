using System.Collections;
using System.Collections.Generic;
using FishNet.Broadcast;
using FishNet.Object;
using UnityEngine;

namespace FloatingOffset.Runtime.Example
{
    namespace Types
    {
        public struct ReceiveOffsetBroadcast : IBroadcast
        {
            public double OffsetX, OffsetY, OffsetZ;
            public NetworkObject ViewNob;
            public uint Tick;
        }
    }
}
