using System;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    [Serializable]
    public class OffsetConfiguration
    {
        [SerializeField]
        public int MinimumJoinDistance = 1000;
        [SerializeField]
        public int Hysteresis = 1000;
        [SerializeField]
        public int MaxScenes = 200;
        [SerializeField]
        public bool logging = false;
    }
}
