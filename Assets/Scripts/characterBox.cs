using TMPro;
using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "characterBox", menuName = "Dialogues", order = 0)]
    public class characterBox : ScriptableObject
    {
        [field: Header("Character Info")]
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public Sprite Portrait { get; private set; }
        [field: Header("Dialogue")]
        [field: TextArea(3, 6)]                                                     // Gives textbox in the inspector
        [field: SerializeField] public string[] Dialogue { get; private set; } // Changed from TextMeshProUGUI
        
    }
}