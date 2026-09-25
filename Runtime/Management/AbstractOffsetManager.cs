using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        protected List<IOffsettable<Scene>> offsettables = new List<IOffsettable<Scene>>();


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
        public void RegisterOffsettable(IOffsettable<Scene> offsettable, Scene scene) => offsettables.Add(offsettable);

        public void UnregisterOffsettable(IOffsettable<Scene> offsettable, Scene scene) => offsettables.Remove(offsettable);

        public int OffsettableCount() => offsettables.Count;

        List<IOffsettable<Scene>> temp = new List<IOffsettable<Scene>>();
        public bool GetOffsettablesInScene(Scene scene, out ReadOnlyCollection<IOffsettable<Scene>> found)
        {
            while (offsettables[offsettables.Count - 1] == null)
            {
                offsettables.RemoveAt(offsettables.Count - 1);
            }
            temp.Clear();

            for (int i = 0; i < offsettables.Count; i++)
            {
                var offsettable = offsettables[i];

                if (offsettable is UnityEngine.Object unityObj && unityObj == null)
                {
                    offsettables.RemoveAt(i);
                    continue;
                }

                if (!offsettable.IsValid())
                {
                    offsettables.RemoveAt(i);
                    continue;
                }

                if (offsettable.GetSceneKey() == scene)
                {
                    temp.Add(offsettable);
                }
            }

            found = temp.AsReadOnly();

            if (temp.Count < 1)
                return false;
            return true;
        }
    }
}
