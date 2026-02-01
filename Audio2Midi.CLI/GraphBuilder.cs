using Audio2Midi.Core;
using Audio2Midi.Core.Receivers;
using Audio2Midi.Core.Reducers;
using Audio2Midi.Core.Sources;
using Melanchall.DryWetMidi.Core;

namespace Audio2Midi.CLI;

public class GraphBuilder
{
    public DeviceProvider DeviceProvider { get; private set; }
    
    public GraphBuilder(DeviceProvider deviceProvider)
    {
        DeviceProvider = deviceProvider;
    }

    public CaptureDevice BuildGraphPipeline(CaptureDeviceAssignment deviceAssignment)
    {
        var matchingDevices = DeviceProvider.SourceDevices.Where(x => x.Driver == deviceAssignment.Driver || x.Name == deviceAssignment.Device);
        var enumerable = matchingDevices as Device[] ?? matchingDevices.ToArray();
        var device = enumerable.FirstOrDefault(x => x.Driver == deviceAssignment.Driver) ?? enumerable.FirstOrDefault(x => x.Name == deviceAssignment.Device);

        if (device == null)
        {
            throw new Exception("Matching Device not found");
        }
        
        // get an existing, already recording audio device
        var captureDevice = DeviceProvider.CaptureDevices[device];
        Console.WriteLine($"Prepring device: Name='{device.Name}', Driver='{device.Driver}'");
        foreach (var track in deviceAssignment.Tracks)
        {
            IOperator trackOperator;
            switch (track.Operation)
            {
                
                case TrackOperation.PeakLinear:
                {
                    Console.WriteLine($"> Creating Peak Linear Operator, Tracks=[{string.Join(", ", track.Channels)}]");
                    trackOperator = new PeakOperator(new Track(track.Channels, track.Name), PeakOperation.Linear);
                    break;
                }
                case TrackOperation.PeakLogarithmic:
                {
                    Console.WriteLine($"> Creating Peak Logarithmic Operator, Tracks=[{string.Join(", ", track.Channels)}]");
                    trackOperator = new PeakOperator(new Track(track.Channels, track.Name), PeakOperation.Logarithmic);
                    break;
                }
                case TrackOperation.Unknown:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Console.WriteLine(">> Adding Operator: LastReceivePeak");
            var lastReceivePeak = new LastReceivePeak();

            captureDevice.OnTrackData += trackOperator.OnChannelData;
            trackOperator.OnTrackData += lastReceivePeak.OnTrackData;
            
            
            foreach (var midiBinding in track.Target)
            {
                var midiDevice = DeviceProvider.MidiDevices.First(x => x.Name == midiBinding.Device);
                if (midiBinding.Format is CCMidiFormat ccFormat)
                {
                    Console.WriteLine($">>> Translating into CC MIDI Device='{midiDevice.Name}' with Channel='{ccFormat.Channel}', CC='{ccFormat.CC}'");
                    var sender = new CCMidiSender(midiDevice, lastReceivePeak, ccFormat.Channel, ccFormat.CC, ccFormat.RemapRange);
                    Task.Factory.StartNew(() => { sender.Run(new TimeSpan(0, 0, 0, 0, 1000 / (midiBinding.Framerate ?? 50))); });
                } else if (midiBinding.Format is SysexMidiFormat sysexFormat)
                {
                    Console.WriteLine($">>> Translating as Sysex Device='{midiDevice.Name}' with Message Template='{sysexFormat.sysex}'" );
                    var sender = new SysexMidiSender(midiDevice, lastReceivePeak, sysexFormat.sysex, sysexFormat.RemapRange);
                    if (sysexFormat.SendFirst != null)
                    {
                        midiDevice.SendEvent(new NormalSysExEvent(SysexMidiSender.ParseFunctor(sysexFormat.SendFirst)(0)));
                    }
                    Task.Factory.StartNew(() => { sender.Run(new TimeSpan(0, 0, 0, 0, 1000 / (midiBinding.Framerate ?? 50))); });
                }
            }
        }

        return captureDevice;
    }
}
