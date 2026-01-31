using System.CommandLine;
using Audio2Midi.CLI.Commands;

namespace Audio2Midi.CLI;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var root = new RootCommand("Audio2Midi.Core CLI");
        
        ListCaptureDevices.BindCommand(root);
        ListMidiDevices.BindCommand(root);
        RunConfiguration.BindCommand(root);
        
        return await root.Parse(args).InvokeAsync();
    }

}
