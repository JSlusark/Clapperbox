using System.Collections.Generic;
using Chatterbox.Runtime.Scripts.Chatter;
using UnityEngine;

/*
 * Idea: Chatterfactory creates Chatters.
 * A chatter is responsible of ChatterData behaviour.
 * ChatterData are all those elements that make the dialogue data feel "alive" (talking style,
 * movement style etc).
 */

// namespace Chatterbox.Runtime.Scripts.ChatterTypes
// {
public class ChatterFactory
{
    public static ChatterBehaviour CreateBehaviour(ChatterType type, AudioClip sound)
    {
        ChatterBehaviour newChatterBehaviour = null;

        switch (type)
        {
            case ChatterType.Monotone:
                newChatterBehaviour = new MonotoneSpeech(); // todo: send params to constructor 
                break;

            case ChatterType.Duotone:
                newChatterBehaviour = new DuotoneSpeech();
                break;

            case ChatterType.FullWord:
                newChatterBehaviour = new FullWordSpeech();
                break;

            case ChatterType.Intro:
                newChatterBehaviour = new IntroSpeech();
                break;

            // More add other types once I make sure of the structure to achieve

            default:
                Debug.LogError($"[ChatterFactory] Unhandled ChatterType: {type}");
                return null;
        }

        // 2. Concrete Configuration: The factory sets up the instance parameters
        newChatterBehaviour.SetSound(sound);

        return newChatterBehaviour;
    }
}
