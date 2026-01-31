using System.CommandLine;
using Audio2Midi.Core;

namespace Audio2Midi.CLI.Commands;

public abstract class ListMidiDevices
{
    public static RootCommand BindCommand(RootCommand rootCommand)
    {
        var listMidiDevicesCommand = new Command("list-midi-devices")
        {
            Description = "Lists midi devices",
        };
        listMidiDevicesCommand.SetAction(_ => ExecuteListMidiDevices());
        rootCommand.Add(listMidiDevicesCommand);
        return rootCommand;
    }

    private static int ExecuteListMidiDevices()
    {
        var devices = MidiSender.ListMidiDevices();
        var longestName = devices.Max(device => device.Name.Length);
        var header = $" Midi Device\n{"".PadRight(longestName + 2, '-')}";
        Console.WriteLine(header);
        foreach (var device in devices)
        {
            Console.WriteLine($" {device.Name}");
        }

        return 0;
    }
}