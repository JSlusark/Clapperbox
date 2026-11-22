#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefaultNamespace;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Chatterbox.Editor
{
    /*
     * Tools > ChatterBox > Create ChatterBox
     * Pick one or more csv files; each one creates a ChatterBox named after the csv,
     * in Assets/Chatterbox/ChatterBoxes/<CsvName>/:
     *  - a copy of the csv
     *  - <CsvName>_Screenplay.asset, already imported from the csv
     *  - <CsvName>.prefab, a variant of ChatterBox_Prefab with the screenplay assigned
     */
    public class CreateChatterBoxWindow : EditorWindow
    {
        private const string BasePrefabPath = "Assets/Chatterbox/Prefab/ChatterBox_Prefab.prefab";
        private const string OutputRoot = "Assets/Chatterbox/ChatterBoxes";

        private readonly List<string> _csvPaths = new List<string>(); // full paths, one ChatterBox each
        private readonly List<string> _failures = new List<string>(); // "file: reason" from the last run
        private Vector2 _scroll;

        [MenuItem("Tools/ChatterBox/Create ChatterBox")]
        public static void Open()
        {
            var window = GetWindow<CreateChatterBoxWindow>(true, "Create ChatterBox");
            window.minSize = new Vector2(400, 260);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Upload CSV", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Each csv becomes its own ChatterBox.\nFirst row is a header, then one line per row: speaker, expression, text", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add CSV...")) AddFile();
            if (GUILayout.Button("Add Folder...")) AddFolder();
            using (new EditorGUI.DisabledScope(_csvPaths.Count == 0))
                if (GUILayout.Button("Clear", GUILayout.Width(60))) _csvPaths.Clear();
            EditorGUILayout.EndHorizontal();

            DrawFileList();
            DrawFailures();

            string label = _csvPaths.Count == 1 ? "Create 1 ChatterBox" : $"Create {_csvPaths.Count} ChatterBoxes";
            using (new EditorGUI.DisabledScope(_csvPaths.Count == 0))
                if (GUILayout.Button(label, GUILayout.Height(28))) CreateAll();
        }

        // The list doubles as a drop area for csv files from Finder or the Project window
        private void DrawFileList()
        {
            Rect dropArea = EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_csvPaths.Count == 0)
                EditorGUILayout.LabelField("Drop csv files here", EditorStyles.centeredGreyMiniLabel);

            for (int i = 0; i < _csvPaths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(Path.GetFileName(_csvPaths[i]), _csvPaths[i])); // tooltip shows the full path
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    _csvPaths.RemoveAt(i);
                    GUIUtility.ExitGUI(); // list changed mid-draw, stop this repaint
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            HandleDrop(dropArea);
        }

        private void HandleDrop(Rect dropArea)
        {
            Event evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;
            if (!dropArea.Contains(evt.mousePosition)) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (string path in DragAndDrop.paths) // Finder gives full paths, the Project window "Assets/..." paths
                {
                    if (Directory.Exists(path)) AddCsvsInFolder(path);
                    else AddPath(path);
                }
            }
            evt.Use();
        }

        private void DrawFailures()
        {
            if (_failures.Count == 0) return;
            EditorGUILayout.HelpBox("Not created:\n" + string.Join("\n", _failures), MessageType.Error);
        }

        private void AddFile()
        {
            string path = EditorUtility.OpenFilePanel("Select screenplay CSV", "", "csv");
            if (!string.IsNullOrEmpty(path)) AddPath(path); // empty = cancelled
        }

        private void AddFolder()
        {
            string folder = EditorUtility.OpenFolderPanel("Select a folder of screenplay CSVs", "", "");
            if (!string.IsNullOrEmpty(folder)) AddCsvsInFolder(folder);
        }

        private void AddCsvsInFolder(string folder)
        {
            foreach (string path in Directory.GetFiles(folder, "*.csv").OrderBy(p => p))
                AddPath(path);
        }

        private void AddPath(string path)
        {
            if (!path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) return;

            string fullPath = Path.GetFullPath(path);
            if (!_csvPaths.Contains(fullPath)) _csvPaths.Add(fullPath);
            _failures.Clear(); // the list changed, old results no longer apply
        }

        private void CreateAll()
        {
            _failures.Clear();
            var created = new List<UnityEngine.Object>();
            var failedPaths = new List<string>();

            try
            {
                for (int i = 0; i < _csvPaths.Count; i++)
                {
                    string fileName = Path.GetFileName(_csvPaths[i]);
                    EditorUtility.DisplayProgressBar("Creating ChatterBoxes", fileName, (float)i / _csvPaths.Count);

                    GameObject prefab = CreateFromCsv(_csvPaths[i], out string error);
                    if (prefab != null) created.Add(prefab);
                    else
                    {
                        failedPaths.Add(_csvPaths[i]);
                        _failures.Add($"{fileName}: {error}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[Chatterbox] Created {created.Count}/{_csvPaths.Count} ChatterBox(es).");

            if (created.Count > 0)
            {
                EditorUtility.FocusProjectWindow();
                Selection.objects = created.ToArray();
                EditorGUIUtility.PingObject(created[created.Count - 1]);
            }

            // Close when everything worked; otherwise keep only the failed files so they can be fixed and retried
            if (failedPaths.Count == 0)
            {
                Close();
                return;
            }

            _csvPaths.Clear();
            _csvPaths.AddRange(failedPaths);
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

            // The ChatterBox is named after the csv; a number is added if that name is taken
            string name = GetUniqueName(Path.GetFileNameWithoutExtension(csvPath));
            string folder = CreateOutputFolder(name);

            // 1. Copy the csv into the project so Unity can load it as a TextAsset
            string csvAssetPath = $"{folder}/{Path.GetFileName(csvPath)}";
            File.Copy(csvPath, csvAssetPath);
            AssetDatabase.ImportAsset(csvAssetPath);
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>(csvAssetPath);

            // 2. Screenplay, imported with the existing csv importer
            var screenPlay = CreateInstance<ScreenPlayData>();
            screenPlay.csvImport = csv;
            AssetDatabase.CreateAsset(screenPlay, $"{folder}/{name}_Screenplay.asset");
            ScreenPlayCsvImporter.Import(screenPlay, csv);

            if (screenPlay.importState != ImportState.Success)
            {
                AssetDatabase.DeleteAsset(folder); // don't leave half-made ChatterBoxes behind
                error = "The csv could not be imported, check the Console for details.";
                return null;
            }

            // 3. Prefab variant with the screenplay assigned
            GameObject prefab = CreatePrefabVariant(basePrefab, screenPlay, $"{folder}/{name}.prefab", out error);
            if (prefab == null)
            {
                AssetDatabase.DeleteAsset(folder);
                return null;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Chatterbox] Created ChatterBox '{name}' in {folder}");
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

        // "Event_Shop" -> "Event_Shop", or "Event_Shop_2" if a ChatterBox with that name already exists
        private static string GetUniqueName(string baseName)
        {
            string name = baseName;
            for (int i = 2; AssetDatabase.IsValidFolder($"{OutputRoot}/{name}"); i++)
                name = $"{baseName}_{i}";
            return name;
        }

        private static string CreateOutputFolder(string name)
        {
            if (!AssetDatabase.IsValidFolder(OutputRoot))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(OutputRoot), Path.GetFileName(OutputRoot));

            string guid = AssetDatabase.CreateFolder(OutputRoot, name);
            return AssetDatabase.GUIDToAssetPath(guid);
        }
    }
}
#endif
