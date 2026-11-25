public readonly struct VoiceUnit
{
    public readonly float Pitch;
    public readonly int Chunk;
    public readonly float Tempo;
    public readonly float SoundLength; // seconds the blip may play before it is cut

    public VoiceUnit(float pitch, int chunk, float tempo, float soundLength)
    {
        Chunk = chunk; // chunk of text revealed by this line
        Pitch = pitch;
        Tempo = tempo;
        SoundLength = soundLength; // sound limit for attached audio clip, in seconds
    }
}
