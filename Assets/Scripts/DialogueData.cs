using TMPro;
using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogues/DialogueData", order = 0)]
    public class DialogueData : ScriptableObject
    {
        [field: Header("Character Info")]
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public Sprite Portrait { get; private set; }
        [field: SerializeField] public AudioClip Sound { get; private set; }
        
        [field: Header("Dialogue Content")]
        [field: TextArea(3, 6)]                                                     // Gives textbox in the inspector
        [field: SerializeField] public string[] Dialogue { get; private set; } // Changed from TextMeshProUGUI
        
    }
}