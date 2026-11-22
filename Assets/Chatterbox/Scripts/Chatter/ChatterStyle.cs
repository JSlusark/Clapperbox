using System.Collections.Generic;

using UnityEngine;

/*
 * A ChatterStyle describes HOW a line is spoken: it turns the text into tokens.
 * It does not play, time or emit anything: the DialogueManager plays the tokens.
 */
public abstract class ChatterStyle
{
    // Minimum time between tokens
    protected virtual float Tempo => 0.05f;

    // Max time a blip may play. Per-letter styles (Monotone, Duotone, Animalese) share this short limit
    protected virtual float SoundLimit => 0.08f;

    // Default: one token per character
    public virtual IEnumerable<ChatterLine> GetLine(string text, AudioClip sound)
    {
        if (text == null) yield break;

        for (int i = 0; i < text.Length; i++)
            yield return CreateLine(GetPitch(i), 1, sound);
    }

    // Builds a token whose blip is cut at SoundLimit, and waits for the blip so blips never overlap
    //  holds how long each blip may play.
    protected ChatterLine CreateLine(float pitch, int chunk, AudioClip sound)
    {
        float soundLength = GetSoundLength(pitch, sound);
        return new ChatterLine(pitch, chunk, Mathf.Max(Tempo, soundLength), soundLength);
    }

    // How long the clip lasts at this pitch, capped at SoundLimit
    private float GetSoundLength(float pitch, AudioClip sound)
    {
        if (sound == null || pitch == 0f) return 0f;
        return Mathf.Min(sound.length / Mathf.Abs(pitch), SoundLimit);
    }

    protected virtual float GetPitch(int index)
    {
        return 1f;
    }
}

public class MonotoneStyle : ChatterStyle
{
}

public class DuotoneStyle : ChatterStyle
{
    // Same tokens as Monotone, but tone alternates
    protected override float GetPitch(int index)
    {
        return (index % 2 == 0) ? 1.1f : 1f;
    }
}

// public class AnimaleseStyle : ChatterStyle
// {
// }

public class FullWordStyle : ChatterStyle
{
    // One token per word, revealing the word and the space after it
    protected override float Tempo => 0.12f;
    protected override float SoundLimit => 0.25f;

    public override IEnumerable<ChatterLine> GetLine(string text, AudioClip sound)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        string[] words = text.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            bool isLast = i == words.Length - 1;
            yield return CreateLine(GetPitch(i), words[i].Length + (isLast ? 0 : 1), sound);
        }
    }
}

public class IntroStyle : ChatterStyle
{
    protected override float SoundLimit => 1f;

    // A single token that reveals the whole line
    public override IEnumerable<ChatterLine> GetLine(string text, AudioClip sound)
    {
        if (string.IsNullOrEmpty(text)) yield break;
        yield return CreateLine(GetPitch(0), text.Length, sound);
    }
}
