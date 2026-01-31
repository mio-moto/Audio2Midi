using System.CommandLine;
using Audio2Midi.Core;

namespace Audio2Midi.CLI.Commands;

public static class ListCaptureDevices
{
    public static RootCommand BindCommand(RootCommand rootCommand)
    {
        var listDevicesCmd = new Command("list-capture-devices")
        {
            Description = "Lists all audio capture devices",
            // TODO: add filter by audio stack
        };
        listDevicesCmd.SetAction(result => ExecuteListCaptureDevices());
        rootCommand.Add(listDevicesCmd);
        return rootCommand;
    }

    private static int ExecuteListCaptureDevices()
    {
        var devices = DeviceRecording.ListRecordingDevices(false).ToArray();
        var longestName = devices.Max(device => device.Name.Length);
        var longestDriver = devices.Max(device => device.Driver.Length);
        const string deviceHeader = " Audio Device Name";
        const string driverHeader = "Driver Name";
        var header = $"{deviceHeader.PadRight(longestName+1)} | {driverHeader}\n{"".PadRight(longestName + longestDriver + 5, '-')}";

        Console.WriteLine(header);
        foreach (var device in devices)
        {
            Console.WriteLine($" {device.Name.PadRight(longestName)} | {device.Driver}");
        }

        return 0;
    }
}