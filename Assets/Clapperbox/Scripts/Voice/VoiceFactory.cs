using Clapperbox.Runtime.Scripts.Voice;
using UnityEngine;

/*
 * Idea: VoiceFactory creates voice styles.
 * A voice style is responsible for how a line is spoken.
 * Voice data are all those elements that make the dialogue data feel "alive" (talking style,
 * movement style etc).
 */

// namespace Clapperbox.Runtime.Scripts.VoiceTypes
// {
public class VoiceFactory
{
    public static VoiceStyle CreateStyle(VoiceType type)
    {
        switch (type)
        {
            case VoiceType.Monotone:
                return new MonotoneStyle(); // todo: send params to constructor 

            case VoiceType.Duotone:
                return new DuotoneStyle();

            case VoiceType.FullWord:
                return new FullWordStyle();

            case VoiceType.Intro:
                return new IntroStyle();

            // More add other types once I make sure of the structure to achieve

            default:
                Debug.LogError($"[VoiceFactory] Unhandled VoiceType: {type}");
                return null;
        }
    }
}
