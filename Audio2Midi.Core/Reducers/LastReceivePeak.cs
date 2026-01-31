namespace Audio2Midi.Core.Reducers;

public class LastReceivePeak : IReducers
{
    public void OnTrackData(TrackData trackData)
    {
        CurrentValue = trackData.PcmData.Max();
    }

    public float CurrentValue { get; private set; } = 0.0f;
}
