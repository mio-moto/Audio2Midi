using System.Diagnostics.CodeAnalysis;
using Audio2Midi.Core.Sources;
using ManagedBass;
using ManagedBass.Wasapi;

namespace Audio2Midi.Core
{
    public record Device(string Name, string Driver);

    public record WasapiDevice(string Name, string Driver, int Channels, int SampleRate) : Device(Name, Driver);

    public record RecordInfo(Device Device, int RecordHandle, int SampleRate, int Channels);
    public class DeviceRecording
    {
        public bool HasRecordingInitialized { get; private set; }
        public RecordInfo? RecordInfo { get; private set; }
        public List<Thread> SpawnedThreads { get; private set; }
        
        public DeviceRecording()
        {
            SpawnedThreads = new List<Thread>();
        }

        public CaptureDevice CreateCaptureDevice(Device device, int minimumFramerateInMs, int? hintChannels, int? hintSampleRate) {
            if (!GetBassDevice(device, out var result))
            {
                throw new Exception("Bass device not found");
            }
            (var bassDevice, int deviceIndex) = result.Value;
            if (!Bass.RecordInit(deviceIndex))
            {
                throw new Exception("BASS could not initialize the recording session");
            }
            if(!Bass.RecordGetInfo(out var recordInfo))
            {
                throw new Exception("Could not get recording informations");   
            }

            // the given record info conflicts with support formats
            // I cannot tell if that is the minimum or maximum recording sample rate or  what else may decide it
            var sampleRate = hintSampleRate ?? recordInfo.Frequency;
            var channels = hintChannels ?? recordInfo.Channels;
            var captureDevice = new CaptureDevice(channels, sampleRate);

            // no quarrels about this being thread unsafe so far
            var thread = new Thread(() =>
            {
                Bass.CurrentRecordingDevice = deviceIndex;
                var handle = Bass.RecordStart(sampleRate, channels, BassFlags.Default, minimumFramerateInMs, captureDevice.RecordCallback, IntPtr.Zero);
                if (handle == 0)
                {
                    throw new Exception("Could not start recording");
                }
                RecordInfo = new(device, handle, sampleRate, channels);
                Console.WriteLine($"Current Recording Device: {Bass.CurrentRecordingDevice}");
            })
            {
                IsBackground = true,
                Name = $"DeviceCapture-{device.Driver}-{device.Name}"
            };
            SpawnedThreads.Add(thread);
            thread.Start();
            return captureDevice;
        }
        
        public void StopRecording()
        {
            if (!HasRecordingInitialized || RecordInfo == null)
            {
                throw new Exception("Recording has not started or has previously failed");
            }

            HasRecordingInitialized = false;
            Bass.ChannelStop(RecordInfo.Channels);
            RecordInfo = null;
            Bass.RecordFree();
        }

        public static bool GetBassDevice(Device device, [NotNullWhen(true)] out (DeviceInfo bassDeviceInfo, int index)? result)
        {
            {
                var index = 0;
                while (Bass.RecordGetDeviceInfo(index, out var deviceInfo))
                {
                    if (deviceInfo.Name == device.Name && deviceInfo.Driver == device.Driver)
                    {
                        result = (deviceInfo, index);
                        return true;
                    }
                    index += 1;
                }

                result = null;
                return false;
            }
        } 
        
        
        
        /// <summary>
        /// Get a collection of recording devices that are currently available
        /// </summary>
        public static IEnumerable<Device> ListRecordingDevices(bool withWasapi)
        {
            // BASS doesn't distinguish a lot between audio stacks and it is terribly inconsistent to handle.
            // it has a generalized `BASS_RecordGetDeviceInfo` that includes all audio stacks and `BASS_WASAPI_DeviceInfo`
            // as well as `BASS_ASIO_DeviceInfo`. There's no such methods for ALSA, PulseAudio, Apple Core Audio and such
            // improvement could be to support ASIO and WMA directly
            if (withWasapi)
            {
                for (var i = 0; BassWasapi.GetDeviceInfo(i, out var deviceInfo); i++)
                {
                    if (deviceInfo.IsInput && !deviceInfo.IsUnplugged && deviceInfo.IsEnabled)
                    {
                        yield return new WasapiDevice(deviceInfo.Name, deviceInfo.ID, deviceInfo.MixChannels, deviceInfo.MixFrequency);
                    }    
                }
            }
            else
            {
                for (var i = 0; Bass.RecordGetDeviceInfo(i, out var deviceInfo); i += 1)
                {
                    if (!deviceInfo.IsEnabled)
                    {
                        continue;
                    }
                    yield return new Device(deviceInfo.Name, deviceInfo.Driver);
                }
            }
        }
    }
}
