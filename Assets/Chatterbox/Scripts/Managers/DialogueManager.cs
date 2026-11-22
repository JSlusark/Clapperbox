using System;
using System.Collections;
using System.Collections.Generic;
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

    private Coroutine _lineRoutine;

    public static event Action<ChatterLine, AudioClip> OnPerformance;
    public static event Action OnLineSkipped;

    private int _line = 0;
    
    private void Start()
    {
        StartLine(_line++);
    }

    public void NextLine() 
    {
        if (_line > screenPlay.dialogueList.Count - 1) return;

        InterruptPreviousLine();
        StartLine(_line++);
    }

    private void InterruptPreviousLine() // Stops audio to not interfere on next line
    {
        if (_lineRoutine != null) StopCoroutine(_lineRoutine);
        _lineRoutine = null;
        OnLineSkipped?.Invoke();
    }
    
    private void StartLine(int i)
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
        var chatterStyle = ChatterFactory.CreateStyle(speakerData.chatterType);
        if (chatterStyle == null) return;

        _lineRoutine = StartCoroutine(PerformLine(chatterStyle.GetLine(line.text, speakerData.chatterSound), speakerData.chatterSound));
    }

    private IEnumerator PerformLine(IEnumerable<ChatterLine> line, AudioClip sound)
    {
        foreach (var unit in line)
        {
            OnPerformance?.Invoke(unit, sound);
            yield return new WaitForSeconds(unit.Tempo);
        }

        _lineRoutine = null;
    }

    private void LoadView(Line line, CharacterData speaker)
    {
        Debug.Log($"Speaker: {speaker.name}, {speaker.GetExpression(line.expression)} ");

        dialogueView.SetLine(line.text); // 
        avatarView.Show(speaker.GetExpression(line.expression)); // more a reaction view here
        nameView.Show(speaker.speakerName, speaker.speakerColor);
        bgView.Show(speaker.speakerColor);

    }
}