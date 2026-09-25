using System;
using System.Collections.Generic;
using FloatingOffset.Runtime.Types;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// State management for scenes. Wraps raw array modification calls.
    /// </summary>
    /// <typeparam name="TSceneKey"></typeparam>
    public class OffsetSceneCollection<TSceneKey>
    {
        /// <summary>
        /// Maps a scene key to its underlying index in the scenes array.
        /// </summary>
        private Dictionary<TSceneKey, int> scene_indexes = new Dictionary<TSceneKey, int>();

        /// <summary>
        /// Contiguous array of scene data for fast iteration.
        /// </summary>
        private OffsetScene<TSceneKey>[] scenes;

        public OffsetSceneCollection()
        {
            scenes = new OffsetScene<TSceneKey>[1];
        }

        private int activeCount = 0; // Tracks the boundary of populated scenes
        private int aliveCount = 0;  // Tracks the boundary of registered scenes

        private void Swap(int indexA, int indexB)
        {
            if (indexA == indexB) return;

            // Swap the structs in memory
            var temp = scenes[indexA];
            scenes[indexA] = scenes[indexB];
            scenes[indexB] = temp;

            // Sync the dictionary so lookups still point to the correct indices
            scene_indexes[scenes[indexA].key] = indexA;
            scene_indexes[scenes[indexB].key] = indexB;
        }

        /// <summary>
        /// Gets the current mathematical offset for the scene at the specified array index.
        /// </summary>
        /// <param name="scene_index">The array index of the scene.</param>
        /// <returns>The 64-bit vector representing the scene's floating origin offset.</returns>
        public Vector3d GetOffsetAt(int scene_index)
        {
            return scenes[scene_index].offset;
        }

        /// <summary>
        /// Gets the current mathematical offset for the specified scene key.
        /// </summary>
        /// <param name="scene">The unique key identifying the scene.</param>
        /// <returns>The 64-bit vector representing the scene's floating origin offset.</returns>
        public Vector3d GetOffset(TSceneKey scene)
        {
            return GetOffsetAt(scene_indexes[scene]);
        }

        /// <summary>
        /// Gets the total number of registered views (e.g., players, cameras) currently in the scene at the specified array index.
        /// </summary>
        /// <param name="scene_index">The array index of the scene.</param>
        /// <returns>The number of active views.</returns>
        public int GetViewCountAt(int scene_index)
        {
            if (scene_index >= scenes.Length)
            {
                return 0;
            }
            return scenes[scene_index].view_count;
        }

        /// <summary>
        /// Gets the total number of registered views (e.g., players, cameras) currently in the specified scene.
        /// </summary>
        /// <param name="scene">The unique key identifying the scene.</param>
        /// <returns>The number of active views.</returns>
        public int GetViewCount(TSceneKey scene)
        {
            return GetViewCountAt(scene_indexes[scene]);
        }

        /// <summary>
        /// Is this a valid scene? i.e. is it associated with an active world scene.
        /// </summary>
        /// <param name="scene_index"></param>
        /// <returns></returns>
        public bool IsValid(int scene_index)
        {
            if (scene_index >= scenes.Length)
                return false;
            return scenes[scene_index].valid;
        }
        /// <summary>
        /// Is this a valid scene? i.e. is it associated with an active world scene.
        /// </summary>
        /// <param name="scene_index"></param>
        /// <returns></returns>
        public bool IsValid(TSceneKey scene_index)
        {
            if (!scene_indexes.ContainsKey(scene_index))
                return false;
            return IsValid(scene_indexes[scene_index]);
        }

        /// <summary>
        /// Decrements the view count for the scene at the specified array index.
        /// </summary>
        /// <param name="scene_index">The array index of the scene.</param>
        public void RemoveViewAt(int scene_index)
        {
            scenes[scene_index].view_count--;

            if (scenes[scene_index].view_count < 1)
            {
                SetEmpty(scene_index);
            }
        }

        /// <summary>
        /// Removes a tracked view from its associated scene, decrementing that scene's view count.
        /// </summary>
        /// <param name="view">The offset object being removed.</param>
        public void RemoveView(TSceneKey scene)
        {
            if (scene_indexes.ContainsKey(scene))
            {
                RemoveViewAt(scene_indexes[scene]);
            }
        }

        /// <summary>
        /// Increments the view count for the scene at the specified array index.
        /// </summary>
        /// <param name="scene_index">The array index of the scene.</param>
        private void AddViewAt(int scene_index)
        {
            scenes[scene_index].view_count++;

            // If this is the first view, upgrade the scene from the Empty zone to the Active zone
            if (scenes[scene_index].view_count == 1 && scene_index >= activeCount)
            {
                Swap(scene_index, activeCount);
                activeCount++;
            }
        }
        /// <summary>
        /// Registers a tracked view to its associated scene, incrementing that scene's view count.
        /// </summary>
        /// <param name="view">The offset object being added.</param>
        public void AddView(TSceneKey scene)
        {
            AddViewAt(scene_indexes[scene]);
        }
        /// <summary>
        /// Gets the scene key from the given public scene index.
        /// </summary>
        /// <param name="scene_index"></param>
        /// <returns></returns>
        public TSceneKey GetSceneKeyAt(int scene_index)
        {
            return scenes[scene_index].key;
        }
        /// <summary>
        /// Register a Scene with this offset scene collection.
        /// </summary>
        /// <param name="sceneKey"></param>
        public void Register(TSceneKey sceneKey)
        {
            // If our alive boundary hits the end of the array, we are out of space.
            if (aliveCount >= scenes.Length)
            {
                Array.Resize(ref scenes, scenes.Length * 2);
            }

            int newIndex = aliveCount;

            OffsetScene<TSceneKey> scene = new OffsetScene<TSceneKey>();
            scene.valid = true;
            scene.key = sceneKey;
            scene.view_count = 0; // Starts in the 'Empty' zone

            scenes[newIndex] = scene;
            scene_indexes[sceneKey] = newIndex;

            aliveCount++;
        }
        /// <summary>
        /// Unregister a scene from this offset scene collection
        /// </summary>
        /// <param name="scene"></param>
        public void Unregister(TSceneKey scene)
        {
            if (!scene_indexes.TryGetValue(scene, out int index)) return;
            UnregisterAt(index);
        }
        /// <summary>
        /// Unregister a scene from this offset scene collection
        /// </summary>
        /// <param name="index"></param>
        public void UnregisterAt(int index)
        {
            // 1. If it is in the Active zone, downgrade it to the Empty zone
            if (index < activeCount)
            {
                activeCount--;
                Swap(index, activeCount);
                index = activeCount; // Update our local index reference
            }

            // 2. Now that it is in the Empty zone, downgrade it to the Dead zone
            aliveCount--;
            Swap(index, aliveCount);

            // 3. Clear the data and erase from dictionary
            scenes[aliveCount].valid = false;
            scene_indexes.Remove(scenes[index].key);
        }
        /// <summary>
        /// Does this scene collection have a scene matching this key?
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public bool HasScene(TSceneKey scene)
        {
            return scene_indexes.ContainsKey(scene);
        }
        /// <summary>
        /// Sets the offset of the given OffsetGroup. Remember to call handler.UpdateOffset after!
        /// </summary>
        /// <param name="index"></param>
        /// <param name="offset"></param>
        public OffsetScene<TSceneKey> OffsetAt(int index, Vector3d offset)
        {
            scenes[index].offset = offset;
            return scenes[index];
        }
        /// <summary>
        /// Sets the offset of the given OffsetGroup. Remember to call handler.UpdateOffset after!
        /// </summary>
        /// <param name="key"></param>
        /// <param name="offset"></param>
        public OffsetScene<TSceneKey> Offset(TSceneKey key, Vector3d offset)
        {
            return OffsetAt(scene_indexes[key], offset);
        }
        /// <summary>
        /// Gets the Offset Scene at the given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public OffsetScene<TSceneKey> GetSceneAt(int index)
        {
            return scenes[index];
        }
        /// <summary>
        /// Gets the Offset Scene with the given key.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>ƒ
        public OffsetScene<TSceneKey> GetScene(TSceneKey key)
        {
            return scenes[scene_indexes[key]];
        }
        /// <summary>
        /// Empties the given scene.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public void SetEmpty(int scene_index)
        {
            scenes[scene_index].view_count = 0;

            // If the scene is currently inside the Active zone, move it to the Empty zone
            if (scene_index < activeCount)
            {
                activeCount--;
                Swap(scene_index, activeCount);
            }
        }

        /// <summary>
        /// Attempts to recycle a scene from the Empty Zone. 
        /// Returns true and outputs the index if successful.
        /// </summary>
        public bool TryPopEmpty(out int recycledIndex)
        {
            // The Empty Zone exists strictly between the Active boundary and the Alive boundary.
            // If activeCount == aliveCount, the Empty Zone is completely empty.
            if (activeCount < aliveCount)
            {
                // 1. Grab the scene sitting exactly on the active boundary
                recycledIndex = activeCount;

                // 2. Expand the Active zone to officially pull this scene inside it
                activeCount++;
                return true;
            }

            // No empty scenes exist. The server must create a new one.
            recycledIndex = -1;
            return false;
        }
        /// <summary>
        /// Adds views to the given scene.
        /// </summary>
        /// <param name="scene_index">The public index of the scene.</param>
        /// <param name="count">The views to add.</param>
        public void AddViewsAt(int scene_index, int count)
        {
            bool wasEmpty = scenes[scene_index].view_count == 0;
            scenes[scene_index].view_count += count;

            // Upgrade the scene if it just gained its first views
            if (wasEmpty && scenes[scene_index].view_count > 0 && scene_index >= activeCount)
            {
                Swap(scene_index, activeCount);
                activeCount++;
            }
        }
        /// <summary>
        /// Are the two scenes on the same layer?
        /// </summary>
        /// <param name="i">Scene 1</param>
        /// <param name="j">Scene 2</param>
        /// <returns></returns>
        public bool SameLayer(int i, int j) => scenes[i].layer == scenes[j].layer;

        public int IndexOf(TSceneKey sceneKey)
        {
            return scene_indexes[sceneKey];
        }

        /// <summary>
        /// The current total capacity of the underlying scenes array.
        /// </summary>
        public int Capacity { get => scenes.Length; }
        /// <summary>
        /// Count of active scenes on the scenes array.
        /// </summary>
        public int Count { get => activeCount; }
        /// <summary>
        /// Count of empty scenes on the scenes array.
        /// </summary>
        public int EmptyCount { get => aliveCount - activeCount; }
    }

}
