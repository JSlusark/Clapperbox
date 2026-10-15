using System.Collections.Generic;

using DefaultNamespace;

using UnityEditor;
using UnityEngine;

public static class CharacterObjectsImport
{
    public static List<string> Import(List<Line> speaker)
    {
        List<string> speakers = new List<string>();

        for (int i = 0; i < speaker.Count; i++)
        {
            speakers.Add(speaker[i].speaker);
        }

        return GetUniqueSpeakers(speakers);
    }
    
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

    private static Dictionary<string, CharacterData> FindAllCharacters()
    {
        Dictionary<string, CharacterData> charactersByName = new Dictionary<string, CharacterData>(); // Keys are unique in dictionary, makes it easier to avoid name duplicates

        foreach (string guid in AssetDatabase.FindAssets($"t:{nameof(CharacterData)}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (character == null || string.IsNullOrEmpty(character.speakerName)) continue;

            if (charactersByName.TryGetValue(character.speakerName, out CharacterData existing))
            {
                Debug.LogWarning($"[Chatterbox] Duplicate speakerName '{character.speakerName}' at '{path}' " +
                                 $"and '{AssetDatabase.GetAssetPath(existing)}'. Using the first one.");
                continue;
            }

            charactersByName.Add(character.speakerName, character);
        }

        return charactersByName;
    }

    // Rebuilds the screenplay's cast from the speakers in its dialogue list.
    // Does not save: the caller is responsible for SetDirty/SaveAssets.
    public static void ImportCharacters(ScreenPlayData screenPlay)
    {
        if (screenPlay == null)
        {
            Debug.LogWarning("[Chatterbox] No ScreenPlayData to import characters into.");
            return;
        }

        List<string> uniqueSpeakers = Import(screenPlay.dialogueList);
        Dictionary<string, CharacterData> charactersByName = FindAllCharacters();
        List<CharacterData> importedCharacters = new List<CharacterData>();

        foreach (string speaker in uniqueSpeakers)
        {
            if (!charactersByName.TryGetValue(speaker, out CharacterData character))
            {
                Debug.LogWarning($"[Chatterbox] No CharacterData with speakerName '{speaker}' found in the project.");
                continue;
            }

            importedCharacters.Add(character);
        }

        screenPlay.characters = importedCharacters;
        Debug.Log($"[Chatterbox] Imported {importedCharacters.Count}/{uniqueSpeakers.Count} character(s) into {screenPlay.name}.");
    }
}
