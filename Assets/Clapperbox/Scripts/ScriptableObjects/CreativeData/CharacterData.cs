using System.Collections.Generic;
using Clapperbox.Runtime.Scripts.Voice;
using UnityEngine;
using UnityEngine.Serialization;


namespace DefaultNamespace
{
    // An expression is the name of its sprite file: "Angry-more.png" is the expression "Angry-more"
    [System.Serializable]
    public struct ExpressionSprite
    {
        [FormerlySerializedAs("type")] public string name;
        public Sprite sprite;
    }

    /*
     * Uses editor script "AutomaticFileNameCreator" to create automatically renamed Character_data SO.
     * The SO takes the name of the folder in which teh SO is created.
     * Example:
     * - You are in "Assets/Clapperbox/Folder_test/Characters/Rial"
     * - Right click > Create > Clapperbox > CreativeData > Character Data
     * - Creates SO with title "Rial_data"
     */
    public class CharacterData : ScriptableObject
    {
        [Header("Info")] public string speakerName;
        public Color speakerColor;

        [Header("Expressions")] // would be cool to add voiceClip types by expressions
        public List<ExpressionSprite> expressions;

        // Case doesn't matter: "happy" in a csv finds the "Happy" sprite. Null when the character has no such expression
        public Sprite GetExpression(string expressionName)
        {
            if (expressions == null || string.IsNullOrEmpty(expressionName)) return null;
            foreach (var entry in expressions)
                if (string.Equals(entry.name, expressionName, System.StringComparison.OrdinalIgnoreCase))
                    return entry.sprite;
            return null;
        }

        [Header("Voice")] // would be cool to add voiceClip types by expressions
        [FormerlySerializedAs("chatterSound")] public AudioClip voiceClip;

        [FormerlySerializedAs("chatterType")] public VoiceType voiceType; // standard will be on box style while overwrite from character if needed?
    }
}