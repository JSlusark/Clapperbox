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
     *  1. Style: every ChatterBox prefab inside a folder of BoxStyles (BoxStyles/<Folder>/<Prefab>.prefab).
     *     New folders with a ChatterBox prefab show up automatically.
     *  2. CSV source: "Use example CSV" (only for Sample_<Name> folders that have a <Name>_sample.csv)
     *     or "Use custom CSV" (always available, the only choice for non-sample styles).
     *  3. Custom CSV: add one or more csv files; each one creates its own ChatterBox named after it:
     *  - Chatterbox/BoxPrefabs/<CsvName>.prefab              variant of the chosen style with the screenplay assigned
     *  - Chatterbox/ScreenPlays/<CsvName>_Screenplay.asset   already imported from the csv
     *  - Chatterbox/ScreenPlays/raw_csv/<CsvName>.csv        only for csv files picked from outside the project;
     *                                                        csv files already in the project are used where they are
     */
    public class CreateChatterBoxWindow : EditorWindow
    {
        private const string BasePrefabPath = "Assets/Chatterbox/BoxPrefabs/ChatterBox_Prefab.prefab";
        private const string ChatterBoxesFolder = "Assets/Chatterbox/BoxPrefabs";
        private const string ScreenplaysFolder = "Assets/Chatterbox/ScreenPlays";
        private const string PlaywrightFolder = "Assets/Chatterbox/ScreenPlays/raw_csv";
        private const string StylesFolder = "Assets/Chatterbox/BoxStyles";
        private const string SamplePrefix = "Sample_";
        private const string StylePrefKey = "Chatterbox.CreateChatterBox.Style"; // remembers the last style picked
        private const int ThumbWidth = 1920, ThumbHeight = 1080; // rendered at full size, then cropped to the box
        private const int ThumbMargin = 16;                       // px kept around the box when cropping
        private const float MaxPreviewHeight = 260;
        private static readonly Color PreviewBackground = new Color(0.16f, 0.16f, 0.16f);

        private enum CsvSource { Example, Custom }

        private class BoxStyle
        {
            public GameObject Prefab;
            public string Label;      // shown in the dropdown
            public TextAsset Example; // the sample's csv, null for non-sample styles
        }

        private readonly List<string> _csvPaths = new List<string>(); // full paths, one ChatterBox each
        private readonly List<string> _failures = new List<string>(); // "file: reason" from the last run
        private Vector2 _scroll;

        private readonly List<BoxStyle> _styles = new List<BoxStyle>();
        private readonly Dictionary<GameObject, Texture2D> _thumbnails = new Dictionary<GameObject, Texture2D>();
        private int _styleIndex;
        private CsvSource _source = CsvSource.Example;

        [MenuItem("Tools/ChatterBox/Create ChatterBox")]
        public static void Open()
        {
            var window = GetWindow<CreateChatterBoxWindow>(true, "Create ChatterBox");
            window.minSize = new Vector2(420, 520);
        }

        private void OnEnable() => LoadStyles();

        private void OnDisable()
        {
            foreach (var texture in _thumbnails.Values)
                if (texture != null) DestroyImmediate(texture);
            _thumbnails.Clear();
        }

        // Style prefabs and folders can be added or edited while the window is open
        private void OnProjectChange()
        {
            OnDisable();
            LoadStyles();
            Repaint();
        }

        // ---- Layout styles ---------------------------------------------------------------------------------

        private const string StyleLabelText = "Selected style:";
        private const string DialogueLabelText = "Selected dialogue:";

        private static GUIStyle _headerStyle, _paddingStyle;

        private static GUIStyle HeaderStyle => _headerStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };

        // A little room on every side of the window's content
        private static GUIStyle PaddingStyle => _paddingStyle ??= new GUIStyle { padding = new RectOffset(10, 10, 6, 6) };

        // Both dropdown labels share one width so the dropdowns line up, sized to the longer label (no extra gap)
        private static float DropdownLabelWidth => Mathf.Max(
            EditorStyles.boldLabel.CalcSize(new GUIContent(StyleLabelText)).x,
            EditorStyles.boldLabel.CalcSize(new GUIContent(DialogueLabelText)).x) + 2;

        // Bold label right next to its dropdown
        private static int LabeledPopup(string label, int index, string[] options)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(DropdownLabelWidth));
            index = EditorGUILayout.Popup(index, options);
            EditorGUILayout.EndHorizontal();
            return index;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(PaddingStyle, GUILayout.ExpandHeight(true));
            try { DrawContent(); }
            finally { EditorGUILayout.EndVertical(); }
        }

        private void DrawContent()
        {
            DrawStylePicker();
            if (SelectedStyle == null) return;
            EditorGUILayout.Space(8);

            DrawSourcePicker();
            EditorGUILayout.Space(4);

            if (UsingExample)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{SelectedStyle.Example.name}.csv");
                if (GUILayout.Button("Locate", GUILayout.Width(60))) EditorGUIUtility.PingObject(SelectedStyle.Example);
                EditorGUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                DrawFailures();
                if (GUILayout.Button("Create ChatterBox", GUILayout.Height(28))) CreateFromExample();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select CSV...")) AddFile();
            if (GUILayout.Button("Add all from folder...")) AddFolder();
            using (new EditorGUI.DisabledScope(_csvPaths.Count == 0))
                if (GUILayout.Button("Clear", GUILayout.Width(60))) _csvPaths.Clear();
            EditorGUILayout.EndHorizontal();

            DrawFileList();
            DrawFailures();

            string label = _csvPaths.Count == 1 ? "Create 1 ChatterBox" : $"Create {_csvPaths.Count} ChatterBoxes";
            using (new EditorGUI.DisabledScope(_csvPaths.Count == 0))
                if (GUILayout.Button(label, GUILayout.Height(28))) CreateAll();
        }

        // ---- Style picker ---------------------------------------------------------------------------------

        // One entry per ChatterBox prefab inside a sub-folder of BoxStyles; prefabs loose in BoxStyles are ignored
        private void LoadStyles()
        {
            _styles.Clear();
            if (AssetDatabase.IsValidFolder(StylesFolder))
            {
                var found = AssetDatabase.FindAssets("t:Prefab", new[] { StylesFolder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(path => (path, folder: StyleFolderName(path)))
                    .Where(p => p.folder != null)
                    .Select(p => (p.path, p.folder, prefab: AssetDatabase.LoadAssetAtPath<GameObject>(p.path)))
                    .Where(p => p.prefab != null && p.prefab.GetComponentInChildren<DialogueManager>(true) != null)
                    .OrderBy(p => p.folder).ThenBy(p => p.path)
                    .ToList();

                foreach (var group in found.GroupBy(p => p.folder))
                {
                    var example = FindExampleCsv(group.Key);
                    bool several = group.Count() > 1;
                    foreach (var p in group)
                        _styles.Add(new BoxStyle
                        {
                            Prefab = p.prefab,
                            Label = several ? $"{group.Key}/{p.prefab.name}" : group.Key, // "/" makes a sub-menu
                            Example = example,
                        });
                }
            }

            string remembered = EditorPrefs.GetString(StylePrefKey, "");
            _styleIndex = Mathf.Max(0, _styles.FindIndex(s => AssetDatabase.GetAssetPath(s.Prefab) == remembered));
            ClampSource();
        }

        // "Assets/Chatterbox/BoxStyles/Sample_CozyLand/X.prefab" -> "Sample_CozyLand"; null when not inside a sub-folder
        private static string StyleFolderName(string prefabPath)
        {
            string relative = prefabPath.Substring(StylesFolder.Length).TrimStart('/');
            int slash = relative.IndexOf('/');
            return slash > 0 ? relative.Substring(0, slash) : null;
        }

        // Sample_CozyLand -> the project's CozyLand_sample.csv (preferring the style folder, then ScreenPlays/raw_csv)
        private static TextAsset FindExampleCsv(string folder)
        {
            if (!folder.StartsWith(SamplePrefix, StringComparison.OrdinalIgnoreCase)) return null;

            string fileName = folder.Substring(SamplePrefix.Length) + "_sample.csv";
            string path = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName) + " t:TextAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => string.Equals(Path.GetFileName(p), fileName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.StartsWith($"{StylesFolder}/{folder}/"))
                .ThenByDescending(p => p.StartsWith($"{PlaywrightFolder}/"))
                .ThenBy(p => p)
                .FirstOrDefault();
            return path != null ? AssetDatabase.LoadAssetAtPath<TextAsset>(path) : null;
        }

        private BoxStyle SelectedStyle => _styles.Count > 0 ? _styles[_styleIndex] : null;
        private bool UsingExample => _source == CsvSource.Example && SelectedStyle?.Example != null;

        // Non-sample styles can only use a custom csv
        private void ClampSource()
        {
            if (SelectedStyle?.Example == null) _source = CsvSource.Custom;
        }

        private void DrawStylePicker()
        {
            DrawHeader("Choose your style:",
                "Sets your ChatterBox UI.\n" +
                "Each style is a prefab that already includes the logic for setting up your screenplay presentation.");
            if (_styles.Count == 0)
            {
                EditorGUILayout.HelpBox($"No ChatterBox styles found. Add a folder with a ChatterBox prefab to {StylesFolder}.", MessageType.Warning);
                return;
            }

            // Preview of the selected style, cropped to the ChatterBox itself
            Texture2D thumbnail = GetThumbnail(SelectedStyle.Prefab);
            float aspect = thumbnail != null ? (float)thumbnail.width / thumbnail.height : 16f / 9f;
            Rect preview = GUILayoutUtility.GetAspectRect(aspect, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(MaxPreviewHeight));
            EditorGUI.DrawRect(preview, PreviewBackground);
            if (thumbnail != null) GUI.DrawTexture(preview, thumbnail, ScaleMode.ScaleToFit);

            EditorGUILayout.BeginHorizontal();
            int picked = LabeledPopup(StyleLabelText, _styleIndex, _styles.Select(s => s.Label).ToArray());
            if (GUILayout.Button("Locate", GUILayout.Width(60))) EditorGUIUtility.PingObject(SelectedStyle.Prefab);
            EditorGUILayout.EndHorizontal();

            if (picked != _styleIndex)
            {
                bool hadExample = SelectedStyle.Example != null;
                _styleIndex = picked;
                EditorPrefs.SetString(StylePrefKey, AssetDatabase.GetAssetPath(SelectedStyle.Prefab));
                if (!hadExample && SelectedStyle.Example != null) _source = CsvSource.Example; // samples default to their example
                ClampSource();
                _failures.Clear();
            }
        }

        private void DrawSourcePicker()
        {
            DrawHeader("Load a screenplay",
                "Screenplays are .CSV files.\n" +
                "Screenplays load dialogue and assets into your ChatterBox.\n" +
                "Each row of the .CSV contains the speaker name, their line and their linked asset.");

            if (SelectedStyle.Example == null)
            {
                using (new EditorGUI.DisabledScope(true))
                    LabeledPopup(DialogueLabelText, 0, new[] { "Custom CSV" });
                return;
            }

            int picked = LabeledPopup(DialogueLabelText, (int)_source, new[] { "Sample CSV", "Custom CSV" });
            if (picked != (int)_source)
            {
                _source = (CsvSource)picked;
                _failures.Clear();
            }
        }

        // Bold header with an info-icon subtitle right under it, and some room before the section's content
        private static void DrawHeader(string title, string subtitle)
        {
            EditorGUILayout.LabelField(title, HeaderStyle, GUILayout.Height(22));
            GUILayout.Space(-2); // pulls the subtitle up against its header

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent("console.infoicon.sml"), GUILayout.Width(18), GUILayout.Height(18));
            EditorGUILayout.LabelField(subtitle, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
        }

        private Texture2D GetThumbnail(GameObject style)
        {
            if (style == null) return null;
            if (!_thumbnails.TryGetValue(style, out var texture) || texture == null)
                _thumbnails[style] = texture = RenderThumbnail(style);
            return texture;
        }

        // Renders the prefab's UI as it looks in game: an instance in a preview scene, its canvas switched to a camera
        private static Texture2D RenderThumbnail(GameObject prefab)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var target = new RenderTexture(ThumbWidth, ThumbHeight, 24);
            try
            {
                var cameraObject = new GameObject("ThumbnailCamera");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.scene = scene;
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                // Alpha 0 marks empty screen, so the box can be found and cropped afterwards
                camera.backgroundColor = new Color(PreviewBackground.r, PreviewBackground.g, PreviewBackground.b, 0);
                camera.targetTexture = target;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                foreach (var canvas in instance.GetComponentsInChildren<Canvas>(true))
                {
                    if (!canvas.isRootCanvas) continue;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 5;
                }
                Canvas.ForceUpdateCanvases();
                foreach (var text in instance.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.ForceMeshUpdate();
                camera.Render();

                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var full = new Texture2D(ThumbWidth, ThumbHeight, TextureFormat.RGBA32, false);
                full.ReadPixels(new Rect(0, 0, ThumbWidth, ThumbHeight), 0, 0);
                RenderTexture.active = previous;
                camera.targetTexture = null;

                var texture = CropToContent(full);
                DestroyImmediate(full);
                return texture;
            }
            finally
            {
                DestroyImmediate(target);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        // Cuts the render down to the visibly drawn pixels plus a small margin; the whole render if nothing was drawn
        private static Texture2D CropToContent(Texture2D full)
        {
            int w = full.width, h = full.height;
            Color32[] pixels = full.GetPixels32();
            int minX = w, maxX = -1, minY = h, maxY = -1;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a <= 12) continue; // ignore near-invisible pixels (faint sprite edges)
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            if (maxX < 0) { minX = 0; minY = 0; maxX = w - 1; maxY = h - 1; }
            minX = Mathf.Max(0, minX - ThumbMargin);
            minY = Mathf.Max(0, minY - ThumbMargin);
            maxX = Mathf.Min(w - 1, maxX + ThumbMargin);
            maxY = Mathf.Min(h - 1, maxY + ThumbMargin);

            int cw = maxX - minX + 1, ch = maxY - minY + 1;
            var cropped = new Texture2D(cw, ch, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
            cropped.SetPixels(full.GetPixels(minX, minY, cw, ch)); // RGB is already drawn over the preview background
            cropped.Apply();
            return cropped;
        }

        // ---- CSV list --------------------------------------------------------------------------------------

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

                    GameObject prefab = CreateFromCsv(_csvPaths[i], SelectedStyle.Prefab, out string error);
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

        private void CreateFromExample()
        {
            _failures.Clear();
            string csvPath = Path.GetFullPath(AssetDatabase.GetAssetPath(SelectedStyle.Example));
            GameObject prefab = CreateFromCsv(csvPath, SelectedStyle.Prefab, out string error);
            if (prefab == null)
            {
                _failures.Add($"{Path.GetFileName(csvPath)}: {error}");
                return;
            }

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Close();
        }

        // Kept separate from the window so it can also be called from code or tests
        public static GameObject CreateFromCsv(string csvPath, out string error) => CreateFromCsv(csvPath, null, out error);

        // style: the ChatterBox prefab to make a variant of; null = ChatterBox_Prefab
        public static GameObject CreateFromCsv(string csvPath, GameObject style, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
            {
                error = "The selected csv file could not be found.";
                return null;
            }

            var basePrefab = style != null ? style : AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            if (basePrefab == null)
            {
                error = $"Base prefab not found at {BasePrefabPath}.";
                return null;
            }

            EnsureFolder(ChatterBoxesFolder);
            EnsureFolder(ScreenplaysFolder);
            EnsureFolder(PlaywrightFolder);

            // The ChatterBox is named after the csv; a number is added if that name is taken
            string name = GetUniqueName(Path.GetFileNameWithoutExtension(csvPath));
            string screenPlayPath = $"{ScreenplaysFolder}/{name}_Screenplay.asset";
            string prefabPath = $"{ChatterBoxesFolder}/{name}.prefab";

            // 1. The csv as a TextAsset: used in place if it's in the project, otherwise copied into Playwright
            string csvAssetPath = GetCsvAssetPath(csvPath, out bool copied);
            var csv = AssetDatabase.LoadAssetAtPath<TextAsset>(csvAssetPath);

            // 2. Screenplay, imported with the existing csv importer.
            //    A screenplay left behind by a deleted ChatterBox is reused, so its name (and any references to it) carry on
            var screenPlay = AssetDatabase.LoadAssetAtPath<ScreenPlayData>(screenPlayPath);
            bool reused = screenPlay != null;
            if (!reused)
            {
                screenPlay = CreateInstance<ScreenPlayData>();
                AssetDatabase.CreateAsset(screenPlay, screenPlayPath);
            }
            screenPlay.csvImport = csv;
            ScreenPlayCsvImporter.Import(screenPlay, csv);

            // Don't leave half-made ChatterBoxes behind; a reused screenplay is kept, it existed before this run
            string createdScreenPlayPath = reused ? null : screenPlayPath;
            if (screenPlay.importState != ImportState.Success)
            {
                Cleanup(createdScreenPlayPath, copied ? csvAssetPath : null);
                error = "The csv could not be imported, check the Console for details.";
                return null;
            }

            // 3. Prefab variant with the screenplay assigned
            GameObject prefab = CreatePrefabVariant(basePrefab, screenPlay, prefabPath, out error);
            if (prefab == null)
            {
                Cleanup(createdScreenPlayPath, copied ? csvAssetPath : null);
                return null;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Chatterbox] Created ChatterBox '{name}' at {prefabPath}");
            return prefab;
        }

        // Returns the csv's "Assets/..." path, copying it into Playwright first if it lives outside the project
        private static string GetCsvAssetPath(string csvPath, out bool copied)
        {
            string fullPath = Path.GetFullPath(csvPath).Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/') + "/";
            copied = !fullPath.StartsWith(dataPath);

            if (!copied) return "Assets/" + fullPath.Substring(dataPath.Length);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{PlaywrightFolder}/{Path.GetFileName(csvPath)}");
            File.Copy(fullPath, assetPath);
            AssetDatabase.ImportAsset(assetPath);
            return assetPath;
        }

        private static void Cleanup(string screenPlayPath, string copiedCsvPath)
        {
            if (screenPlayPath != null) AssetDatabase.DeleteAsset(screenPlayPath);
            if (copiedCsvPath != null) AssetDatabase.DeleteAsset(copiedCsvPath); // never delete the user's own csv
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
                    error = $"{basePrefab.name} has no DialogueManager.";
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
            for (int i = 2; NameTaken(name); i++)
                name = $"{baseName}_{i}";
            return name;
        }

        // Only an existing ChatterBox takes a name. A leftover screenplay doesn't (it gets reused),
        // unless something other than a screenplay sits at its path
        private static bool NameTaken(string name)
        {
            string screenPlayPath = $"{ScreenplaysFolder}/{name}_Screenplay.asset";
            return AssetDatabase.LoadMainAssetAtPath($"{ChatterBoxesFolder}/{name}.prefab") != null
                || (AssetDatabase.LoadMainAssetAtPath(screenPlayPath) != null
                    && AssetDatabase.LoadAssetAtPath<ScreenPlayData>(screenPlayPath) == null);
        }

        // Creates every missing folder in a path like "Assets/Dialogue/ChatterBoxes"
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
