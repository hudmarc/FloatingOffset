using UnityEngine;
using UnityEditor;
using FloatingOffset.Runtime;
using System;
using System.Globalization;

namespace FloatingOffset.Editor
{
    [CustomEditor(typeof(Transform), true)]
    [CanEditMultipleObjects]
    public class TransformUniverseExtension : UnityEditor.Editor
    {
        private UnityEditor.Editor defaultTransformEditor;
        private Vector3 targetPosition;
        private int lastTargetId = -1;

        private static OffsetUniverse cachedUniverse;
        private Vector3d sceneOffset = Vector3d.zero;

        // The key used to save the open/closed state in the registry
        private const string FoldoutPrefKey = "FloatingOffset_TransformFoldout";

        void OnEnable()
        {
            Type transformInspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.TransformInspector");
            if (transformInspectorType != null)
            {
                defaultTransformEditor = UnityEditor.Editor.CreateEditor(targets, transformInspectorType);
            }
        }

        void OnDisable()
        {
            if (defaultTransformEditor != null)
            {
                DestroyImmediate(defaultTransformEditor);
            }
        }

        private static OffsetUniverse GetUniverse()
        {
            if (cachedUniverse != null) return cachedUniverse;

            string[] guids = AssetDatabase.FindAssets("t:OffsetUniverse");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                cachedUniverse = AssetDatabase.LoadAssetAtPath<OffsetUniverse>(path);
            }
            return cachedUniverse;
        }

        public override void OnInspectorGUI()
        {
            // 1. Draw native fields
            if (defaultTransformEditor != null)
            {
                defaultTransformEditor.OnInspectorGUI();
            }
            else
            {
                DrawDefaultInspector();
            }

            Transform t = target as Transform;
            t.TryGetComponent(out OffsetTransform offset_transform);
            if (t == null) return;

            OffsetUniverse universe = GetUniverse();
            if (universe == null) return; // Fail silently if no universe asset exists

            EditorGUILayout.Space();



            // Load the saved foldout state
            bool isExpanded = EditorPrefs.GetBool(FoldoutPrefKey, false);

            string display = $"Floating Offset <{FormatCoordinate(sceneOffset.x)},{FormatCoordinate(sceneOffset.y)},{FormatCoordinate(sceneOffset.z)}>";

            // Draw the foldout header (true parameter allows clicking the text to toggle)
            bool newExpandedState = EditorGUILayout.Foldout(isExpanded, Application.isPlaying && universe != null ? display : "Floating Offset", true, EditorStyles.foldoutHeader);

            // Save the state if the user clicked it
            if (newExpandedState != isExpanded)
            {
                EditorPrefs.SetBool(FoldoutPrefKey, newExpandedState);
            }

            // 2. Draw the contents only if expanded
            if (newExpandedState)
            {
                // Indent the UI so it visually belongs to the foldout
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("helpbox");

                if (!Application.isPlaying)
                {
                    EditorGUILayout.LabelField("[Requires Play Mode]");
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                    return;
                }

                if (!universe.ServerActive || !universe.HasScene(t.gameObject.scene))
                {
                    EditorGUILayout.LabelField("[Scene not registered]");
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                    return;
                }

                sceneOffset = universe.GetSceneOffset(t.gameObject.scene);
                Vector3d position = sceneOffset + Mathd.toVector3d(t.position);

                if (lastTargetId != t.GetInstanceID())
                {
                    targetPosition = new Vector3((float)position.x, (float)position.y, (float)position.z);
                    lastTargetId = t.GetInstanceID();
                }
                EditorGUILayout.LabelField($"Offset: ");

                targetPosition = EditorGUILayout.Vector3Field("Teleport Target", targetPosition);

                // Add a small layout bump so the button doesn't stretch awkwardly across the indent
                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * 15f);

                if (GUILayout.Button("Copy"))
                {
                    // Format explicitly using invariant culture so decimal periods don't break internationally
                    string copyString = $"{position.x.ToString(CultureInfo.InvariantCulture)}, " +
                                        $"{position.y.ToString(CultureInfo.InvariantCulture)}, " +
                                        $"{position.z.ToString(CultureInfo.InvariantCulture)}";
                    EditorGUIUtility.systemCopyBuffer = copyString;
                }

                if (offset_transform != null)
                    if (GUILayout.Button("Paste"))
                    {
                        string clipboard = EditorGUIUtility.systemCopyBuffer;
                        if (!string.IsNullOrEmpty(clipboard))
                        {
                            // Clean up common Vector3 formatting characters
                            clipboard = clipboard.Replace("(", "").Replace(")", "").Replace("<", "").Replace(">", "").Replace(" ", "");
                            string[] parts = clipboard.Split(',');

                            if (parts.Length == 3 &&
                                float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float x) &&
                                float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float y) &&
                                float.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float z))
                            {
                                targetPosition = new Vector3(x, y, z);

                                // Clear GUI focus so the Vector3Field visually updates if the user was currently typing in it
                                GUI.FocusControl(null);
                            }
                            else
                            {
                                Debug.LogWarning("Clipboard does not contain valid Vector3 data.");
                            }
                        }
                    }

                if (GUILayout.Button("Teleport"))
                {
                    Vector3d targetPositionDouble = new Vector3d(targetPosition.x, targetPosition.y, targetPosition.z);

                    if (offset_transform != null)
                    {
                        universe.TeleportTo(offset_transform, targetPositionDouble);
                    }
                    else
                    {
                        Vector3d newWorldPos = targetPositionDouble - sceneOffset;
                        t.position = new Vector3((float)newWorldPos.x, (float)newWorldPos.y, (float)newWorldPos.z);
                    }
                }


                GUILayout.EndHorizontal();


                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
            string FormatCoordinate(double value)
            {
                // Use Math.Abs to catch large negative numbers as well (e.g., -50000)
                if (Math.Abs(value) >= 10000)
                {
                    // "0.##E0" forces scientific notation (e.g., 1.52E4)
                    return value.ToString("0.##E0");
                }
                else
                {
                    // "0.##" keeps your standard formatting (e.g., 150.25)
                    return value.ToString("0.##");
                }
            }
        }
    }
}