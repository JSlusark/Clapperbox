using UnityEngine;

using System.Collections.Generic;

namespace DefaultNamespace
{
    // TODO: Need to chnage naming and organization as finalised concept was still to be "figured out"
    [CreateAssetMenu(fileName = "ChatterBoxData", menuName = "ChatterBox/ChatterBox Data", order = 0)]
    public class ChatterBoxData : ScriptableObject
    {
        public List<CharacterData> characters = new List<CharacterData>();
        


        // public CharacterData characterData;
        public ScreenPlayData screenPlay;
        public BoxUiData boxUiData; // ui data (boxdata like size, alignment and file; avatarUIData like file, style and alignment, pointer etc)
    }
}