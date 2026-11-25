using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEditor;

using UnityEngine;

namespace DefaultNamespace
{
    public static class ScreenPlayCsvImporter
    {
        private const string CsvSplitRegex = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";


        // NAME and EXPRESSION of every well-formed row (header skipped), split the same way Import does.
        // Used to check a csv before anything is created; malformed rows are left for Import to report.
        public static List<(string speaker, string expression)> ReadRows(string csvText)
        {
            var rows = new List<(string speaker, string expression)>();
            string[] lines = csvText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = Regex.Split(lines[i], CsvSplitRegex);
                if (fields.Length == 3) rows.Add((fields[0].Trim(' '), fields[1].Trim(' ')));
            }
            return rows;
        }

        public static bool IsCsvFile(TextAsset csvAsset)
        {
            if (csvAsset == null) return false;

            // need to check from file path since the editor doesn't know the file extension from the TextAsset itself
            string path = AssetDatabase.GetAssetPath(csvAsset);
            return path.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase);
        }

        // Wipes the screenplay and flags the error so the inspector can show it.
        private static void Fail(ScreenPlayData targetData, string error)
        {
            Debug.LogWarning($"[Csv Import] Could not import data: {error}");
            targetData.importState = ImportState.Error;
            EditorUtility.SetDirty(targetData);
            AssetDatabase.SaveAssets();
        }

        public static void Clear(ScreenPlayData targetData)
        {
            if (targetData == null) return;

            Undo.RecordObject(targetData, "Clear Screenplay");
            targetData.dialogueList.Clear();
            targetData.characters.Clear();
            targetData.csvImport = null; // when we get error the file sets to null if this is active, are we calling clear also in error?
            targetData.importState = ImportState.Empty;
            EditorUtility.SetDirty(targetData);
            AssetDatabase.SaveAssets();
            Debug.Log("[Csv Import]Successfully cleared screenPlay list!");
        }



        public static void Import(ScreenPlayData targetData, TextAsset csvAsset)
        {
            if (targetData == null)
            {
                Debug.LogWarning("[Csv Import] Could not import data: no ScreenPlayData target");
                return;
            }

            if (csvAsset == null)
            {
                Fail(targetData, "No file was added in the Csv Import box");
                return;
            }

            if (!IsCsvFile(csvAsset))
            {
                Fail(targetData, $"'{csvAsset.name}' is not a .csv file");
                return;
            }

            Undo.RecordObject(targetData, "Import Screenplay");
            targetData.dialogueList.Clear();

            string[] lines = csvAsset.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] fields = Regex.Split(line, CsvSplitRegex);

                if (fields.Length == 3)
                {
                    string nameField = fields[0].Trim(' ');
                    string expressionField = fields[1].Trim(' ');
                    string dialogueField = fields[2].Trim(' ');

                    Line listEntry = new Line();
                    listEntry.speaker = nameField;
                    listEntry.text = dialogueField;
                    if (Enum.TryParse(expressionField, true, out Expression type))
                    {
                        listEntry.expression = type;
                        listEntry.invalidExpression = "None";
                    }
                    else
                    {
                        Debug.LogWarning($"Expression type invalidExpression: '{expressionField}' on line {i + 1} could not convert to enum. Defaulting to Neutral.");
                        listEntry.expression = Expression.Unrecognised;
                        listEntry.invalidExpression = expressionField;
                    }

                    Debug.Log($"{i + 1}.IMPORTED: \n Name: {nameField} \n Line: {dialogueField} \n  Expression: {expressionField}");
                    targetData.dialogueList.Add(listEntry);
                }
                else
                {
                    Fail(targetData, $"line {i + 1} has {fields.Length} columns, expected 3");
                    return;
                }
            }

            CharacterObjectsImport.ImportCharacters(targetData);
            targetData.importState = ImportState.Success;

            EditorUtility.SetDirty(targetData);
            AssetDatabase.SaveAssets();
            Debug.Log($"Successfully imported {targetData.dialogueList.Count} lines into {targetData.name}.");
        }


    }
}