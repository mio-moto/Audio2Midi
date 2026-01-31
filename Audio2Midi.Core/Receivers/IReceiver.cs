using Audio2Midi.Core.Reducers;

namespace Audio2Midi.Core;

/**
 * Single track receiver, typically either transformers like histograms and falloffs or end of line senders.
 * Basically anything that makes sense on a more fixed framerate or circular buffers.
 */
public interface IReceiver
{
    public void OnTrackData(TrackData track);
    public IReducers Reducer { get; }
}
