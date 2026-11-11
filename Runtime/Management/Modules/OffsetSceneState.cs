using System.Collections.Generic;

namespace FloatingOffset.Runtime.Example
{

    /// <summary>
    /// Server-only plain C# class, tracks the true state of all offset scenes in the game engine.
    /// </summary>
    public class EngineUniverseState<T>
    {
        private Dictionary<T, Vector3d> current_offsets = new Dictionary<T, Vector3d>();

        public EngineUniverseState() { }

        public bool HasScene(T scene) => current_offsets.ContainsKey(scene);
        public Dictionary<T, Vector3d>.KeyCollection offsetScenes => current_offsets.Keys;
        public Dictionary<T, Vector3d>.ValueCollection offsets => current_offsets.Values;
        public void SetOffset(T key, Vector3d offset) => current_offsets[key] = offset;
        public bool TryAddOffset(T key)
        {
            if (current_offsets.ContainsKey(key))
                return false;
            else
            {
                current_offsets.Add(key, Vector3d.zero);
                return true;
            }
        }
        public Vector3d GetOffset(T scene) => current_offsets.ContainsKey(scene) ? current_offsets[scene] : Vector3d.zero;
    }
}
