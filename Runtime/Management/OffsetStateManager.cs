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
        protected Dictionary<Scene, List<IOffsettable<Scene>>> offsettables = new Dictionary<Scene, List<IOffsettable<Scene>>>();
        private int offsettable_count = 0;
        protected IOffsetObject<Scene> mainView = null;
        private Scene first_scene;
        internal Scene firstScene => first_scene;

        private void Start()
        {
            if (manager == null)
            {
                manager = gameObject.GetComponent<AbstractOffsetManager>();
            }
        }

        internal IEnumerable<Scene> scenes => current_offsets.Keys.AsEnumerable();
        internal void UnregisterView(OffsetView view)
        {
            if (universe.ServerActive)
                universe.server.UnregisterView(view);
        }
        public void RegisterOffsettable(IOffsettable<Scene> offsettable, Scene scene)
        {
            if (!offsettables.ContainsKey(scene))
                offsettables.Add(scene, new List<IOffsettable<Scene>> { offsettable });
            else
                offsettables[scene].Add(offsettable);

            offsettable_count++;
        }

        public void UnregisterOffsettable(IOffsettable<Scene> offsettable, Scene scene)
        {
            if (offsettables.ContainsKey(scene))
            {
                offsettables[scene].Remove(offsettable);
                offsettable_count--;
            }
            else
            {
                throw new Exception("Offsettable not found in expected scene. Offsettables cannot be moved between scenes.");
            }
        }
        public int OffsettableCount() => offsettable_count;
        public void AddOffset(Scene scene)
        {
            first_scene = scene;
            current_offsets.Add(scene, Vector3d.zero);
        }
        public virtual Vector3d GetOffset(Scene scene) => current_offsets.ContainsKey(scene) ? current_offsets[scene] : Vector3d.zero;
        public void SetOffset(Scene key, Vector3d offset)
        {
            current_offsets[key] = offset;
        }
        public virtual bool HasScene(Scene scene) => current_offsets.ContainsKey(scene);
        internal void RegisterView(OffsetView view)
        {
            if (universe.logging)
                Debug.Log($"Registered View {view.name}");
            if (universe.ServerActive)
            {
                universe.server.RegisterView(view);
                if (mainView == null)
                    mainView = view;
            }

            manager.OnViewRegistered(view);
        }

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

        public bool TryGetOffsettable(Scene key, out List<IOffsettable<Scene>> list) => offsettables.TryGetValue(key, out list);

        public Scene GetMainSceneKey() => mainView.GetSceneKey();

        public bool IsMainView(IOffsetObject<Scene> offsetObject) => offsetObject == mainView;

        public int CountOffsettables() => offsettable_count;
    }
}
