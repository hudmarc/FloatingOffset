using UnityEngine;

namespace FloatingOffset.Runtime
{
    public class OffsetBehaviour : MonoBehaviour
    {
        [field: SerializeField]
        public OffsetUniverse universe { get; private set; }

#if UNITY_EDITOR
        protected virtual void Reset()
        {
            // Fires when the component is first added to a GameObject
            TryAssignDefaultUniverse();
        }

        protected virtual void OnValidate()
        {
            // Fires when the inspector updates. Acts as a safety net.
            TryAssignDefaultUniverse();
        }

        private void TryAssignDefaultUniverse()
        {
            // Only search for the asset if the field is currently empty
            if (universe == null)
            {
                universe = OffsetUniverseEditorHelper.GetOrCreateDefaultUniverse();

                // Tell Unity this component has changed so it prompts you to save the scene
                if (universe != null && !Application.isPlaying)
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}