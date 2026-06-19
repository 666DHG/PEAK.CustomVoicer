using System;

namespace PEAK.CustomVoicer.Voice;

public static class AudioNormalization
{
    public const float DefaultTargetRmsDb = -18f;
    public const float DefaultPeakLimit = 0.95f;
    public const float DefaultMaxGain = 12f;
    public const float SilenceRmsThreshold = 0.0001f;
    private const float ActiveSampleThreshold = 0.005f;

    public readonly struct Analysis
    {
        public Analysis(float rms, float activeRms, float peak)
        {
            Rms = rms;
            ActiveRms = activeRms;
            Peak = peak;
        }

        public float Rms { get; }
        public float ActiveRms { get; }
        public float Peak { get; }
        public bool IsSilent => Rms < SilenceRmsThreshold || Peak <= 0f;
        public float LoudnessRms => ActiveRms > 0f ? ActiveRms : Rms;
    }

    public static Analysis Analyze(float[] samples)
    {
        if (samples == null || samples.Length == 0)
        {
            return new Analysis(0f, 0f, 0f);
        }

        double sumSquares = 0d;
        var peak = 0f;

        for (var i = 0; i < samples.Length; i++)
        {
            var sample = samples[i];
            sumSquares += sample * sample;
            peak = Math.Max(peak, Math.Abs(sample));
        }

        var rms = (float)Math.Sqrt(sumSquares / samples.Length);
        double activeSumSquares = 0d;
        var activeSamples = 0;

        for (var i = 0; i < samples.Length; i++)
        {
            var sample = samples[i];
            if (Math.Abs(sample) < ActiveSampleThreshold)
            {
                continue;
            }

            activeSumSquares += sample * sample;
            activeSamples++;
        }

        var activeRms = activeSamples > 0
            ? (float)Math.Sqrt(activeSumSquares / activeSamples)
            : rms;

        return new Analysis(rms, activeRms, peak);
    }

    public static float CalculateGain(float[] samples, float targetRmsDb, float peakLimit = DefaultPeakLimit, float maxGain = DefaultMaxGain)
    {
        return CalculateGain(Analyze(samples), targetRmsDb, peakLimit, maxGain);
    }

    public static float CalculateGain(Analysis analysis, float targetRmsDb, float peakLimit = DefaultPeakLimit, float maxGain = DefaultMaxGain)
    {
        if (analysis.IsSilent || float.IsNaN(targetRmsDb) || float.IsInfinity(targetRmsDb))
        {
            return 1f;
        }

        var loudnessRms = analysis.LoudnessRms;
        if (loudnessRms <= 0f)
        {
            return 1f;
        }

        var targetRms = DbToLinear(targetRmsDb);
        var rmsGain = targetRms / loudnessRms;
        var clampedMaxGain = maxGain > 0f ? maxGain : 1f;
        var gain = Math.Min(rmsGain, clampedMaxGain);

        if (float.IsNaN(gain) || float.IsInfinity(gain) || gain <= 0f)
        {
            return 1f;
        }

        return gain;
    }

    public static void ApplyGain(float[] samples, float gain, float peakLimit = DefaultPeakLimit)
    {
        if (samples == null || samples.Length == 0)
        {
            return;
        }

        var limit = peakLimit > 0f ? Math.Min(peakLimit, 1f) : 1f;
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = ClampSample(samples[i] * gain, limit);
        }
    }

    public static float DbToLinear(float db)
    {
        return (float)Math.Pow(10d, db / 20d);
    }

    private static float ClampSample(float sample, float limit)
    {
        if (sample > limit)
        {
            return limit;
        }

        if (sample < -limit)
        {
            return -limit;
        }

        return sample;
    }
}
