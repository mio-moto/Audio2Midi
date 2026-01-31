namespace Audio2Midi.Core.Sources;

/**
 * Multi-Broadcast source, sending out all its track data into many receivers
 */
public interface ISource
{
    public Action<ChannelData[]>? OnTrackData { get; set; }
}
