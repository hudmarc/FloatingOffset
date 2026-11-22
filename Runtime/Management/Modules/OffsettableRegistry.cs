using System;
using System.Collections.Generic;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    public class OffsettableRegistry
    {
        private Dictionary<object, ISceneKeyedObject<Scene>> offsettables = new Dictionary<object, ISceneKeyedObject<Scene>>();
        private Dictionary<object, IPreSceneOffsetListener<Scene>> pre_listeners = new Dictionary<object, IPreSceneOffsetListener<Scene>>();
        private Dictionary<object, ISceneOffsetListener<Scene>> listeners = new Dictionary<object, ISceneOffsetListener<Scene>>();
        private Dictionary<object, IPostSceneOffsetListener<Scene>> post_listeners = new Dictionary<object, IPostSceneOffsetListener<Scene>>();

        private HashSet<object> ignore_list = new HashSet<object>();

        public OffsettableRegistry(bool logging)
        {
            if (logging)
                UnityEngine.Debug.Log("(Server/Client) Instantiated OffsettableRegistry");
        }

        public int Count => offsettables.Count;

        public void RegisterOffsettable(ISceneKeyedObject<Scene> keyed_object)
        {
            offsettables.Add(keyed_object.GetObject(), keyed_object);

            // type dance

            var pre_listener = keyed_object as IPreSceneOffsetListener<Scene>;
            if (pre_listener != null)
                pre_listeners.Add(pre_listener.GetObject(), pre_listener);

            var listener = keyed_object as ISceneOffsetListener<Scene>;
            if (listener != null)
                listeners.Add(pre_listener.GetObject(), listener);

            var post_listener = keyed_object as IPostSceneOffsetListener<Scene>;
            if (post_listener != null)
                post_listeners.Add(pre_listener.GetObject(), post_listener);

            if (keyed_object.Ignore())
                ignore_list.Add(keyed_object.GetObject());
        }

        public void UnregisterOffsettable(ISceneKeyedObject<Scene> keyed_object)
        {
            offsettables.Remove(keyed_object.GetObject());
            pre_listeners.Remove(keyed_object.GetObject());
            listeners.Remove(keyed_object.GetObject());
            post_listeners.Remove(keyed_object.GetObject());
            ignore_list.Remove(keyed_object);
        }
        public int OffsettableCount() => offsettables.Count;

        public void DoPreCallbacks(object obj, Vector3d old_offset, Vector3d new_offset, Scene destination)
        {
            if (pre_listeners.TryGetValue(obj, out var listener))
                listener.OnPreOffset(old_offset, new_offset, destination);
        }

        public void DoCallbacks(object obj, Vector3d old_offset, Vector3d new_offset, Scene destination)
        {
            if (listeners.TryGetValue(obj, out var listener))
                listener.OnOffset(old_offset, new_offset, destination);
        }

        public void DoPostCallbacks(object obj, Vector3d old_offset, Vector3d new_offset, Scene destination)
        {
            if (post_listeners.TryGetValue(obj, out var listener))
                listener.OnPostOffset(old_offset, new_offset, destination);
        }

        public bool ShouldIgnore(object obj) => ignore_list.Contains(obj);

        public bool TryGetRegistered(GameObject obj, out ISceneKeyedObject<Scene> keyed_object)
        {
            return offsettables.TryGetValue(obj, out keyed_object);
        }
    }
}
