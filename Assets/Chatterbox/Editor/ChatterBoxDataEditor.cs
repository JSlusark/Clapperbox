using DefaultNamespace;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChatterBoxData))]
public class ChatterBoxDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ChatterBoxData chatterBoxData = (ChatterBoxData)target;
        if (GUILayout.Button("Import Characters"))
        {
            CharacterObjectsImport.ImportCharacters(chatterBoxData);
        }
    }
}