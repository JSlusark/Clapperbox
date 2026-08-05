using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DefaultNamespace
{
    [System.Serializable]
    public class Dialogue
    {
        [TextArea(3, 6)] public string text;
        public Expression expression = Expression.Neutral;
    }

    [CreateAssetMenu(fileName = "Dialogue", menuName = "Chatterbox/Dialogue", order = 0)]
    public class DialogueData : ScriptableObject
    {
        [Header("Dialogue details:")] public List<Dialogue> dialogueList = new List<Dialogue>();

        [Header("Import From Csv:")] [Tooltip("Drag your .csv file here, then click the button below.")]
        public TextAsset csvImport;


        public void ClearDialogueList()
        {
            if (csvImport == null) return;
            dialogueList.Clear();
            Debug.Log($"Successfully cleared dialogueList list!");
        }

        public void ImportCsv() // Serparate in its own class and import  declaration here later?
        {
            if (csvImport == null)
            {
                Debug.LogWarning("No CSV file assigned! Please assign a TextAsset first.");
                return;
            }

            dialogueList.Clear();

            string[] lines = csvImport.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            string regex = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";

            Debug.Log($"Skipping Header line: {lines[0]}");
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] fields = Regex.Split(line, regex);

                if (fields.Length == 2)
                {
                    string dialogueField = fields[0].Trim(' ');
                    string expressionField = fields[1].Trim(' ');

                    Dialogue listEntry = new Dialogue();
                    listEntry.text = dialogueField;
                    if (Enum.TryParse(expressionField, true, out Expression type))
                        listEntry.expression = type;
                    else
                    {
                        Debug.LogWarning($"Expression type unrecognised: '{expressionField}' on line {i + 1} could not convert to enum. Defaulting to Neutral.");
                        listEntry.expression = Expression.Neutral;
                    }

                    Debug.Log($"{i + 1}.IMPORTED: \n  Dialogue: {dialogueField} \n  Expression: {expressionField}");
                    dialogueList.Add(listEntry);
                }
            }

            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            #endif

            Debug.Log($"Successfully imported {dialogueList.Count} dialogueList lines!");
        }
    }

    // Adds features to the DialogueData scriptable object - to move in editor folder
    #if UNITY_EDITOR
    [CustomEditor(typeof(DialogueData))]
    public class DialogueDataEditor : Editor
    {
        private SerializedProperty dialogueProp;
        private SerializedProperty csvImportProp;

        private void OnEnable()
        {
            dialogueProp = serializedObject.FindProperty("dialogueList"); // grabs property reference
            csvImportProp = serializedObject.FindProperty("csvImport");
        }

        public override void OnInspectorGUI() // Redraws the inspector gui elements to customise its view
        {
            serializedObject.Update();

            DialogueData script = (DialogueData)target; // targets the dialogue data 

            // dialogueProp.isExpanded = EditorGUILayout.Foldout(dialogueProp.isExpanded, dialogueProp.displayName, true);

            // Draws dialogue list field with children and adds clear button below the list
            EditorGUILayout.PropertyField(dialogueProp, true);
            EditorGUILayout.BeginHorizontal();
            // dialogueProp.isExpanded = EditorGUILayout.Foldout(dialogueProp.isExpanded, dialogueProp.displayName, true);
            // dialogueProp.arraySize = EditorGUILayout.DelayedIntField(GUIContent.none, dialogueProp.arraySize, GUILayout.Width(50));
            GUILayout.FlexibleSpace(); // Pushes the button to the right edge
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
                script.ClearDialogueList();
            EditorGUILayout.EndHorizontal();


            // Draws the CSV import field and import button
            EditorGUILayout.LabelField(csvImportProp.displayName, EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            // Directly draw the Object Picker field for TextAsset without any label
            csvImportProp.objectReferenceValue = EditorGUILayout.ObjectField(
                                                                             GUIContent.none,
                                                                             csvImportProp.objectReferenceValue,
                                                                             typeof(TextAsset),
                                                                             false
                                                                            );

            if (GUILayout.Button("Import", GUILayout.Width(65)))
            {
                script.ImportCsv();
            }

            EditorGUILayout.EndHorizontal();
            // Redraws the rest of the SO fields, need to remember to exclude customised properties or  they will reappear
            DrawPropertiesExcluding(serializedObject, "m_Script", "dialogueList", "csvImport");

            serializedObject.ApplyModifiedProperties();
        }
    }
    #endif
}