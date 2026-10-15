using System.Collections;
using Chatterbox.Runtime.Scripts.Generics;
using DefaultNamespace;
using UnityEngine;

public class DialogueManager : Singleton<DialogueManager>
{
    [SerializeField] private ScreenPlayData screenPlay;

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
        if (_line > screenPlay.dialogueList.Count - 1) return;
        PerformLine(_line++);
    }
    
    private void PerformLine(int i)
    {
        var line = screenPlay.dialogueList[i];
        var speakerData = screenPlay.GetCharacter(line.speaker);
        if (speakerData == null)
        {
            Debug.LogError($"[Chatterbox] No character '{line.speaker}' in {screenPlay.name}. Re-import the csv or check the speakerName.");
            return;
        }

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