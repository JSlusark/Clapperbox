using System.Collections.Generic;
using Chatterbox.Runtime.Scripts.Chatter;
using UnityEngine;


namespace DefaultNamespace
{
    [System.Serializable]
    public struct ExpressionSprite
    {
        public Expression type;
        public Sprite sprite;
    }

    /*
     * Uses editor script "AutomaticFileNameCreator" to create automatically renamed Character_data SO.
     * The SO takes the name of the folder in which teh SO is created.
     * Example:
     * - You are in "Assets/Chatterbox/Folder_test/Characters/Rial"
     * - Right click > Create/ChatterBox/Character Data
     * - Creates SO with title "Rial_data"
     */
    public class CharacterData : ScriptableObject
    {
        [Header("Info")] public string speakerName;
        public Color speakerColor;

        [Header("Expressions")] // would be cool to add chatterSound types by expressions
        public List<ExpressionSprite> expressions;

        public Sprite GetExpression(Expression type)
        {
            var entry = expressions.Find(e => e.type == type);
            return entry.sprite;
        }

        [Header("Voice")] // would be cool to add chatterSound types by expressions
        public AudioClip chatterSound;

        public ChatterType chatterType; // standard will be on box style while overwrite from character if needed?
    }
}