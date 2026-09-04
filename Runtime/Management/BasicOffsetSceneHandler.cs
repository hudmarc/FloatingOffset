using System;
using System.Collections.Generic;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime.Example
{
    public class BasicOffsetSceneHandler : AbstractOffsetSceneHandler, IOffsetHandler<Scene>
    {
        private Scene last_scene = default;

        /// <summary>
        /// Updates the offset for the given scene.
        /// </summary>
        /// <param name="scene"></param>
        public void UpdateOffset(OffsetScene<Scene> scene)
        {
            var key = scene.key;
            if (state.TryAddOffset(key))
            {
                if (scene.offset == state.GetOffset(scene.key))
                    return;
            }

            if (universe.logging)
                Debug.Log($"OFFSET: [{scene.key.handle.ToHex()}]\n{state.GetOffset(scene.key):#.#}->{scene.offset:#.#} ");
            Vector3d old_offset = state.GetOffset(key);
            state.SetOffset(key,scene.offset);

            if (state.TryGetOffsettable(scene.key, out List<IOffsettable<Scene>> list))
            {
                offsetter.Offset(old_offset, state.GetOffset(key), scene.key, list.ToArray());
            }
            else
            {
                offsetter.Offset(old_offset, state.GetOffset(key), scene.key);
            }
        }

        /// <summary>
        /// Transfer the given offsettable to the given offset scene. Removes it from the offset scene this was called on.<br>
        /// Offsets the transform so that it matches the offset of the target scene.
        /// </summary>
        /// <param name="offsetObject"></param>
        /// <param name="scene"></param>
        public void TransferTo(IOffsetObject<Scene> offsetObject, Scene from, Scene to, bool reposition = false)
        {
            Vector3d absoluteRealPos = state.GetOffset(from) + offsetObject.GetEnginePosition();

            SceneManager.MoveGameObjectToScene(gameObject, to);

            // Calculate the exact local Unity position required for the new scene
            // Because Real = Unity + Offset, therefore Unity = Real - Offset
            if (reposition)
            {
                Vector3d newUnityPos = absoluteRealPos - state.GetOffset(to);

                offsetObject.SetEnginePosition(newUnityPos);
            }
            Scene main_scene = state.GetMainSceneKey();

            if (state.IsMainView(offsetObject))
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
        /// Clone the given scene and clears it of OffsetViews. Calls the callback when done.
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
            SceneManager.LoadSceneAsync(scene.buildIndex, parameters).completed += (arg) => OnLoadEnd(new Scene[] { SceneManager.GetSceneAt(SceneManager.sceneCount - 1) });
            QueueSceneLoadCallback(onSceneReady);
        }

        public void Unload(Scene scene)
        {
            SceneManager.UnloadSceneAsync(scene);
        }
    }
}
