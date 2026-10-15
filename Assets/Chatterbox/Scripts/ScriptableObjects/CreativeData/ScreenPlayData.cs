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
    [System.Serializable]
    public class Line
    {
        [TextArea(1, 1)] public string speaker;
        [TextArea(1, 6)] public string text;
        public Expression expression = Expression.Neutral;
        [TextArea(1, 1)] public string invalidExpression;
    }

    [CreateAssetMenu(fileName = "ScreenPlay", menuName = "ChatterBox/CreativeData/Screen Play", order = 0)]
    public class ScreenPlayData : ScriptableObject
    {
        // [Header("Line details:")]
        public List<Line> dialogueList = new List<Line>();

        // [Header("Characters:")] [Tooltip("Resolved automatically from the speakers in the csv on import.")]
        public List<CharacterData> characters = new List<CharacterData>();

        public CharacterData GetCharacter(string speakerName)
        {
            return characters.Find(character => character != null && character.speakerName == speakerName);
        }

        // [Header("Import From Csv:")] [Tooltip("Drag your .csv file here, then click the button below.")]
        public TextAsset csvImport;
    }
}