using Audio2Midi.Core;
using Audio2Midi.Core.Sources;
using Melanchall.DryWetMidi.Multimedia;

namespace Audio2Midi.CLI;

public class DeviceProvider
{
    public ICollection<Device> SourceDevices { get; private set; }
    public IDictionary<Device, CaptureDevice> CaptureDevices { get; private set; }
    public ICollection<OutputDevice> MidiDevices { get; private set; }

    public DeviceProvider()
    {
        SourceDevices = new List<Device>();
        CaptureDevices = new Dictionary<Device, CaptureDevice>();
        MidiDevices = new List<OutputDevice>();
    }
    
    /**
     * Initialized requested MIDI devices and returns a list of them.
     */
    public ICollection<OutputDevice> AddMidiDeviceByName(params string[] deviceNames)
    {
        var existingDevices = MidiDevices.Where(x => deviceNames.Contains(x.Name));
        var missingDevices = deviceNames.Where(x => MidiDevices.All(y => y.Name != x));
        
        var allMidiDevices = OutputDevice.GetAll();
        var targetDevices = allMidiDevices.Where(d => missingDevices.Contains(d.Name)).ToList();
        foreach (var targetDevice in targetDevices)
        {
            Console.WriteLine($"Preparing MIDI Device Name='{targetDevice.Name}'");
            targetDevice.PrepareForEventsSending();
            MidiDevices.Add(targetDevice);
        }

        return targetDevices;
    }

    public ICollection<Device> AddCaptureDevices(params CaptureDeviceAssignment[] devices)
    {
        var existingDevices = SourceDevices.Where(x => devices.Any(y => y.Driver == x.Driver || y.Device == x.Name));
        var missingDevices = devices.Where(x => SourceDevices.All(y => y.Driver != x.Driver && y.Name != x.Device)).ToArray();
        var additionalDevices = GetUniqueCaptureDevices(missingDevices);
        foreach (var device in additionalDevices)
        {
          Console.WriteLine($"Create Capture device Name='{device.Name}', Driver='{device.Driver}'");
          SourceDevices.Add(device);
          var deviceRecording = new DeviceRecording();
          var captureDevice = deviceRecording.CreateCaptureDevice(device, 10, null, null);
          CaptureDevices.Add(device, captureDevice);
        }

        return additionalDevices;
    }
    
    private static List<Device> GetUniqueCaptureDevices(CaptureDeviceAssignment[] requestedDevices)
    {
        var existingCaptureDevices = DeviceRecording.ListRecordingDevices(false).ToList();

        // everything with a driver gets priority
        var onlyDrivers = requestedDevices.Where(x => x.Driver != null);
        // everything with just a name will be matched later
        var onlyNames = requestedDevices.Where(x => x.Device != null && x.Driver == null);
        
        var result = new List<Device>();
        foreach (var onlyDriver in onlyDrivers)
        {
            var device = existingCaptureDevices.FirstOrDefault(x => x.Driver == onlyDriver.Driver);
            if (device == null)
            {
                continue;
            }
            // no duplicates
            if (result.Any(x => x.Driver == device.Driver))
            {
                continue;
            }
            result.Add(device);
        }

        foreach (var onlyName in onlyNames)
        {
            if (result.Any(x => x.Name == onlyName.Device))
            {
                continue;
            }

            // this should be always unique?
            var device = existingCaptureDevices.FirstOrDefault(x => x.Name == onlyName.Device);
            if (device == null)
            {
                continue;
            }
            
            result.Add(device);
        }

        return result;
    } 
}
