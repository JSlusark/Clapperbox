#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using DefaultNamespace;

namespace Chatterbox.Editor
{
    public class AutomaticFileNameCreator
    {
        [MenuItem("Assets/Create/ChatterBox/CreativeData/Character Data", priority = 0)] // Adds the custom option

        public static void CreateDynamicCharacterSettings()
        {
            string pointedPath = GetPath();
            // heuo
            if (pointedPath == null) return;
            string folderName = Path.GetFileName(pointedPath);
            string targetFilename = $"{folderName}_data.asset";
            string fullAssetPath = Path.Combine(pointedPath, targetFilename);

            var existingAsset = AssetDatabase.LoadAssetAtPath<CharacterData>(fullAssetPath);
            if (existingAsset != null)
            {
                Debug.Log($"Cannot create {targetFilename} because it already exists in ${folderName}");
                return;
            }

            // Creates and and register the file into Unity's database
            CharacterData newAsset = ScriptableObject.CreateInstance<CharacterData>();
            newAsset.speakerName = folderName;
            AssetDatabase.CreateAsset(newAsset, fullAssetPath);
            AssetDatabase.SaveAssets();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newAsset;
            Debug.Log($"Successfully created character settings at: {newAsset}");

        }
        
        private static string GetPath()
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            
            if (string.IsNullOrEmpty(selectedPath))
            {
                Debug.LogWarning("[Chatterbox] Asset generation failed. Please click inside the Project window folder view and try again.");
                return null;
            }
            else if (!AssetDatabase.IsValidFolder(selectedPath)) selectedPath = Path.GetDirectoryName(selectedPath);
            Debug.Log("Selected path: " + selectedPath);
            return selectedPath;
        }

  

    }
}
#endif