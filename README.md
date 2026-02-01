# Audio2MIDI

This application maps incoming realtime audio to MIDI messages.  
The primary use case is to use a multi track audio device and map its peak audio onto LED feedback of MIDI controllers.

![](Documentation/example.avif)

## Configuration

The data structure can be found in `Audio2Midi.CLI/Configuration.cs`.
You will have to identify the devices you want to map from and to, for this the CLI provides commands:

```sh
$ ./Audio2Midi.CLI list-midi-devices
 Midi Device
------------------------------
 Microsoft GS Wavetable Synth
 2- Focusrite USB MIDI
 Intech Grid MIDI device
 M8

$ ./Audio2Midi.CLI list-capture-devices
 Audio Device Name                       | Driver Name
---------------------------------------------------------------------------------------------------
 Default                                 | 
 Analogue 1 + 2 (2- Focusrite USB Audio) | {0.0.1.00000000}.{133a2c4d-529f-4927-8fc5-ef545b9de2da}
 Analogue 5 + 6 (2- Focusrite USB Audio) | {0.0.1.00000000}.{20945e72-00ea-4cf6-9399-011160c2c51c}
 Analogue 7 + 8 (2- Focusrite USB Audio) | {0.0.1.00000000}.{48c579ae-8f37-4022-8ed7-c6627562226a}
 Digital Audio Interface (12- M8)        | {0.0.1.00000000}.{62b99293-8018-4989-9388-e6cb4f3069de}
 Microphone (Steam Streaming Microphone) | {0.0.1.00000000}.{988dc78e-b17d-4536-b131-c12f550fb8f1}
 Analogue 3 + 4 (2- Focusrite USB Audio) | {0.0.1.00000000}.{c8e41dea-c3af-4b15-be40-a1a6826055df}
 Speakers (Steam Streaming Microphone)   | {0.0.0.00000000}.{07069192-e600-4837-b00d-5e3739bc62c6}
 Digital Audio Interface (12- M8)        | {0.0.0.00000000}.{6112600c-5908-4c5b-bcda-7bd35b0ccb94}
 Speakers (Steam Streaming Speakers)     | {0.0.0.00000000}.{6aa89d07-509f-471d-a046-d33e325e9b92}
 Playback 7 + 8 (2- Focusrite USB Audio) | {0.0.0.00000000}.{8bc6ec21-ca58-4db5-b220-c8c12a4fb089}
 Speakers (2- Focusrite USB Audio)       | {0.0.0.00000000}.{e9678f5d-a248-4622-9926-73a51a365d0d}
 Playback 3 + 4 (2- Focusrite USB Audio) | {0.0.0.00000000}.{ff48e934-798e-4966-b2a7-2e60e7a7106c}
```

Now you can pick the devices and write a configuration like such:

```json
{
  "devices": [{
    "driver": "{0.0.1.00000000}.{242a9eda-b755-4301-b093-0f97cb9260c4}",
    "sampleRate": "44100",
    "channels": "24",
    "tracks": [{
      "channels": [3,4],
      "operation": "PeakLinear",
      "target": [{
          "device": "Intech Grid MIDI device",
          "framerate": 20,
          "format": { "type": "CC", "channel": 10, "cc": 1 }
        }]
      }]
    }, {
    "driver": "{0.0.1.00000000}.{62b99293-8018-4989-9388-e6cb4f3069de}",
    "sampleRate": "44100",
    "channels": "24",
    "tracks": [{
      "channels": [3,4],
      "operation": "PeakLinear",
      "target": [{
          "device": "Intech Grid MIDI device",
          "framerate": 20,
          "format": { "type": "CC", "channel": 10, "cc": 9 }
        }]
      }]
  }]
}
```

The logic is as following:
- devices get mapped
  - by either `driver` or `name`, where `driver` is preferred
  - `sampleRate` and `channels` are used as hint and are option 
- from devices are tracks extracted
  - tracks map audio `channels` of the `device`. Right / left channels are often adjacent to each other, but not always 
  - they run through an `operation` (`PeakLinear`, `PeakLogarithmic`)
- the now reduced PCM data is kept and awaited to be polled from a `target`
  - a target has a framerate, some controllers get easily overwhelmed by a lot of traffic
  - the target is described by the format, so they can be discerned 
  - this can be multiple targets, so more than one MIDI controller can receive the data 


The code used on the Intech GRID Controller is rather simple:

```lua
-- set on element 16 (system) as setup event (when disabling minimalist mode in the grid editor)
self.midirx_cb = function(self, event, header)
  local channel = event[1]
  local message = event[2]
  local parameter = event[3]
  local value = event[4]
  -- channel 10 and CC messages
  if channel == 10 and message == 176 then
    led_value(parameter - 1, 0, value)
  end
end
```

# License

This project is licensed under the MIT License.  
The included native Bass libraries have individual licensing terms, which is free for non-commercial use.