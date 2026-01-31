namespace Audio2Midi.Core;

/**
 * Multi-Track receiver, typically channel operators calculating discrete values in any shape or form
 */
public interface IOperator
{
    public Action<TrackData>? OnTrackData { get; set; }
    public void OnChannelData(ChannelData[] channelData);
}
