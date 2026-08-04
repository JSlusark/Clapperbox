using UnityEngine;
using System.Collections.Generic;

namespace DefaultNamespace
{
    
    [CreateAssetMenu(fileName = "Data", menuName = "Chatterbox/Data", order = 0)]
    public class Data : ScriptableObject
    {
        public SpeakerData speakerData;
        public DialogueData dialogueList;
    }
}