using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FloatingOffset.Runtime.Types;
using System.Linq;
using System;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// Shared (FishNet and Unity) state management for the Unity side of the Offset Scene management.
    /// </summary>
    public class OffsetStateManager : OffsetBehaviour
    {
        private AbstractOffsetManager manager;
        protected Dictionary<Scene, Vector3d> current_offsets = new Dictionary<Scene, Vector3d>();
        protected IOffsetObject<Scene> mainView = null;
        internal IEnumerable<Scene> scenes => current_offsets.Keys.AsEnumerable();

        private void Start()
        {
            if (manager == null)
            {
                manager = gameObject.GetComponent<AbstractOffsetManager>();
            }
        }

        internal void RegisterView(OffsetView view)
        {
            if (universe.logging)
                Debug.Log($"Registered View {view.name}");

            manager.SetupViewBeforeRegister(view);

            if (universe.ServerActive)
            {
                universe.server.RegisterView(view);
                if (mainView == null)
                    mainView = view;
            }
        }
        internal void UnregisterView(OffsetView view)
        {
            if (universe.ServerActive)
                universe.server.UnregisterView(view);
        }
        public void AddOffset(Scene scene)
        {
            current_offsets.Add(scene, Vector3d.zero);
        }
        public virtual Vector3d GetOffset(Scene scene) => current_offsets.ContainsKey(scene) ? current_offsets[scene] : Vector3d.zero;
        public void SetOffset(Scene key, Vector3d offset)
        {
            current_offsets[key] = offset;
        }
        public virtual bool HasScene(Scene scene) => current_offsets.ContainsKey(scene);
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
        public Scene GetMainSceneKey() => mainView.GetSceneKey();

        public bool IsMainView(IOffsetObject<Scene> offsetObject) => offsetObject == mainView;
    }
}
