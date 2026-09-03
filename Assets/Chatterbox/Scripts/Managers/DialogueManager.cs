using System.Collections;
using System.Linq;

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
        PerformLine(_line++);
    }

    public void NextLine() 
    {
        // todo: if called, ensure to stop any hanging audio
        if (_line > chatterBoxData.screenPlay.dialogueList.Count - 1) return;
        PerformLine(_line++);
    }
    
    private void PerformLine(int i)
    {
        var line = chatterBoxData.screenPlay.dialogueList[i];
        var speakerData =  chatterBoxData.characters.FirstOrDefault(character => character.speakerName == line.speaker);

       LoadView(line, speakerData);

        // Sets the chatter (the way the text, audio and creatives are animated )
        ChatterBehaviour = ChatterFactory.CreateBehaviour(speakerData.chatterType, speakerData.chatterSound);

        if (ChatterBehaviour != null)
            ChatterBehaviour.Execute(line.text, speakerData);
    }

    private void LoadView(Line line, CharacterData speaker)
    {
        Debug.Log($"Speaker: {speaker.name}, {speaker.GetExpression(line.expression)} ");

        dialogueView.SetLine(line.text); // 
        avatarView.Show(speaker.GetExpression(line.expression)); // more a reaction view here
        nameView.Show(speaker.speakerName, speaker.speakerColor);
        bgView.Show(speaker.speakerColor);
        // lineview.Show
        
    }
    
    public static Coroutine RunCoroutine(IEnumerator routine)
    {
        return Instance.StartCoroutine(routine);
    }
}