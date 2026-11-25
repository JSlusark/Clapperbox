using System.Collections.Generic;
using System.Linq;

namespace Clapperbox.Editor
{
    using UnityEditor;

    using UnityEngine;

    using System.IO;

    using DefaultNamespace;

    [CustomEditor(typeof(CharacterData))]
    public class CharacterDataImport : Editor
    {
        // CONSTANTS
        private const string AudioFolder = "Voice";
        private const string SpritesFolder = "Expressions";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var inspectedObject = (CharacterData)target;
            GUILayout.Space(15);

            if (GUILayout.Button("Import Voice From Folder", GUILayout.Height(30)))
                ImportVoice(inspectedObject);

            if (GUILayout.Button("Import Expressions From Folder", GUILayout.Height(30)))
                ImportExpressions(inspectedObject);
        }

        // The folder the CharacterData asset sits in, e.g. "Assets/Clapperbox/Characters/Nugget"
        public static string GetFolderLocation(CharacterData character)
        {
            string assetLocation = AssetDatabase.GetAssetPath(character);
            return Path.GetDirectoryName(assetLocation);
        }

        public static string GetExpressionsFolder(CharacterData character) =>
            Path.Combine(GetFolderLocation(character), SpritesFolder).Replace('\\', '/');

        private static void SaveImport(CharacterData character)
        {
            EditorUtility.SetDirty(character);
            AssetDatabase.SaveAssets();
        }

        // Static so the Create Dialogue Box tool can fill in characters it creates; returns false when nothing was imported
        public static bool ImportVoice(CharacterData character)
        {
            string folder = GetFolderLocation(character);

            string audioPath = Path.Combine(folder, AudioFolder);

            if (!Directory.Exists(audioPath))
            {
                Debug.LogError($"[Voice] Folder missing");
                return false;
            }

            string[] audioFiles = Directory.GetFiles(audioPath);
            foreach (string filePath in audioFiles)
            {
                AudioClip clip;

                string cleanPath = filePath.Replace('\\', '/');
                if (cleanPath.EndsWith(".meta")) continue;

                string fileName = Path.GetFileNameWithoutExtension(cleanPath);

                if (fileName.Equals("voice", System.StringComparison.OrdinalIgnoreCase))
                {
                    clip = AssetDatabase.LoadAssetAtPath<AudioClip>(cleanPath);
                    if (clip != null)
                    {
                        character.voiceClip = clip;
                        break;
                    }
                }
            }

            if (!character.voiceClip)
            {
                Debug.LogError($"[Voice] Could not import Voice file: folder might be empty, contain invalid name or file type");
                return false;
            }

            SaveImport(character);
            Debug.Log("Voice Import Successful");
            return true;
        }

        public static bool ImportExpressions(CharacterData character)
        {
            string folder = GetFolderLocation(character);

            string spritesDirectory = Path.Combine(folder, SpritesFolder);
            if (!Directory.Exists(spritesDirectory))
            {
                Debug.LogError($"[Expressions] Missing target sprite folder directory at: {spritesDirectory}");
                return false;
            }

            if (character.expressions == null) character.expressions = new List<ExpressionSprite>();
            else character.expressions.Clear();

            string[] spriteFiles = Directory.GetFiles(spritesDirectory);

            foreach (string filePath in spriteFiles)
            {
                string cleanPath = filePath.Replace('\\', '/');
                if (cleanPath.EndsWith(".meta")) continue;

                // Every sprite is an expression named after its file: "Angry-more.png" -> "Angry-more"
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(cleanPath);
                if (sprite != null)
                    character.expressions.Add(new ExpressionSprite
                    {
                        name = Path.GetFileNameWithoutExtension(cleanPath),
                        sprite = sprite
                    });
            }

            if (character.expressions.Count == 0)
            {
                Debug.LogError($"[Expressions] Could not import expressions file: folder might be empty, contain invalid names or file types");
                return false;
            }

            SaveImport(character);
            Debug.Log("Expressions Import Successful");
            return true;
        }
    }
}
