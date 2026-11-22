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
        public void Offset(Vector3d old_offset, Vector3d new_offset, Scene scene, OffsettableRegistry registry)
        {
            OnOffset(old_offset, new_offset, scene, registry); //Calls MoveRootTransforms internally
        }
        private void MoveRootTransforms(Vector3 difference, GameObject[] objects, Vector3d old_offset, Vector3d new_offset, Scene scene, OffsettableRegistry registry)
        {
            foreach (GameObject g in objects)
            {
                registry.DoPreCallbacks(g, old_offset, new_offset, scene);

                if (!registry.ShouldIgnore(g))
                    g.transform.position += difference;
                    
                registry.DoCallbacks(g, old_offset, new_offset, scene);
            }
        }
        protected virtual void OnOffset(Vector3d old_offset, Vector3d new_offset, Scene scene, OffsettableRegistry registry)
        {
            var objects = scene.GetRootGameObjects(); //TODO: Could this be a sorted cache that is refreshed every few frames?
            // i.e. first 'n' objects are Offsettable, last k objects are IgnoreOffset

            Vector3d real_difference = old_offset - new_offset;
            Vector3 difference = real_difference.ToVector3();

            MoveRootTransforms(difference, objects, old_offset, new_offset, scene, registry);

            Vector3 remainder = (real_difference - difference.ToVector3d()).ToVector3();

            if (remainder.sqrMagnitude > 0.0f)
                MoveRootTransforms(remainder, objects, old_offset, new_offset, scene, registry);


            foreach (GameObject g in objects)
            {
                registry.DoPostCallbacks(g, old_offset, new_offset, scene);
            }
        }
    }
}
