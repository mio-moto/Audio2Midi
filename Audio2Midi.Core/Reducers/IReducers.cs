namespace Audio2Midi.Core.Reducers;

/**
 * Reducers condense a track data down to singular values to be polled (maybe?) by receives.
 */
public interface IReducers
{
    public void OnTrackData(TrackData trackData);
    public float CurrentValue { get; }
}
