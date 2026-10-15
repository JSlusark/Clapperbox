public readonly struct ChatterLine
{
    public readonly float Pitch;
    public readonly int Chunk;
    public readonly float Tempo;

    public ChatterLine(float pitch, int chunk, float tempo)
    {
        Chunk = chunk; // chunk of text revealed by this line
        Pitch = pitch;
        Tempo = tempo;
    }
}
