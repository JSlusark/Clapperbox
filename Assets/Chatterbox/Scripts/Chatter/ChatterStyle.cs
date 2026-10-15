using System.Collections.Generic;

/*
 * A ChatterStyle describes HOW a line is spoken: it turns the text into tokens.
 * It does not play, time or emit anything: the DialogueManager plays the tokens.
 */
public abstract class ChatterStyle
{
    protected float Tempo = 0.02f;

    // Default: one token per character
    public virtual IEnumerable<ChatterLine> GetLine(string text)
    {
        if (text == null) yield break;

        for (int i = 0; i < text.Length; i++)
            yield return new ChatterLine(GetPitch(i), 1, Tempo);
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
    public override IEnumerable<ChatterLine> GetLine(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        string[] words = text.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            bool isLast = i == words.Length - 1;
            yield return new ChatterLine(GetPitch(i), words[i].Length + (isLast ? 0 : 1), Tempo);
        }
    }
}

public class IntroStyle : ChatterStyle
{
    // A single token that reveals the whole line
    public override IEnumerable<ChatterLine> GetLine(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;
        yield return new ChatterLine(GetPitch(0), text.Length, Tempo);
    }
}
