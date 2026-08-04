using DefaultNamespace;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private Data data;
    
    [SerializeField] private NameView nameView;
    [SerializeField] private DialogueView dialogueView;
    [SerializeField] private AvatarView avatarView;
    [SerializeField] private AvatarBgView bgView;

    private int _line = 0;

    private void Start()
    {
        ShowLine(_line);
    }

    public void NextLine()
    {
        if (_line >= data.dialogueList.dialogue.Count - 1) return;
        ShowLine(++_line);
    }
    
    private void ShowLine(int line)
    {
        
        var dialogue = data.dialogueList.dialogue[line];
        var speaker = data.speakerData;
        
        nameView.Show(speaker.speakerName, speaker.speakerColor);
        avatarView.Show(speaker.GetExpression(dialogue.expression));
        dialogueView.Show(dialogue.text, speaker.sound);
        bgView.Show(speaker.speakerColor);
    }

}