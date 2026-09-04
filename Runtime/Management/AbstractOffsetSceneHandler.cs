using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    public class AbstractOffsetSceneHandler : OffsetBehaviour
    {
        public Offsetter offsetter;
        protected OffsetStateManager state;
        protected readonly LoadSceneParameters parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
        // Initialize references to the Offsetter and the OffsetStateManager
        private void Start()
        {
            if (offsetter == null)
            {
                offsetter = gameObject.GetComponent<Offsetter>();
            }
            if (state == null)
            {
                state = gameObject.GetComponent<OffsetStateManager>();
            }
        }
        /// <summary>
        /// Process physics in stacked scenes
        /// </summary>
        /// <param name="delta"></param>
        virtual public void PhysicsProcess(float delta)
        {
            foreach (var scene in state.scenes)
            {
                if (scene.IsValid() && scene != state.firstScene)
                    scene.GetPhysicsScene().Simulate(delta);
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
                if (readyActions.Count > 0)
                    readyActions.Dequeue()(scene);
            }
        }
        public void QueueSceneLoadCallback(Action<Scene> onSceneReady) => readyActions.Enqueue(onSceneReady);
    }
}
