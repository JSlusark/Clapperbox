
using UnityEditor;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using DefaultNamespace;


public class DialogueImporterWindow : EditorWindow
{
    
    //  private DialogueData _target;
    // private string _log = "";
    // private int _valid = 0;
    // private int _skipped = 0;
    //
    // [MenuItem("Window/Dialogue/CSV Importer")]
    // public static void ShowWindow() => GetWindow<DialogueImporterWindow>("Dialogue CSV Importer");
    //
    // private void OnGUI()
    // {
    //     EditorGUILayout.Space();
    //     _target = (DialogueData)EditorGUILayout.ObjectField("Target Dialogue Asset", _target, typeof(DialogueData), true);
    //
    //     EditorGUILayout.Space();
    //     EditorGUILayout.BeginVertical("box");
    //     EditorGUILayout.LabelField("Instructions", EditorStyles.boldLabel);
    //     EditorGUILayout.LabelField("1. Select a DialogueData asset above");
    //     EditorGUILayout.LabelField("2. Drag & drop a CSV file here");
    //     EditorGUILayout.LabelField("Format: text,expression", EditorStyles.miniLabel);
    //     EditorGUILayout.EndVertical();
    //
    //     EditorGUILayout.Space();
    //     if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
    //     {
    //         DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
    //         if (Event.current.type == EventType.DragPerform)
    //         {
    //             DragAndDrop.AcceptDrag();
    //             if (DragAndDrop.paths.Length > 0 && File.Exists(DragAndDrop.paths[0]))
    //                 ImportCSV(DragAndDrop.paths[0]);
    //         }
    //         Event.current.Use();
    //     }
    //
    //     EditorGUILayout.Space();
    //     EditorGUILayout.LabelField("Preview & Log", EditorStyles.boldLabel);
    //     EditorGUILayout.TextArea(_log, GUILayout.Height(120));
    //
    //     EditorGUILayout.Space();
    //     if (_target != null && GUILayout.Button("📥 Import CSV"))
    //     {
    //         string path = EditorUtility.OpenFilePanel("Select Dialogue CSV", "", "csv");
    //         if (!string.IsNullOrEmpty(path)) ImportCSV(path);
    //     }
    // }
    //
    // private void ImportCSV(string path)
    // {
    //     if (_target == null) { Debug.LogError("Select a DialogueData asset first."); return; }
    //
    //     var lines = new List<DialogueLine>();
    //     _valid = 0; _skipped = 0; _log = "";
    //
    //     using (var reader = new StreamReader(path))
    //     {
    //         reader.ReadLine(); // Skip header
    //         string line;
    //         int lineNum = 1;
    //         while ((line = reader.ReadLine()) != null)
    //         {
    //             lineNum++;
    //             if (string.IsNullOrWhiteSpace(line)) continue;
    //
    //             var parts = SplitCSVLine(line);
    //             if (parts.Length < 2) { _skipped++; _log += $"Line {lineNum}: Skipped (bad format)\n"; continue; }
    //
    //             string text = parts[0].Trim(' ', '"');
    //             string exprStr = parts[1].Trim(' ', '"');
    //
    //             if (Enum.TryParse(exprStr, true, out Expression expr))
    //             {
    //                 lines.Add(new DialogueLine { text = text, expression = expr });
    //                 _valid++;
    //             }
    //             else
    //             {
    //                 _skipped++;
    //                 _log += $"Line {lineNum}: Skipped (invalid expression '{exprStr}')\n";
    //             }
    //         }
    //     }
    //
    //     if (_valid > 0)
    //     {
    //         Undo.RecordObject(_target, "Import Dialogue CSV");
    //         _target.lines = lines;
    //         EditorUtility.SetDirty(_target);
    //         AssetDatabase.SaveAssets();
    //         AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_target));
    //         _log = $"✅ Imported {_valid} lines. Skipped {_skipped}.\n" + _log;
    //     }
    //     else
    //     {
    //         _log = "❌ No valid lines found.";
    //     }
    // }
    //
    // private string[] SplitCSVLine(string line)
    // {
    //     var result = new List<string>();
    //     var current = new System.Text.StringBuilder();
    //     bool inQuotes = false;
    //     for (int i = 0; i < line.Length; i++)
    //     {
    //         char c = line[i];
    //         if (c == '"') inQuotes = !inQuotes;
    //         else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
    //         else current.Append(c);
    //     }
    //     result.Add(current.ToString());
    //     return result.ToArray();
    // }
    //
}