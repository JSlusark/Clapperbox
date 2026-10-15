using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DefaultNamespace
{
#if UNITY_EDITOR
    [CustomEditor(typeof(ScreenPlayData))]
    [CanEditMultipleObjects]
    public class ScreenPlayDataEditor : Editor
    {
        private SerializedProperty _dialogueProp;
        private SerializedProperty _csvImportProp;
        private SerializedProperty _charactersProp;

        private Color defaultColor;
        private Color errorColor;
        private string alert;

        private void OnEnable()
        {
            _dialogueProp = serializedObject.FindProperty("dialogueList");
            _csvImportProp = serializedObject.FindProperty("csvImport");
            _charactersProp = serializedObject.FindProperty("characters");
            defaultColor = GUI.backgroundColor;
            errorColor = new Color(1f, 0f, 0f);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawCsvImport();
            DrawCharacters();
            DrawCustomList();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCsvImport()
        {
            ScreenPlayData script = (ScreenPlayData)target;
            if (_csvImportProp != null)
            {
                EditorGUILayout.LabelField(_csvImportProp.displayName, EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();

                _csvImportProp.objectReferenceValue = EditorGUILayout.ObjectField(
                     GUIContent.none,
                     _csvImportProp.objectReferenceValue,
                     typeof(TextAsset),
                     false
                    );

                if (GUILayout.Button("Import", GUILayout.Width(65)))
                    ScreenPlayCsvImporter.Import(script, (TextAsset)_csvImportProp.objectReferenceValue);

                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                    ScreenPlayCsvImporter.Clear(script);

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawCharacters()
        {
            if (_charactersProp == null) return;
            using (new EditorGUI.DisabledScope(true) /* disables character list and is deactivated/disposed at end of using block*/)
                EditorGUILayout.PropertyField(_charactersProp, true); // draws the characters list
            /* 
            - "EditorGUI.BeginDisabledGroup(true)" is what can make an inspector field read-only, but it does not have a disposable scope and disables everything after itself. So DisabledScope is used as an alternative.
            - https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/using
            - IDisposable is an interface, a contract that says "this type (EditorGUI.DisabledScope) has a Dispose method". Dispose is where a type releases resources the garbage collector can't clean up on its own (file handles, network connections, unmanaged memory, etc.); it must be called explicitly, or automatically at the end of a using block. In DisabledScope's case it releases nothing: it just calls EndDisabledGroup().
                */
        }

        private void DrawCustomList()
        {
            if (_dialogueProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                _dialogueProp.isExpanded =
                    EditorGUILayout.Foldout(_dialogueProp.isExpanded, _dialogueProp.displayName, true);
                ChangeUI(labelWidth: 70);
                _dialogueProp.arraySize =
                    EditorGUILayout.DelayedIntField("Lines found:", _dialogueProp.arraySize, GUILayout.Width(110));

                EditorGUILayout.EndHorizontal();

                if (_dialogueProp.isExpanded)
                    CustomizeEntryView(_dialogueProp);
                // DrawPropertiesExcluding(serializedObject, "m_Script", "dialogueList", "csvImport");
            }
        }

        private void CustomizeEntryView(SerializedProperty dialogueProp)
        {
            EditorGUI.indentLevel++;

            for (int i = 0; i < dialogueProp.arraySize; i++)
            {
                SerializedProperty elementProp = dialogueProp.GetArrayElementAtIndex(i);
                SerializedProperty expressionProp = elementProp.FindPropertyRelative("expression");
                SerializedProperty unrecognisedProp = elementProp.FindPropertyRelative("invalidExpression");


                bool isUnrecognised = false;
                if (expressionProp != null && expressionProp.enumValueIndex >= 0 &&
                    expressionProp.enumValueIndex < expressionProp.enumNames.Length)
                {
                    if (expressionProp.enumNames[expressionProp.enumValueIndex] == "Unrecognised")
                    {
                        isUnrecognised = true;
                        alert = " ⚠️";
                    }
                    else
                        alert = "";
                }

                // Foldout header
                EditorGUILayout.PropertyField(elementProp, new GUIContent($"Line {i}{alert}"), false);
                if (elementProp.isExpanded) // children
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.BeginVertical();

                    SerializedProperty speakerProp = elementProp.FindPropertyRelative("speaker");
                    if (speakerProp != null) EditorGUILayout.PropertyField(speakerProp);

                    SerializedProperty textProp = elementProp.FindPropertyRelative("text");
                    if (textProp != null) EditorGUILayout.PropertyField(textProp);

                    float originalLabelWidth = EditorGUIUtility.labelWidth;
                    if (!isUnrecognised)
                    {
                        ChangeUI(textColor: defaultColor, labelWidth: 110f);
                        EditorGUILayout.PropertyField(expressionProp);
                    }

                    if (unrecognisedProp != null && unrecognisedProp.stringValue != "None")
                    {
                        ChangeUI(textColor: errorColor, labelWidth: 135f);

                        unrecognisedProp.stringValue =
                            EditorGUILayout.TextField(unrecognisedProp.displayName, unrecognisedProp.stringValue);

                        ChangeUI(labelWidth: originalLabelWidth, textColor: defaultColor, bgColor: defaultColor);
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
        }

        private void
            ChangeUI(float? labelWidth = null, Color? textColor = null,
                     Color? bgColor =
                         null) // nullable value types, i change all values or some with func(valuetochange:value)
        {
            if (labelWidth != null) EditorGUIUtility.labelWidth = labelWidth.Value;
            if (textColor != null) GUI.color = textColor.Value;
            if (bgColor != null) GUI.backgroundColor = bgColor.Value;
        }
    }
#endif
}