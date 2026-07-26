using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using System;
using FloatingOffset.Runtime.Types;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The offset manager bootstraps the OffsetServer. Disable it on network clients.
    /// </summary>
    [RequireComponent(typeof(Offsetter))]
    public class OffsetManager : OffsetBehaviour, IOffsetHandler<Scene>
    {
        private readonly LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        [SerializeField]
        protected Offsetter offsetter;
        protected Dictionary<Scene, Vector3d> current_offsets = new Dictionary<Scene, Vector3d>();
        private Dictionary<Scene, List<IOffsettable<Scene>>> offsettables = new Dictionary<Scene, List<IOffsettable<Scene>>>();
        /// <summary>
        /// Set false to disable physics processing on stacked scenes.
        /// </summary>
        public bool updateScenePhysicsInternally = true;

        private void Start()
        {
            Physics.simulationMode = SimulationMode.Script;
        }

        protected void Awake()
        {
            if (!enabled)
                return;

            universe.InitializeHandler(this);
            universe.InitializeServer();
        }
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
#endif
        private void InitializeOffsetter()
        {
            // Only search for the asset if the field is currently empty
            if (offsetter == null)
            {
                offsetter = GetComponent<Offsetter>();
            }
        }
        void LateUpdate()
        {
            if (universe.ServerActive)
                universe.Process();
        }
        protected void FixedUpdate()
        {
            if (!updateScenePhysicsInternally)
                return;

            PhysicsProcess(Time.fixedDeltaTime);
        }
        public void PhysicsProcess(float delta)
        {
            foreach (var scene in current_offsets.Keys)
            {
                scene.GetPhysicsScene().Simulate(delta);
            }
        }
        private Scene last_scene = default;
        /// <summary>
        /// Clone the given scene and clears it of OffsetTransforms. Calls the callback when done.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="onSceneReady"></param>
        public void Clone(Scene scene, Action<Scene> onSceneReady)
        {
            float start_time = Time.time;
            if (last_scene == scene)
            {
                if (universe.logging)
                    Debug.LogWarning($"Prevented double execution of completed callback by SceneManager LoadSceneAsync on scene {scene.handle.ToHex()}");
                return;
            }
            last_scene = scene;
            // this is called twice if the editor is unfocused. seems to be a Unity bug.
            SceneManager.LoadSceneAsync(scene.buildIndex, parameters).completed += (arg) => SetupScene(onSceneReady, start_time);
        }
        // Runs some setup code on the scene and calls the callback.
        private void SetupScene(Action<Scene> onSceneReady, float start_time)
        {
            //fixes a bizarre Unity bug where the "completed" callback from LoadSceneAsync gets called twice under certain circumstances.
            // offsetGroups.ContainsKey(SceneManager.GetSceneAt(SceneManager.sceneCount - 1)) is causing scenes to NEVER be registered!
            if (universe.logging)
                Debug.Log($"setting up scene {SceneManager.GetSceneAt(SceneManager.sceneCount - 1).handle.ToHex()}");

            Scene scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

            SetSceneVisibility(scene, false);

            CullOffsetTransforms(scene);

            // important order of operations: do NOT invoke this before you cull the scene!
            onSceneReady?.Invoke(scene);
        }
        // culls scened OffsetTransforms from any scenes that are duplicates of an existing scene.
        private void CullOffsetTransforms(Scene scene)
        {
            if (universe.logging)
                Debug.Log($"Culling objects from scene {scene.handle.ToHex()}");
            var objects = scene.GetRootGameObjects();

            foreach (GameObject g in objects)
            {
                OffsetTransform obj = g.GetComponent<OffsetTransform>();

                if (obj != null)
                {
                    obj.gameObject.SetActive(false);
                    Destroy(obj.gameObject);
                }
            }
        }
        /// <summary>
        /// Transfer the given offsettable to the given offset scene. Removes it from the offset scene this was called on.<br>
        /// Offsets the transform so that it matches the offset of the target scene.
        /// </summary>
        /// <param name="offsetObject"></param>
        /// <param name="scene"></param>
        public virtual void TransferTo(IOffsetObject<Scene> offsetObject, Scene from, Scene to, bool reposition = false)
        {
            Vector3d absoluteRealPos = current_offsets[from] + offsetObject.GetEnginePosition();

            offsetObject.SetSceneKey(to);

            // Calculate the exact local Unity position required for the new scene
            // Because Real = Unity + Offset, therefore Unity = Real - Offset
            if (reposition)
            {
                Vector3d newUnityPos = absoluteRealPos - current_offsets[to];

                offsetObject.SetEnginePosition(newUnityPos);
            }
            Scene main_scene = universe.mainView.GetSceneKey();

            if (offsetObject == universe.mainView)
            {
                SetSceneVisibility(from, false);
                SetSceneVisibility(to, true);
            }
            else
            {
                SetSceneVisibility(from, from == main_scene);
                SetSceneVisibility(to, to == main_scene);
            }



            if (universe.logging)
                Debug.Log($"Transferred {((MonoBehaviour)offsetObject).name} from {from.handle.ToHex()} to {to.handle.ToHex()}");
        }
        /// <summary>
        /// Updates the offset for the given scene.
        /// </summary>
        /// <param name="scene"></param>
        public virtual void UpdateOffset(OffsetScene<Scene> scene)
        {
            var key = scene.key;
            if (!current_offsets.ContainsKey(key))
            {
                current_offsets.Add(key, Vector3d.zero);
            }
            else if (scene.offset == current_offsets[scene.key])
                return;
            if (universe.logging)
                Debug.Log($"OFFSET: [{scene.key.handle.ToHex()}]\n{current_offsets[key]:#.#}->{scene.offset:#.#} ");
            Vector3d old_offset = current_offsets[key];
            current_offsets[key] = scene.offset;

            if (offsettables.TryGetValue(scene.key, out List<IOffsettable<Scene>> list))
            {
                offsetter.Offset(old_offset, current_offsets[key], scene.key, list.ToArray());
            }
            else
            {
                offsetter.Offset(old_offset, current_offsets[key], scene.key);
            }
        }

        public void RegisterOffsettable(IOffsettable<Scene> offsettable, Scene scene)
        {
            if (!offsettables.ContainsKey(scene))
                offsettables.Add(scene, new List<IOffsettable<Scene>> { offsettable });
            else
                offsettables[scene].Add(offsettable);
        }

        private void SetSceneVisibility(Scene scene, bool visible)
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
        public void Unload(Scene scene)
        {
            SceneManager.UnloadSceneAsync(scene);
        }
        public virtual Vector3d GetOffset(Scene scene)
        {
            return current_offsets[scene];
        }
    }
}
