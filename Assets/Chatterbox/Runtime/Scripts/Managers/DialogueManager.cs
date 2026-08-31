using System.Collections;
using Chatterbox.Runtime.Scripts.Generics;
using DefaultNamespace;
using UnityEngine;

public class DialogueManager : Singleton<DialogueManager>
{
    [SerializeField] private Data data;

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
        if (_line >= data.dialogueList.dialogueList.Count - 1) return;
        PerformLine(++_line);
    }
    
    private void PerformLine(int line)
    {
        var dialogue = data.dialogueList.dialogueList[line];
        var speaker = data.characterData;
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

    /*
     * Intially was thinking for the dialogue manager to represent each character and
     * hide when said character does not have to speak (keeping pointer to convo), since
     * I was envisioning each dialogue/character as its own prefab for a fun game concept
     * i was having involving having to interact with different dialogues
     *
     * However it would be cool to have the dialogue manager actually managing all dialogues
     * and could use the function below and call it from a potential "narration manager"
     * 
     */
    // public void SwitchSpeaker()
    // {
    // }

    public static Coroutine RunCoroutine(IEnumerator routine)
    {
        return Instance.StartCoroutine(routine);
    }
}