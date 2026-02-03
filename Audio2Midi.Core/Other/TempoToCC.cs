using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace Audio2Midi.Core.Other;

public record TempoConfig(int Channel, int? PulseCountCC, int? BeatCountCC, int? BarCountCC, int? PhraseCountCC, int BarsPerPhrase = 8,  int BeatsPerBar=4, int PulsePerBeat=6, int PPQN = 24);


public class TempoToCC
{
    private InputDevice From { get; set; }
    private OutputDevice To { get; set; }
    private TempoConfig Config { get; set; }
    
    public TempoToCC(InputDevice from, OutputDevice to, TempoConfig tempoConfig)
    {
        From = from;
        To = to;
        Config = tempoConfig;
        From.EventReceived += FromOnEventReceived;
    }

    private bool _started;
    private int _pulse;
    private int _beat;
    private int _bar;
    private int _phrase;

    private void FromOnEventReceived(object? sender, MidiEventReceivedEventArgs e)
    {
        switch (e.Event)
        {
            case TimingClockEvent:
                // One MIDI clock tick (1/24 quarter note)
                if (_started)
                {
                    _pulse += 1;
                }
                break;

            case StartEvent:
                _pulse = 0;
                _beat = 0;
                _bar = 0;
                _started = true;
                break;
            case ContinueEvent:
                _pulse = 0;
                _beat = 0;
                _bar = 0;
                _started = true;
                break;
            case StopEvent:
                _pulse = 0;
                _beat = 0;
                _bar = 0;
                _started = false;
                break;
        }

        if (_pulse >= Config.PulsePerBeat)
        {
            _pulse -= Config.PulsePerBeat;
            _beat += 1;
        }

        if (_beat >= Config.BeatsPerBar)
        {
            _beat -= Config.BeatsPerBar;
            _bar += 1;
        }

        if (_bar >= Config.BarsPerPhrase)
        {
            _bar -= Config.BarsPerPhrase;
            _phrase += 1;
        }
        // Phrase can only be 0...127
        _phrase %= 128;

        if (Config.PulseCountCC != null)
        {
            To.SendEvent(new ControlChangeEvent((SevenBitNumber) Config.PulseCountCC, (SevenBitNumber) _pulse) { Channel = (FourBitNumber) Config.Channel});
        }

        if (Config.BeatCountCC != null)
        {
            To.SendEvent(new ControlChangeEvent((SevenBitNumber) Config.BeatCountCC, (SevenBitNumber) _beat) { Channel = (FourBitNumber) Config.Channel});
        }
        
        if (Config.BarCountCC != null)
        {
            To.SendEvent(new ControlChangeEvent((SevenBitNumber) Config.BarCountCC, (SevenBitNumber) _bar) { Channel = (FourBitNumber) Config.Channel});
        }
        if (Config.PhraseCountCC != null)
        {
            To.SendEvent(new ControlChangeEvent((SevenBitNumber) Config.PhraseCountCC, (SevenBitNumber) _phrase) { Channel = (FourBitNumber) Config.Channel});
        }
    }
}
