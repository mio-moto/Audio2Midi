using System.Runtime.InteropServices;
using System.Threading.Channels;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace Audio2Midi.Core
{
    public class MidiSender
    {
        public OutputDevice OutputDevice { get; private set; }

        public MidiSender(OutputDevice outputDevice)
        {
            OutputDevice = outputDevice;
            outputDevice.PrepareForEventsSending();
        }

        public static ICollection<InputDevice> ListMidiInputDevices() => InputDevice.GetAll();
        public static ICollection<OutputDevice> ListMidiOutputDevices() => OutputDevice.GetAll();
        public bool RecordCallback(int handle, IntPtr buffer, int length, IntPtr user)
        {
            // length is in bytes, 16-bit PCM = 2 bytes per sample
            int totalSamples = length / 2;
            short[] samples = new short[totalSamples];
            Marshal.Copy(buffer, samples, 0, totalSamples);

            var peaks = new float[24];
            int channelCount = peaks.Length;

            // Interleaved multi-channel: [ch0,ch1,...,chN-1, ch0,ch1,...]
            for (int i = 0; i < totalSamples; i += channelCount)
            {
                for (int ch = 0; ch < channelCount; ch++)
                {
                    float sample = samples[i + ch] / 32768f;
                    if (Math.Abs(sample) > peaks[ch])
                        peaks[ch] = Math.Max(peaks[ch], Math.Abs(sample));
                }
            }

            var trackPeaks = new float[8];
            for (var i = 0; i < 8; i++)
            {
                // first two channels are mix, then channels are left/right
                var peakChannelIndex = 2 + i * 2;
                var leftPeak = peaks[peakChannelIndex];
                var rightPeak = peaks[peakChannelIndex + 1];
                trackPeaks[i] = Math.Max(leftPeak, rightPeak);
            }

            for (var i = 0; i < trackPeaks.Length; i++)
            {
                var intValue = (int) (trackPeaks[i] * 127.0f);

                OutputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber) (i + 1), (SevenBitNumber) intValue) { Channel = (FourBitNumber) 10 });
            }

            return true; // return true to continue recording
        }
    }
}
