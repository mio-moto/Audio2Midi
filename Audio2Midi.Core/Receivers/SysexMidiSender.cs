using System.Globalization;
using Audio2Midi.Core.Reducers;
using Audio2Midi.Core.Utilities;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace Audio2Midi.Core.Receivers;

public class SysexMidiSender : IReceiver
{
    public OutputDevice OutputDevice { get; set; }
    public IReducers Reducer { get; }
    public string SysexTemplate { get; private set; }
    public ClampRange RemapClampRange { get; private set; }
    private Func<int, byte[]> OnValue { get; set; }
    

    public SysexMidiSender(OutputDevice outputDevice, IReducers reducer, string sysexTemplate, ClampRange? remapRange)
    {
        OutputDevice = outputDevice;
        Reducer = reducer;
        SysexTemplate = sysexTemplate;
        RemapClampRange = remapRange ?? new ClampRange(0f, 127f);
        OnValue = ParseFunctor(sysexTemplate);
    }

    public static Func<int, byte[]> ParseFunctor(string sysexTemplate)
    {
        // pretty simple for now: if it's a hex value, insert the hex / number value, then use that, otherwise insert the value
        var convertFunctors = sysexTemplate.Split(" ").Select<string,Func<int, byte>>(x =>
        {
            if (!byte.TryParse(x.Replace("0x", ""), NumberStyles.AllowHexSpecifier | NumberStyles.HexNumber, null, out var i))
            {
                if (x != "{x}")
                {
                    throw new ArgumentException($"Invalid sysex template '{sysexTemplate}', value inserts are notated as {{x}}");
                }
                return (value) => (byte) value;
            }
            return (value) => (byte) i;
        });


        return (value) => convertFunctors.Select(func => func(value)).ToArray();
    }

    public async void Run(TimeSpan dutyCycle)
    {
        while (true)
        {
            var peak = Reducer.CurrentValue;
            int value = (int) RemapClampRange.Remap(peak);
            var messageBytes = OnValue(value);
            OutputDevice.SendEvent(new NormalSysExEvent(messageBytes));
            Thread.Sleep(dutyCycle);
        }
    }
    
    public void OnTrackData(TrackData track)
    {
        var peak = Reducer.CurrentValue;
        int value = (int) RemapClampRange.Remap(peak);
        var messageBytes = OnValue(value);
        OutputDevice.SendEvent(new NormalSysExEvent(messageBytes));
    }
}
