using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    [System.Serializable]
    public class Dialogue
    {
        [TextArea(3, 6)] 
        public string text;
        public Expression expression = Expression.Neutral;

    }
    
    [CreateAssetMenu(fileName = "Dialogue", menuName = "Chatterbox/Dialogue", order = 0)]
    public class DialogueData : ScriptableObject
    {
        public List<Dialogue> dialogue = new List<Dialogue>();
    }
}