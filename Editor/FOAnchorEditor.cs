using UnityEngine;
using UnityEditor;
using FloatingOffset.Runtime;

namespace FloatingOffset.Editor
{
    [CustomEditor(typeof(OffsetAnchor), true)]
    [CanEditMultipleObjects]
    public class FOAnchorEditor : UnityEditor.Editor
    {
        OffsetAnchor anchor;
        void OnEnable()
        {
            anchor = target as OffsetAnchor;
        }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal("box");
                if (GUILayout.Button("Anchor Here"))
                {
                    anchor.SetRealPosition(UnityFunctions.toVector3d(anchor.transform.position));
                }
                if (GUILayout.Button("Reset Anchor"))
                {
                    anchor.SetRealPosition(Vector3d.zero);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {

            }
        }
    }
}