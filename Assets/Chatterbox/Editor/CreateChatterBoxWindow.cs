#if UNITY_EDITOR
using System.IO;
using DefaultNamespace;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Chatterbox.Editor
{
    /*
     * Tools > ChatterBox > Create ChatterBox
     * Pick a csv and it creates, in Assets/Chatterbox/ChatterBoxes/ChatterBox_n/:
     *  - a copy of the csv
     *  - Screenplay_n.asset, already imported from the csv
     *  - ChatterBox_n.prefab, a variant of ChatterBox_Prefab with the screenplay assigned
     */
    public class CreateChatterBoxWindow : EditorWindow
    {
        private const string BasePrefabPath = "Assets/Chatterbox/Prefab/ChatterBox_Prefab.prefab";
        private const string OutputRoot = "Assets/Chatterbox/ChatterBoxes";

        private string _csvPath;
        private string _error;

        [MenuItem("Tools/ChatterBox/Create ChatterBox")]
        public static void Open()
        {
            var window = GetWindow<CreateChatterBoxWindow>(true, "Create ChatterBox");
            window.minSize = new Vector2(380, 160);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Upload CSV", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("First row is a header, then one line per row: speaker, expression, text", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            string fileLabel = string.IsNullOrEmpty(_csvPath) ? "No file selected" : Path.GetFileName(_csvPath);
            EditorGUILayout.LabelField(fileLabel, EditorStyles.textField); // read-only display of the picked file
            if (GUILayout.Button("Browse...", GUILayout.Width(80))) Browse();
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);

            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_csvPath)))
            {
                if (GUILayout.Button("Create ChatterBox", GUILayout.Height(28)))
                {
                    GameObject prefab = CreateFromCsv(_csvPath, out _error);
                    if (prefab != null)
                    {
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = prefab;
                        EditorGUIUtility.PingObject(prefab);
                        Close();
                    }
                }
            }
        }

        private void Browse()
        {
            string path = EditorUtility.OpenFilePanel("Select screenplay CSV", "", "csv");
            if (string.IsNullOrEmpty(path)) return; // cancelled

            _csvPath = path;
            _error = null;
        }

        // Kept separate from the window so it can also be called from code or tests
        public static GameObject CreateFromCsv(string csvPath, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
            {
                error = "The selected csv file could not be found.";
                return null;
            }

            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            if (basePrefab == null)
            {
                error = $"Base prefab not found at {BasePrefabPath}.";
                return null;
            }

            int n = GetNextNumber();
            string folder = CreateOutputFolder(n);

            // 1. Copy the csv into the project so Unity can load it as a TextAsset
            string csvAssetPath = $"{folder}/{Path.GetFileName(csvPath)}";
            File.Copy(csvPath, csvAssetPath);
            AssetDatabase.ImportAsset(csvAssetPath);
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>(csvAssetPath);

            // 2. Screenplay, imported with the existing csv importer
            var screenPlay = CreateInstance<ScreenPlayData>();
            screenPlay.csvImport = csv;
            AssetDatabase.CreateAsset(screenPlay, $"{folder}/Screenplay_{n}.asset");
            ScreenPlayCsvImporter.Import(screenPlay, csv);

            if (screenPlay.importState != ImportState.Success)
            {
                AssetDatabase.DeleteAsset(folder); // don't leave half-made ChatterBoxes behind
                error = "The csv could not be imported, check the Console for details.";
                return null;
            }

            // 3. Prefab variant with the screenplay assigned
            GameObject prefab = CreatePrefabVariant(basePrefab, screenPlay, $"{folder}/ChatterBox_{n}.prefab", out error);
            if (prefab == null)
            {
                AssetDatabase.DeleteAsset(folder);
                return null;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Chatterbox] Created ChatterBox_{n} in {folder}");
            return prefab;
        }

        private static GameObject CreatePrefabVariant(GameObject basePrefab, ScreenPlayData screenPlay, string path, out string error)
        {
            error = null;

            // A preview scene keeps the temporary instance out of the open scene
            var previewScene = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, previewScene);
                var manager = instance.GetComponentInChildren<DialogueManager>(true);
                if (manager == null)
                {
                    error = "ChatterBox_Prefab has no DialogueManager.";
                    return null;
                }

                // screenPlay is a private [SerializeField], so it is set through SerializedObject
                var serializedManager = new SerializedObject(manager);
                serializedManager.FindProperty("screenPlay").objectReferenceValue = screenPlay;
                serializedManager.ApplyModifiedPropertiesWithoutUndo();

                // Saving an instance of a prefab creates a variant of it
                return PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static int GetNextNumber()
        {
            int n = 1;
            while (AssetDatabase.IsValidFolder($"{OutputRoot}/ChatterBox_{n}")) n++;
            return n;
        }

        private static string CreateOutputFolder(int n)
        {
            if (!AssetDatabase.IsValidFolder(OutputRoot))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(OutputRoot), Path.GetFileName(OutputRoot));

            string guid = AssetDatabase.CreateFolder(OutputRoot, $"ChatterBox_{n}");
            return AssetDatabase.GUIDToAssetPath(guid);
        }
    }
}
#endif
