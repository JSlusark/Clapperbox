using UnityEngine;
using System.Collections.Generic;

namespace DefaultNamespace
{
    // TODO: Need to chnage naming and organization as finalised concept was still to be "figured out"
    [CreateAssetMenu(fileName = "Data", menuName = "Chatterbox/Data", order = 0)]
    public class Data : ScriptableObject
    {
        public SpeakerData speakerData;
        public DialogueData dialogueList;
    }
}