using System;
using System.Collections.Generic;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// The offset manager bootstraps the OffsetServer. Disable it on network clients.
    /// </summary>
    public abstract class AbstractOffsetManager : OffsetBehaviour
    {
        [SerializeField]
        protected AbstractOffsetSceneHandler handler;
        [SerializeField]
        protected OffsetStateManager state;

        private int offsettable_count = 0;


        protected Dictionary<Scene, List<IOffsettable<Scene>>> offsettables = new Dictionary<Scene, List<IOffsettable<Scene>>>();


        /// <summary>
        /// Set false to disable physics processing on stacked scenes.
        /// </summary>
        public bool updateScenePhysicsInternally = true;
        /// <summary>
        /// Runs the Process loop on the OffsetUniverse.
        /// </summary>
        protected void Process() => universe.server.Process();

        /// <summary>
        /// Called immediately before RegisterView is called.
        /// </summary>
        /// <param name="view"></param>
        public virtual void SetupViewBeforeRegister(OffsetView view)
        {
            // this space left intentionally blank
        }

        /// <summary>
        /// Teleport the given OffsetView view to the given position in space.
        /// </summary>
        /// <param name="view">The offset transform to teleport.</param>
        /// <param name="position">The destination where this offset transform will be teleported.</param>
        public void TeleportTo(OffsetView view, Vector3d position)
        {
            if (universe.ServerActive)
            {
                universe.server.TeleportTo(view, position);
                if (universe.logging)
                    Debug.Log($"Teleported {view.name} to {position}");
            }
        }
        public int CountRegisteredViews() => universe.server.RegisteredViewCount();

        public int CountViews() => universe.server.ActualViewCount();
        /// <summary>
        /// The local offset of the given view.
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public abstract Vector3d GetLocalOffset(IOffsetObject<Scene> view);
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

        public bool TryGetOffsettable(Scene key, out List<IOffsettable<Scene>> list) => offsettables.TryGetValue(key, out list);

        public int CountOffsettables() => offsettable_count;

    }
}
