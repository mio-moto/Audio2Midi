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
        var outputDevices = MidiSender.ListMidiOutputDevices();
        var inputDevices = MidiSender.ListMidiInputDevices();
        var allNames = outputDevices.Select(x => x.Name).ToList();
        allNames.AddRange(inputDevices.Select(x => x.Name).ToList());
        var longestName = allNames.Max(name => name.Length);
        var header = $" Midi Device\n{"".PadRight(longestName + 3, '-')}";
        Console.WriteLine(header);
        foreach (var device in inputDevices)
        {
            Console.WriteLine($"< {device.Name}");
        }
        foreach (var device in outputDevices)
        {
            Console.WriteLine($"> {device.Name}");
        }

        return 0;
    }
}