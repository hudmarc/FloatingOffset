using System;
using System.Collections.Generic;
using FloatingOffset.Runtime.Types;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    [RequireComponent(typeof(Offsetter))]
    public class OffsetSceneHandler : OffsetBehaviour
    {
        [field: SerializeField]
        public Offsetter offsetter { get; private set; }
        protected readonly LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        protected Dictionary<Scene, Vector3d> current_offsets = new Dictionary<Scene, Vector3d>();
        protected Dictionary<Scene, List<IOffsettable<Scene>>> offsettables = new Dictionary<Scene, List<IOffsettable<Scene>>>();
        private int offsettable_count = 0;
        protected IOffsetObject<Scene> mainView = null;
        private Scene first_scene;
#if UNITY_EDITOR
        protected override void Reset()
        {
            // Fires when the component is first added to a GameObject
            base.Reset();
            InitializeOffsetter();
        }

        protected override void OnValidate()
        {
            // Fires when the inspector updates. Acts as a safety net.
            base.OnValidate();
            InitializeOffsetter();
        }
        private void InitializeOffsetter()
        {
            // Only search for the asset if the field is currently empty
            if (offsetter == null)
            {
                offsetter = gameObject.GetComponent<Offsetter>();
            }
        }
#endif

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
        public virtual bool HasScene(Scene scene) => current_offsets.ContainsKey(scene);
        internal void RegisterView(OffsetView offsetTransform)
        {
            if (universe.logging)
                Debug.Log($"Registered View {offsetTransform.name}");
            if (universe.ServerActive)
            {
                universe.server.RegisterView(offsetTransform);
                if (mainView == null)
                    mainView = offsetTransform;
            }
        }

        virtual public void PhysicsProcess(float delta)
        {
            foreach (var scene in current_offsets.Keys)
            {
                if (scene.IsValid() && scene != first_scene)
                    scene.GetPhysicsScene().Simulate(delta);
            }
        }

        // culls scened OffsetTransforms from any scenes that are duplicates of an existing scene.
        protected void CullOffsetTransforms(Scene scene)
        {
            if (universe.logging)
                Debug.Log($"Culling objects from scene {scene.handle.ToHex()}");
            var objects = scene.GetRootGameObjects();

            foreach (GameObject g in objects)
            {
                OffsetView obj = g.GetComponent<OffsetView>();

                if (obj != null)
                {
                    obj.gameObject.SetActive(false);
                    Destroy(obj.gameObject);
                }
            }
        }


        protected void SetSceneVisibility(Scene scene, bool visible)
        {
            if (universe.logging)
                Debug.Log($"Changed visibility on {scene.handle.ToHex()} to {visible}");

            var rootobjectsInScene = scene.GetRootGameObjects();
            for (int i = 0; i < rootobjectsInScene.Length; i++)
            {
                Renderer[] renderers = rootobjectsInScene[i].GetComponentsInChildren<Renderer>();

                for (int j = 0; j < renderers.Length; j++)
                {
                    renderers[j].enabled = visible;
                }

                if (rootobjectsInScene[i].TryGetComponent(out Terrain terrain))
                {
                    terrain.enabled = visible;
                }
            }
        }

        Queue<Action<Scene>> readyActions = new Queue<Action<Scene>>();

        public void OnLoadEnd(Scene[] loadedScenes)
        {
           
            foreach (Scene scene in loadedScenes)
            {
                Debug.Log($"Loaded scene {scene.GetHashCode().ToHex()}");
                readyActions.Dequeue()(scene);
            }
        }
        public void QueueSceneLoadCallback(Action<Scene> onSceneReady) => readyActions.Enqueue(onSceneReady);

        // Runs some setup code on the scene and calls the callback.
        // protected void SetupScene(Action<Scene> onSceneReady, float start_time)
        // {
        //     //fixes a bizarre Unity bug where the "completed" callback from LoadSceneAsync gets called twice under certain circumstances.
        //     // offsetGroups.ContainsKey(SceneManager.GetSceneAt(SceneManager.sceneCount - 1)) is causing scenes to NEVER be registered!
        //     if (universe.logging)
        //         Debug.Log($"setting up scene {SceneManager.GetSceneAt(SceneManager.sceneCount - 1).handle.ToHex()}");

        //     Scene scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

        //     SetSceneVisibility(scene, false);

        //     CullOffsetTransforms(scene);

        //     // important order of operations: do NOT invoke this before you cull the scene!
        //     onSceneReady?.Invoke(scene);
        // }
    }
}
