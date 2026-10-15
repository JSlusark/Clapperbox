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


        public static bool IsCsvFile(TextAsset csvAsset)
        {
            if (csvAsset == null) return false;

            string path = AssetDatabase.GetAssetPath(csvAsset); // "Assets/Chatterbox/Prefab/ScreenPlays/Test.csv"
            if (!path.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[Csv Import] Could not import data: csv file must have a .csv extension");
                return false;
            }
            return true;
        }



        public static void Import(ScreenPlayData targetData, TextAsset csvAsset)
        {

            if (!IsCsvFile(csvAsset))
                return;

            if (targetData == null)
            {
                Debug.LogWarning("[Csv Import] Could not import data: no csv found in the Csv Import field");
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
            }

            CharacterObjectsImport.ImportCharacters(targetData);

            EditorUtility.SetDirty(targetData);
            AssetDatabase.SaveAssets();
            Debug.Log($"Successfully imported {targetData.dialogueList.Count} lines into {targetData.name}.");
        }

        public static void Clear(ScreenPlayData targetData)
        {
            if (targetData == null) return;

            Undo.RecordObject(targetData, "Clear Screenplay");
            targetData.dialogueList.Clear();
            targetData.characters.Clear();
            EditorUtility.SetDirty(targetData);
            AssetDatabase.SaveAssets();
            Debug.Log("[Csv Import]Successfully cleared screenPlay list!");
        }
    }
}