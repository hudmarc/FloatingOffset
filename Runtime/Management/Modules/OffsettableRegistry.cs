using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    public class OffsettableRegistry
    {
        private List<IOffsettable<Scene>> temp = new List<IOffsettable<Scene>>();
        protected List<IOffsettable<Scene>> offsettables = new List<IOffsettable<Scene>>();

        public OffsettableRegistry(bool logging)
        {
            if (logging)
                UnityEngine.Debug.Log("(Server/Client) Instantiated OffsettableRegistry");
        }

        public int Count => offsettables.Count;

        public void RegisterOffsettable(IOffsettable<Scene> offsettable, Scene scene) => offsettables.Add(offsettable);

        public void UnregisterOffsettable(IOffsettable<Scene> offsettable, Scene scene) => offsettables.Remove(offsettable);

        public int OffsettableCount() => offsettables.Count;

        public bool GetOffsettablesInScene(Scene scene, out ReadOnlyCollection<IOffsettable<Scene>> found)
        {
            while (offsettables.Count > 0 && offsettables[offsettables.Count - 1] == null)
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
