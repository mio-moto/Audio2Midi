namespace Audio2Midi.Core;


public record TrackData(float[] PcmData, int[] Channels, int SampleRate);
public record ChannelData(float[] PcmData, int Channel, int SampleRate);
