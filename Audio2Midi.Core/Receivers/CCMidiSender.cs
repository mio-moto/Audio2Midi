using Audio2Midi.Core.Reducers;
using Audio2Midi.Core.Utilities;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace Audio2Midi.Core.Receivers;

public class CCMidiSender : IReceiver
{
    public OutputDevice OutputDevice { get; private set; }
    public IReducers Reducer { get; }
    public int Channel { get; private set; }
    // ReSharper disable once InconsistentNaming
    public int CC { get; private set; }
    public ClampRange RemapClampRange { get; private set; }

    
    
    public CCMidiSender(OutputDevice outputDevice, IReducers reducer, int channel, int cc, ClampRange? remapRange)
    {
        OutputDevice = outputDevice;
        Reducer = reducer;
        Channel = channel;
        CC = cc;
        RemapClampRange = remapRange ?? new ClampRange(0f, 127f);
    }

    public async void Run(TimeSpan dutyCycle)
    {
        while (true)
        {
            var peak = Reducer.CurrentValue;
            short value = (short) RemapClampRange.Remap(peak);
            OutputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)CC, (SevenBitNumber) value) { Channel = (FourBitNumber) Channel});
            Thread.Sleep(dutyCycle);
        }
    }
    
    public void OnTrackData(TrackData track)
    {
        var peak = track.PcmData.Max();
        short value = (short) RemapClampRange.Remap(peak * 128);
        OutputDevice.SendEvent(new ControlChangeEvent((SevenBitNumber)CC, (SevenBitNumber) value) { Channel = (FourBitNumber) Channel});
    }

}
