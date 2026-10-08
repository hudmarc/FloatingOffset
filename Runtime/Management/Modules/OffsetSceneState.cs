using System.Collections.Generic;
using FloatingOffset.Runtime.Types;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime.Example
{

    /// <summary>
    /// Server-only plain C# class, tracks the true state of all offset scenes in the game engine.
    /// </summary>
    public class OffsetSceneState
    {
        private Dictionary<Scene, Vector3d> current_offsets = new Dictionary<Scene, Vector3d>();

        public OffsetSceneState()
        {
            UnityEngine.Debug.Log("(Server) Instantiated OffsetSceneState");
        }

        public bool HasScene(Scene scene) => current_offsets.ContainsKey(scene);
        public Dictionary<Scene, Vector3d>.KeyCollection offsetScenes => current_offsets.Keys;
        public Dictionary<Scene, Vector3d>.ValueCollection offsets => current_offsets.Values;
        public void SetOffset(Scene key, Vector3d offset) => current_offsets[key] = offset;
        public bool TryAddOffset(Scene key)
        {
            if (current_offsets.ContainsKey(key))
                return false;
            else
            {
                current_offsets.Add(key, Vector3d.zero);
                return true;
            }
        }
        public Vector3d GetOffset(Scene scene) => current_offsets.ContainsKey(scene) ? current_offsets[scene] : Vector3d.zero;
    }
}
