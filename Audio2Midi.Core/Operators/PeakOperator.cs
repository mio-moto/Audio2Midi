namespace Audio2Midi.Core;

public enum PeakOperation
{
    Error = 0,
    Linear = 1,
    Logarithmic = 2
}

public record Track(int[] Channels, string? Name);

public class PeakOperator : IOperator
{
    public Track Track { get; private set; }
    public PeakOperation PeakOperation { get; private set; }
    public Action<TrackData>? OnTrackData { get; set; } = null;

    public PeakOperator(Track track, PeakOperation peakOperation)
    {
        Track = track.Name != null ? track : track with { Name = $"Channels=[{String.Join(',', track.Channels)}]" }; 
        PeakOperation = peakOperation;
    }

    public void OnChannelData(ChannelData[] channelData)
    {
        if (channelData.Length <= 0)
        {
            return;
        }
        
        var sampleLength = channelData[0].PcmData.Length;
        var trackPeaks = new float[sampleLength];
        
        // go through all PCM data
        for (var index = 0; index < Track.Channels.Length; index++)
        {
            foreach (int mapping in Track.Channels)
            {
                for (var sample = 0; sample < channelData[mapping].PcmData.Length; sample++)
                {
                    var value = Math.Abs(channelData[mapping].PcmData[sample]);
                    if (PeakOperation == PeakOperation.Logarithmic)
                    {
                        value = (float) Math.Sqrt(value);
                    }
                    trackPeaks[sample] = Math.Max(trackPeaks[sample], value);
                }
            }
            
        } 
        OnTrackData?.Invoke(new TrackData(trackPeaks, Track.Channels, channelData[0].SampleRate));
    }
}
