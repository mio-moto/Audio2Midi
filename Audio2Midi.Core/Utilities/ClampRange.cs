namespace Audio2Midi.Core.Utilities;

public record ClampRange(float Start, float End)
{
    public float Remap(float value)
    {
        const float start1 = 0.0f;
        const float stop1 = 1.0f;
        float start2 = Start;
        float stop2 = End;
        float outgoing = start2 + (stop2 - start2) * ((value - start1) / (stop1 - start1));
        return Math.Clamp(outgoing, Start, End);
    }
}