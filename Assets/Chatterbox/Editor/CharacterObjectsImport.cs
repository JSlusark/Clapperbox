using System.Collections.Generic;

using DefaultNamespace;

using UnityEditor;
using UnityEngine;

public static class CharacterObjectsImport
{
    public static List<string> GetUniqueSpeakers(List<string> speaker)
    {
        List<string> uniqueSpeakers = new List<string>();

        for (int i = 0; i < speaker.Count; i++)
        {
            string first = speaker[i];
            bool toAdd = true;
            for (int y = i + 1; y < speaker.Count; y++)
            {
                string second = speaker[y];
                if (first == second)
                {
                    toAdd = false;
                    break;
                }
            }

            if (toAdd)
                uniqueSpeakers.Add(speaker[i]);
        }
        return uniqueSpeakers;
    }

    public static List<string> Import(List<Line> speaker)
    {
        List<string> speakers = new List<string>();

        for (int i = 0; i < speaker.Count; i++)
        {
            speakers.Add(speaker[i].speaker);
        }

        return GetUniqueSpeakers(speakers);
    }

    public static void ImportCharacters(ChatterBoxData chatterBoxData)
    {
        if (chatterBoxData == null || chatterBoxData.screenPlay == null)
        {
            Debug.LogWarning("[Chatterbox] Assign a ScreenPlayData asset before importing characters.");
            return;
        }

        List<string> uniqueSpeakers = Import(chatterBoxData.screenPlay.dialogueList);
        List<CharacterData> importedCharacters = new List<CharacterData>();
        const string charactersFolder = "Assets/Chatterbox/Prefab/Characters";

        foreach (string speaker in uniqueSpeakers)
        {
            string assetPath = $"{charactersFolder}/{speaker}/{speaker}_data.asset";
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);

            if (character == null)
            {
                Debug.LogWarning($"[Chatterbox] No CharacterData found for '{speaker}' at '{assetPath}'.");
                continue;
            }

            importedCharacters.Add(character);
        }

        Undo.RecordObject(chatterBoxData, "Import Characters");
        chatterBoxData.characters = importedCharacters;
        EditorUtility.SetDirty(chatterBoxData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Chatterbox] Imported {importedCharacters.Count} character(s) into {chatterBoxData.name}.");
    }
}