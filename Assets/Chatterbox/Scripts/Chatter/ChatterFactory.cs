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
    public static ChatterStyle CreateStyle(ChatterType type)
    {
        switch (type)
        {
            case ChatterType.Monotone:
                return new MonotoneStyle(); // todo: send params to constructor 

            case ChatterType.Duotone:
                return new DuotoneStyle();

            case ChatterType.FullWord:
                return new FullWordStyle();

            case ChatterType.Intro:
                return new IntroStyle();

            // More add other types once I make sure of the structure to achieve

            default:
                Debug.LogError($"[ChatterFactory] Unhandled ChatterType: {type}");
                return null;
        }
    }
}
