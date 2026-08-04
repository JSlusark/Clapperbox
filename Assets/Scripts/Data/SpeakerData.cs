using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    [System.Serializable]
    
    public struct ExpressionSprite
    {
        public Expression type;
        public Sprite sprite;
    }
    
    [CreateAssetMenu(fileName = "SpeakerData", menuName = "Chatterbox/Speaker", order = 0)]
    
    public class SpeakerData : ScriptableObject
    {
        [Header("Identity")] 
        public string speakerName;
        public Color speakerColor;
        
        [Header("Assets")] // would be cool to add sound types by expressions
        public AudioClip sound;
        public List<ExpressionSprite> expressions;
        
        public Sprite GetExpression(Expression type)
        {
            var entry = expressions.Find(e => e.type == type);
            return entry.sprite;
        }

  
    }
}