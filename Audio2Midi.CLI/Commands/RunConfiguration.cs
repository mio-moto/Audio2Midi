using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using Audio2Midi.Core;
using Audio2Midi.Core.Other;
using Melanchall.DryWetMidi.Multimedia;

namespace Audio2Midi.CLI.Commands;

public class RunConfiguration
{
    public static RootCommand BindCommand(RootCommand rootCommand)
    {
        var runConfigurationCommand = new Command("run")
        {
            Description = "Run Audio2Midi.Core with the configuration",
            Options = { new Option<string>("--configuration")
            {
                Description = "Path to the configuration file",
                Required = true
            }
            }
        };
        runConfigurationCommand.SetAction(result => ExecuteRunConfiguration(result));
        rootCommand.Add(runConfigurationCommand);
        return rootCommand;
    }

    private static async Task<int> ExecuteRunConfiguration(ParseResult parseResult)
    {
        if (parseResult.Errors.Any())
        {
            throw new Exception("Command parsing error");
        }

        var configurationPath = parseResult.GetValue<string>("--configuration");
        if (string.IsNullOrWhiteSpace(configurationPath))
        {
            throw new Exception("Configuration path cannot be empty (or whitespace)");
        }

        if (!File.Exists(configurationPath))
        {
            throw new FileNotFoundException($"Configuration file {configurationPath} cannot be found");
        }
        
        var configurationFile = new FileInfo(configurationPath);
        var options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            },
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };
        var configuration = await JsonSerializer.DeserializeAsync<Configuration>(configurationFile.OpenRead(), options);
        if (configuration is null)
        {
            throw new Exception($"Configuration file '{configurationPath}' could not be deserialized");
        }
        
        Initialize(configuration);
        
        return 0;
    }

    

    private static void Initialize(Configuration configuration)
    {
        // down here is currently only happy code paths, you can judge me

        var deviceProvider = new DeviceProvider();
        deviceProvider.AddCaptureDevices(configuration.Devices);
        var midiDeviceTargets = configuration.Devices.SelectMany(x => x.Tracks.SelectMany(y => y.Target.Select(x => x.Device))).ToList();
        var midiTempoTarget = configuration.Tempo.Select(x => x.To).ToList();
        midiDeviceTargets.AddRange(midiTempoTarget);
        deviceProvider.AddMidiOutputDevicesByName(midiDeviceTargets.Distinct().ToArray());

        var midiInputs = configuration.Tempo.Select(x => x.From);
        deviceProvider.AddMidiInputDevicesByName(midiInputs.Distinct().ToArray());

        var graphBuilder = new GraphBuilder(deviceProvider);
        foreach (var assignment in configuration.Devices)
        {
            graphBuilder.BuildGraphPipeline(assignment);
        }

        var tempos = new List<TempoToCC>();
        foreach (var tempoConfig in configuration.Tempo)
        {
            var from = tempoConfig.From;
            var to = tempoConfig.To;

            var fromDevice = deviceProvider.MidiInputDevices.First(x => x.Name == from);
            var toDevice = deviceProvider.MidiOutputDevices.First(x => x.Name == to);
            tempos.Add(new TempoToCC(fromDevice, toDevice, tempoConfig));
        }

        Console.ReadLine();
    }
}
