#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    public static class OffsetUniverseEditorHelper
    {
        public static OffsetUniverse GetOrCreateDefaultUniverse()
        {
            // 1. Search the entire project for any asset of type OffsetUniverse
            string[] guids = AssetDatabase.FindAssets("t:OffsetUniverse");

            if (guids.Length > 0)
            {
                // 2. If one exists, load and return the first one found
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<OffsetUniverse>(path);
            }

            // 3. If none exists, create a new one
            OffsetUniverse newUniverse = ScriptableObject.CreateInstance<OffsetUniverse>();
            
            // Define where to save it. (You can change this path to a specific Settings folder)
            string defaultPath = "Assets/DefaultOffsetUniverse.asset"; 
            
            AssetDatabase.CreateAsset(newUniverse, defaultPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[FloatingOffset] Auto-created default OffsetUniverse at: {defaultPath}");
            
            return newUniverse;
        }
    }
}
#endif