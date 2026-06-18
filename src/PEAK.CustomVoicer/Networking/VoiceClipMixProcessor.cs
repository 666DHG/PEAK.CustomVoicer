using System;
using Photon.Voice;
using UnityEngine;

namespace PEAK.CustomVoicer.Networking;

internal sealed class VoiceClipMixProcessor : IProcessor<float>
{
    private readonly float[] _samples;
    private readonly int _clipChannels;
    private readonly int _clipFrequency;
    private readonly int _voiceChannels;
    private readonly int _voiceFrequency;
    private readonly float _gain;
    private double _clipFramePosition;

    public VoiceClipMixProcessor(AudioClip clip, int voiceChannels, int voiceFrequency, float gain)
    {
        if (clip == null)
        {
            throw new ArgumentNullException(nameof(clip));
        }

        _clipChannels = Math.Max(1, clip.channels);
        _clipFrequency = Math.Max(1, clip.frequency);
        _voiceChannels = Math.Max(1, voiceChannels);
        _voiceFrequency = Math.Max(1, voiceFrequency);
        _gain = gain;
        _samples = new float[clip.samples * _clipChannels];
        clip.GetData(_samples, 0);
    }

    public bool IsFinished => _clipFramePosition >= _samples.Length / _clipChannels;

    public float[] Process(float[] buffer)
    {
        if (buffer == null)
        {
            return Array.Empty<float>();
        }

        if (buffer.Length == 0 || IsFinished)
        {
            return buffer;
        }

        var frameCount = buffer.Length / _voiceChannels;
        var step = (double)_clipFrequency / _voiceFrequency;

        for (var frame = 0; frame < frameCount && !IsFinished; frame++)
        {
            var clipFrame = (int)_clipFramePosition;
            var nextClipFrame = Math.Min(clipFrame + 1, (_samples.Length / _clipChannels) - 1);
            var blend = (float)(_clipFramePosition - clipFrame);

            for (var channel = 0; channel < _voiceChannels; channel++)
            {
                var mixed = ReadClipSample(clipFrame, nextClipFrame, blend, channel) * _gain;
                var bufferIndex = frame * _voiceChannels + channel;
                buffer[bufferIndex] = Mathf.Clamp(buffer[bufferIndex] + mixed, -1f, 1f);
            }

            _clipFramePosition += step;
        }

        return buffer;
    }

    private float ReadClipSample(int clipFrame, int nextClipFrame, float blend, int voiceChannel)
    {
        var clipChannel = _clipChannels == 1 ? 0 : Math.Min(voiceChannel, _clipChannels - 1);
        var a = _samples[(clipFrame * _clipChannels) + clipChannel];
        var b = _samples[(nextClipFrame * _clipChannels) + clipChannel];
        return a + ((b - a) * blend);
    }

    public void Dispose()
    {
    }
}
