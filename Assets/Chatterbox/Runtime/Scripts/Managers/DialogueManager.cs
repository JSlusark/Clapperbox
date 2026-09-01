using System.Collections;
using Chatterbox.Runtime.Scripts.Generics;
using DefaultNamespace;
using UnityEngine;

public class DialogueManager : Singleton<DialogueManager>
{
    [SerializeField] private ChatterBoxData chatterBoxData;

    [SerializeField] private NameView nameView;
    [SerializeField] private DialogueView dialogueView;
    [SerializeField] private AvatarView avatarView;
    [SerializeField] private AvatarBgView bgView;

    public ChatterBehaviour ChatterBehaviour;

    private int _line = 0;
    
    private void Start()
    {
        PerformLine(_line);
    }

    public void NextLine() 
    {
        // todo: if called, ensure to stop any hanging audio
        if (_line >= chatterBoxData.screenPlay.dialogueList.Count - 1) return;
        PerformLine(++_line);
    }
    
    private void PerformLine(int line)
    {
        var dialogue = chatterBoxData.screenPlay.dialogueList[line];
        var speaker = chatterBoxData.characterData;
        dialogueView.SetLine(dialogue.text);
        
        avatarView.Show(speaker.GetExpression(dialogue.expression)); // more a reaction view here
        
        // might change this depending if I want to have the narration manager
        nameView.Show(speaker.speakerName, speaker.speakerColor);
        bgView.Show(speaker.speakerColor);

        // Sets the chatter (the way the text, audio and creatives are animated )
        ChatterBehaviour = ChatterFactory.CreateBehaviour(speaker.chatterType, speaker.chatterSound);

        if (ChatterBehaviour != null)
            ChatterBehaviour.Execute(dialogue.text, speaker);
    }
    
    public static Coroutine RunCoroutine(IEnumerator routine)
    {
        return Instance.StartCoroutine(routine);
    }
}