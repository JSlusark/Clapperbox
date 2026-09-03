using System.Collections.Generic;
using System.Linq;

namespace Chatterbox.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.IO;
    using DefaultNamespace;

    [CustomEditor(typeof(CharacterData))]
    public class CharacterDataImport : Editor
    {
        private CharacterData _inspectedObject;
        private string _folder;

        // CONSTANTS
        private const string AudioFolder = "Voice";
        private const string SpritesFolder = "Expressions";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            _inspectedObject = (CharacterData)target;
            GUILayout.Space(15);


            // folder = GetFolderLocation();

            if (GUILayout.Button("Import Voice From Folder", GUILayout.Height(30)))
                ImportAudio();

            if (GUILayout.Button("Import Expressions From Folder", GUILayout.Height(30)))
                ImportExpressions();
        }

        private string GetFolderLocation()
        {
            string assetLocation = AssetDatabase.GetAssetPath(_inspectedObject);
            return Path.GetDirectoryName(assetLocation);
        }

        private void SaveImport()
        {
            EditorUtility.SetDirty(_inspectedObject);
            AssetDatabase.SaveAssets();

        }

        private void ImportAudio()
        {
            _folder = GetFolderLocation();

            string audioPath = Path.Combine(_folder, AudioFolder);

            if (!Directory.Exists(audioPath))
            {
                Debug.LogError($"[Voice] Folder missing");
                return;
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
                        _inspectedObject.chatterSound = clip;
                        break;
                    }
                }
            }

            if (!_inspectedObject.chatterSound)
            {
                Debug.LogError($"[Voice] Could not import Voice file: folder might be empty, contain invalid name or file type");
                return;
            }

            SaveImport();
            Debug.Log("Voice Import Successful");
        }

        private void ImportExpressions()
        {
            _folder = GetFolderLocation();

            string spritesDirectory = Path.Combine(_folder, SpritesFolder);
            if (!Directory.Exists(spritesDirectory))
            {
                Debug.LogError($"[Expressions] Missing target sprite folder directory at: {spritesDirectory}");
                return;
            }

            if (_inspectedObject.expressions.Count > 0)
                _inspectedObject.expressions.Clear();

            string[] spriteFiles = Directory.GetFiles(spritesDirectory);

            List<string> unrecognisedFiles = new List<string>();
            foreach (string filePath in spriteFiles)
            {
                string cleanPath = filePath.Replace('\\', '/');
                if (cleanPath.EndsWith(".meta")) continue;


                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(cleanPath);
                if (sprite != null)
                {
                    ExpressionSprite newExpression = new()
                    {
                        sprite = sprite
                    };

                    // Parses file name to assign to enum
                    string fileName = Path.GetFileNameWithoutExtension(cleanPath);
                    if (System.Enum.TryParse(fileName, true, out Expression matchedType))
                        newExpression.type = matchedType;
                    else
                    {
                        newExpression.type = default;
                        unrecognisedFiles.Add(fileName);
                    }

                    _inspectedObject.expressions.Add(newExpression);
                }
            }

            if (unrecognisedFiles.Count > 0)
            {
                string enumFilePath = "Assets/Chatterbox/Runtime/Scripts/Enums/Expression.cs";

                Debug.LogWarning($"[Expressions] Added expressions as unrecognised: {string.Join(", ", unrecognisedFiles)}");
                Debug.Log($"Add the new expression <a href=\"{enumFilePath}\" line=\"1\">[here]</a> or check for correct file name and import again");
            }

            if (_inspectedObject.expressions.Count == 0)
            {
                Debug.LogError($"[Expressions] Could not import expressions file: folder might be empty, contain invalid names or file types");
                return;
            }

            SaveImport();
            Debug.Log("Expressions Import Successful");

        }
    }
}