using System.Collections.Generic;
using Chatterbox.Runtime.Scripts.Chatter;
using UnityEngine;

// namespace DefaultNamespace
// {
    [System.Serializable]
    
    public struct ExpressionSprite // move to class
    {
        public DialogueEmotion type;
        public Sprite sprite;
    }
    

    [CreateAssetMenu(fileName = "SpeakerData", menuName = "Chatterbox/Speaker", order = 0)]
    
    public class SpeakerData : ScriptableObject
    {
        [Header("Identity")] 
        public string speakerName;
        public Color speakerColor;
        
        [Header("Sound")] // would be cool to add chatterSound types by expressions
        public AudioClip chatterSound;
        public ChatterType chatterType; // based on chatterstyle enum we start use a specific class

        [Header("Sprite")] // would be cool to add chatterSound types by expressions
        public List<ExpressionSprite> expressions;
        public AvatarAnimationStyle AvatarAnimationStyle;
        
        public Sprite GetExpression(DialogueEmotion type)
        {
            var entry = expressions.Find(e => e.type == type);
            return entry.sprite;
        }

  
    }