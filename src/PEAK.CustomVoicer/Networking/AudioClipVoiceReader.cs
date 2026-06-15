using System;
using Photon.Voice;
using UnityEngine;

namespace PEAK.CustomVoicer.Networking;

/// <summary>
/// Pull-based audio source that streams a Unity AudioClip through Photon Voice.
/// </summary>
internal sealed class AudioClipVoiceReader : IAudioReader<float>, IDisposable
{
    private readonly float[] _samples;
    private readonly int _channels;
    private readonly int _samplingRate;
    private int _position;

    public AudioClipVoiceReader(AudioClip clip)
    {
        if (clip == null)
        {
            throw new ArgumentNullException(nameof(clip));
        }

        _channels = clip.channels;
        _samplingRate = clip.frequency;
        _samples = new float[clip.samples * _channels];
        clip.GetData(_samples, 0);
        _position = 0;
    }

    public int Channels => _channels;

    public int SamplingRate => _samplingRate;

    public string Error => string.Empty;

    public bool Read(float[] buffer)
    {
        if (buffer == null || buffer.Length == 0)
        {
            return false;
        }

        var remaining = _samples.Length - _position;
        if (remaining <= 0)
        {
            return false;
        }

        var toCopy = Math.Min(buffer.Length, remaining);
        Array.Copy(_samples, _position, buffer, 0, toCopy);
        _position += toCopy;

        if (toCopy < buffer.Length)
        {
            Array.Clear(buffer, toCopy, buffer.Length - toCopy);
        }

        return true;
    }

    public void Dispose()
    {
    }
}
