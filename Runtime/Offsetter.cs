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
        public void Offset(Vector3d old_offset, Vector3d new_offset, Scene scene, IOffsettable<Scene>[] offsettables = null)
        {
            OnOffset(old_offset, new_offset, scene, offsettables); //Calls MoveRootTransforms internally
        }
        protected virtual void MoveRootTransforms(Vector3 offset, Scene scene)
        {
            var objects = scene.GetRootGameObjects();
            foreach (GameObject g in objects)
            {
                if (g.GetComponent<IgnoreOffset>() == null)
                    g.transform.position += offset;
            }
        }

        protected virtual void MoveOffsettables(IOffsettable<Scene>[] offsettables, Vector3d old_offset, Vector3d new_offset, Scene scene)
        {
            for (int i = 0; i < offsettables.Length; i++)
            {
                offsettables[i].OnOffset(old_offset, new_offset, scene);
            }
        }
        protected virtual void OnOffset(Vector3d old_offset, Vector3d new_offset, Scene scene, IOffsettable<Scene>[] offsettables)
        {
            Vector3d real_difference = old_offset - new_offset;
            Vector3 difference = UnityFunctions.toVector3(real_difference);

            MoveRootTransforms(difference, scene);

            Vector3 remainder = UnityFunctions.toVector3(real_difference - UnityFunctions.toVector3d(difference));

            if (remainder.sqrMagnitude > 0.0f)
                MoveRootTransforms(remainder, scene);

            if (offsettables != null)
                MoveOffsettables(offsettables, old_offset, new_offset, scene);
        }
    }
}
