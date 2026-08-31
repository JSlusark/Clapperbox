using UnityEngine;
using System.Collections.Generic;

namespace DefaultNamespace
{
    // TODO: Need to chnage naming and organization as finalised concept was still to be "figured out"
    [CreateAssetMenu(fileName = "Data", menuName = "ChatterBox/ChatterBox", order = 0)]
    public class Data : ScriptableObject
    {
        public CharacterData characterData;
        public DialogueData dialogueList;
    }
}