using System.Text.Json;
using System.Text.Json.Serialization;
using Audio2Midi.Core;
using Audio2Midi.Core.Utilities;

namespace Audio2Midi.CLI;


public enum TrackOperation
{
    Unknown = 0,
    PeakLinear,
    PeakLogarithmic
}

public record SysexMidiFormat(string sysex, ClampRange? RemapRange, string? SendFirst) : MidiFormat;

/**
 * A CC midi message, with a target channel, the CC and optionall a remapping of the incoming normalized value into a target range.
 * Otherwise 0...127
 */
public record CCMidiFormat(int Channel, int CC, ClampRange? RemapRange) : MidiFormat;

/**
 * Base class for any kind target midi message
 */
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CCMidiFormat), typeDiscriminator: "CC")]
[JsonDerivedType(typeof(SysexMidiFormat), typeDiscriminator: "Sysex")]
public record MidiFormat();

/**
 * The midi binding maps incoming values onto a MIDI device.
 */
public record MidiBinding(string Device, MidiFormat Format, int? Framerate);


/**
 * A track is composed of N channels and then operated on. It then forwards the signal into a <see cref="MidiBinding"/>
 */
public record TrackAssignment(int[] Channels, string? Name, TrackOperation Operation, MidiBinding[] Target);

/**
 * Specifies a capture device from an audio stack. Preferably the driver name.
 * Then <paramref name="Tracks"/> is used to transform its audio tracks to the outgoing format / device. <br />
 * Use <paramref name="SampleRate"/> and <paramref name="Channels"/> as optional hints to set the correct sample rate and available channels of the capture device. <br />
 * Either Driver or Device has be set.
 */
public record CaptureDeviceAssignment(string? Driver, string? Device, TrackAssignment[] Tracks, string? SampleRate, string? Channels) : IJsonOnDeserialized
{
    public void OnDeserialized()
    {
        if (Driver is null && Device is null)
        {
            throw new JsonException("Either Driver or Device must be specified.");
        }
    }
}


public record TempoConfig(
    int Channel,
    string From,
    string To,
    int? PulseCountCC,
    int? BeatCountCC,
    int? BarCountCC,
    int? PhraseCountCC,
    int BarsPerPhrase = 8,
    int BeatsPerBar = 4,
    int PulsePerBeat = 6,
    int PPQN = 24) : Core.Other.TempoConfig(Channel, PulseCountCC, BeatCountCC, BarCountCC, PhraseCountCC, BarsPerPhrase, BeatsPerBar, PulsePerBeat, PPQN);

/**
 * Root configuration format, declares N devices that are being bound.
 * <example>{ devices: [{...}] }</example>
 */
public record Configuration(CaptureDeviceAssignment[] Devices, TempoConfig[] Tempo);
