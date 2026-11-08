using System.Collections.ObjectModel;
using FloatingOffset.Runtime.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    // Loosely based on the Unity Wiki FloatingOrigin script by Peter Stirling
    // URL: http://wiki.unity3d.com/index.php/Floating_Origin
    public class Offsetter : MonoBehaviour
    {
        /// <summary>
        /// Offsets objects in the scene without changing the OffsetScene.
        /// If you call this without updating the OffsetScene it will seem as though you teleported all objects in your scene in a direction.
        /// </summary>
        /// <param name="old_offset"></param>
        /// <param name="new_offset"></param>
        /// <param name="scene"></param>
        /// <param name="offsettables"></param>
        public void Offset(Vector3d old_offset, Vector3d new_offset, Scene scene, ReadOnlyCollection<IOffsettable<Scene>> offsettables = null)
        {
            OnOffset(old_offset, new_offset, scene, offsettables); //Calls MoveRootTransforms internally
        }
        private void MoveRootTransforms(Vector3 offset, GameObject[] objects)
        {
            foreach (GameObject g in objects)
            {
                if (g.GetComponent<IgnoreOffset>() == null)
                    g.transform.position += offset;
            }
        }
        protected virtual void OnOffset(Vector3d old_offset, Vector3d new_offset, Scene scene, ReadOnlyCollection<IOffsettable<Scene>> offsettables)
        {
            var objects = scene.GetRootGameObjects();

            Vector3d real_difference = old_offset - new_offset;
            Vector3 difference = OffsetUtils.ToVector3(real_difference);

            if (offsettables != null)
                for (int i = 0; i < offsettables.Count; i++)
                {
                    if (offsettables[i].GetSceneKey() == scene)
                        offsettables[i].OnPreOffset(old_offset, new_offset, scene);
                }

            MoveRootTransforms(difference, objects);

            Vector3 remainder = OffsetUtils.ToVector3(real_difference - OffsetUtils.ToVector3d(difference));

            if (remainder.sqrMagnitude > 0.0f)
                MoveRootTransforms(remainder, objects);

            if (offsettables != null)
                for (int i = 0; i < offsettables.Count; i++)
                {
                    if (offsettables[i].GetSceneKey() == scene)
                        offsettables[i].OnOffset(old_offset, new_offset, scene);
                }
        }
    }
}
