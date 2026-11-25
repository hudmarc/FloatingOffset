using System;
using System.Collections.Generic;
using FloatingOffset.Runtime.Types;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{

    public class OffsettableRegistry
    {
        private struct Listable<T> where T : class
        {
            public T first;
            public List<T> more;

            public Listable(T item)
            {
                first = item;
                more = null;
            }

            public Listable(Listable<T> list, T new_item)
            {
                if (list.first == null)
                {
                    first = new_item;
                    more = null;
                    return;
                }
                first = list.first;
                if (list.more == null)
                {
                    more = new List<T> { new_item };
                }
                else
                {
                    more = list.more;
                    more.Add(new_item);
                }
            }
        }
        private Dictionary<object, Listable<ISceneKeyedObject<Scene>>> offsettables = new Dictionary<object, Listable<ISceneKeyedObject<Scene>>>();

        private Dictionary<object, Listable<IPreSceneOffsetListener<Scene>>> pre_listeners = new Dictionary<object, Listable<IPreSceneOffsetListener<Scene>>>();

        private Dictionary<object, Listable<ISceneOffsetListener<Scene>>> listeners = new Dictionary<object, Listable<ISceneOffsetListener<Scene>>>();

        private Dictionary<object, Listable<IPostSceneOffsetListener<Scene>>> post_listeners = new Dictionary<object, Listable<IPostSceneOffsetListener<Scene>>>();

        private HashSet<object> ignore_list = new HashSet<object>();

        public OffsettableRegistry(bool logging)
        {
            if (logging)
                UnityEngine.Debug.Log("(Server/Client) Instantiated OffsettableRegistry");
        }

        public int Count => offsettables.Count;

        public void RegisterOffsettable(ISceneKeyedObject<Scene> keyed_object, bool ignore = false)
        {
            var key = keyed_object.GetEngineObject();
            if (ignore)
            {
                ignore_list.Add(key);
            }

            offsettables.Add(key, new Listable<ISceneKeyedObject<Scene>>(offsettables.GetValueOrDefault(key), keyed_object));

            // type dance

            var pre_listener = keyed_object as IPreSceneOffsetListener<Scene>;
            if (pre_listener != null)
                Append(pre_listeners, key, pre_listener);

            var listener = keyed_object as ISceneOffsetListener<Scene>;
            if (listener != null)
                Append(listeners, key, listener);

            var post_listener = keyed_object as IPostSceneOffsetListener<Scene>;
            if (post_listener != null)
                Append(post_listeners, key, post_listener);
        }

        private static void Append<T>(Dictionary<object, Listable<T>> dict, object key, T item) where T : class
        {
            dict.TryGetValue(key, out var existing);
            dict[key] = new Listable<T>(existing, item);
        }

        public void UnregisterOffsettable(ISceneKeyedObject<Scene> keyed_object)
        {
            var key = keyed_object.GetEngineObject();

            if (offsettables.TryGetValue(key, out var value) && value.more != null)
                Debug.Log($"Removed multiple listeners from object {(keyed_object.GetEngineObject() as GameObject).name}");

            offsettables.Remove(keyed_object.GetEngineObject());
            pre_listeners.Remove(keyed_object.GetEngineObject());
            listeners.Remove(keyed_object.GetEngineObject());
            post_listeners.Remove(keyed_object.GetEngineObject());
            ignore_list.Remove(keyed_object.GetEngineObject());
        }
        public int OffsettableCount() => offsettables.Count;

        public void DoPreCallbacks(object obj, Vector3d old_offset, Vector3d new_offset, Scene destination)
        {
            if (pre_listeners.TryGetValue(obj, out var listener))
            {
                listener.first.OnPreOffset(old_offset, new_offset, destination);

                foreach (var additional in listener.more)
                    additional.OnPreOffset(old_offset, new_offset, destination);
            }
        }

        public void DoCallbacks(object obj, Vector3d old_offset, Vector3d new_offset, Scene destination)
        {
            if (listeners.TryGetValue(obj, out var listener))
            {
                listener.first.OnOffset(old_offset, new_offset, destination);

                foreach (var additional in listener.more)
                    additional.OnOffset(old_offset, new_offset, destination);
            }
        }

        public void DoPostCallbacks(object obj, Vector3d old_offset, Vector3d new_offset, Scene destination)
        {
            if (post_listeners.TryGetValue(obj, out var listener))
            {
                listener.first.OnPostOffset(old_offset, new_offset, destination);

                foreach (var additional in listener.more)
                    additional.OnPostOffset(old_offset, new_offset, destination);
            }
        }

        public bool ShouldIgnore(object obj) => ignore_list.Contains(obj);

        public bool HasRegistered(GameObject obj)
        {
            return offsettables.ContainsKey(obj);
        }
    }
}
