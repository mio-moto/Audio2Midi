using System.Runtime.InteropServices;

namespace Audio2Midi.Core.Sources;

// currently only does sint 24bit, I think
public class CaptureDevice(int channels, int sampleRate) : ISource
{
    public int Channels { get; private set; } = channels;
    public int SampleRate { get; private set; } = sampleRate;
    public Action<ChannelData[]>? OnTrackData { get; set; }

    public bool RecordCallback(int handle, IntPtr buffer, int length, IntPtr user)
    {
        // length is in bytes, 16-bit PCM = 2 bytes per sample
        int totalSamples = length / 2;
        short[] samples = new short[totalSamples];
        Marshal.Copy(buffer, samples, 0, totalSamples);
        var channelLength = totalSamples / Channels;
        float[][] channelData = new float[Channels][];
        for (var i = 0; i < Channels; i++)
        {
            channelData[i] = new float[channelLength];
        }
        
        // Interleaved multi-channel: [ch0,ch1,...,chN-1, ch0,ch1,...]
        for (int i = 0; i < totalSamples; i += Channels)
        {
            for (int ch = 0; ch < Channels; ch++)
            {
                float sample = samples[i + ch] / 32768f;
                channelData[ch][i / Channels] = sample;
            }
        }

        var channelSendData = new ChannelData[Channels];
        for (var i = 0; i < Channels; i++)
        {
            channelSendData[i] = new ChannelData(channelData[i], i, SampleRate);
        }
        
        OnTrackData?.Invoke(channelSendData);

        return true; // return true to continue recording
    }
    
}
