using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Newtonsoft.Json;
using PEAK.CustomVoicer.Configuration;
using UnityEngine;
using UnityEngine.Networking;

namespace PEAK.CustomVoicer.Voice;

public sealed class VoicePackLoader : MonoBehaviour
{
    public static VoicePackLoader Instance { get; private set; } = null!;

    private readonly List<LoadedVoiceEntry> _entries = new();
    private string _packDirectory = string.Empty;
    private bool _isLoading;

    public IReadOnlyList<LoadedVoiceEntry> Entries => _entries;
    public bool IsLoaded { get; private set; }

    private void Awake()
    {
        Instance = this;
        _packDirectory = Path.Combine(Paths.PluginPath, "PEAK.CustomVoicer");
        Directory.CreateDirectory(_packDirectory);
        StartCoroutine(LoadPackRoutine());
    }

    public void Reload()
    {
        if (_isLoading)
        {
            return;
        }

        StartCoroutine(LoadPackRoutine());
    }

    public LoadedVoiceEntry? GetEntry(int globalIndex)
    {
        if (globalIndex < 0 || globalIndex >= _entries.Count)
        {
            return null;
        }

        return _entries[globalIndex];
    }

    private IEnumerator LoadPackRoutine()
    {
        _isLoading = true;
        IsLoaded = false;
        UnloadClips();

        var packFile = ModConfig.VoicePackFile.Value;
        var jsonPath = Path.Combine(_packDirectory, packFile);

        if (!File.Exists(jsonPath))
        {
            Plugin.Log.LogWarning($"Voice pack not found: {jsonPath}");
            WriteExamplePack(jsonPath);
            _isLoading = false;
            yield break;
        }

        VoicePackConfig? config;
        try
        {
            config = JsonConvert.DeserializeObject<VoicePackConfig>(File.ReadAllText(jsonPath));
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to parse voice pack JSON: {ex.Message}");
            _isLoading = false;
            yield break;
        }

        if (config?.Entries == null || config.Entries.Count == 0)
        {
            Plugin.Log.LogWarning("Voice pack has no entries.");
            _isLoading = false;
            yield break;
        }

        Plugin.Log.LogInfo($"Loading voice pack '{config.Name}' with {config.Entries.Count} entries.");

        foreach (var entry in config.Entries.Where(e => !string.IsNullOrWhiteSpace(e.File)))
        {
            var audioPath = Path.Combine(_packDirectory, entry.File);
            if (!File.Exists(audioPath))
            {
                Plugin.Log.LogWarning($"Missing audio file: {audioPath}");
                continue;
            }

            var loaded = new LoadedVoiceEntry
            {
                Label = string.IsNullOrWhiteSpace(entry.Label) ? Path.GetFileNameWithoutExtension(entry.File) : entry.Label,
                Subtitle = entry.Subtitle,
            };

            yield return LoadClip(audioPath, clip => loaded.Clip = clip);

            if (loaded.Clip != null)
            {
                PreparePlaybackClips(loaded);
                _entries.Add(loaded);
            }
        }

        IsLoaded = _entries.Count > 0;
        Plugin.Log.LogInfo($"Voice pack ready: {_entries.Count} clips loaded.");
        _isLoading = false;
    }

    private static IEnumerator LoadClip(string path, Action<AudioClip> onLoaded)
    {
        var uri = new Uri(path).AbsoluteUri;
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var audioType = extension switch
        {
            ".wav" => AudioType.WAV,
            ".ogg" => AudioType.OGGVORBIS,
            ".mp3" => AudioType.MPEG,
            _ => AudioType.UNKNOWN,
        };

        if (audioType == AudioType.UNKNOWN)
        {
            Plugin.Log.LogWarning($"Unsupported audio extension: {path}");
            yield break;
        }

        using var request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Plugin.Log.LogError($"Failed to load audio '{path}': {request.error}");
            yield break;
        }

        var clip = DownloadHandlerAudioClip.GetContent(request);
        clip.name = Path.GetFileNameWithoutExtension(path);
        onLoaded(clip);
    }

    private static void PreparePlaybackClips(LoadedVoiceEntry entry)
    {
        var sourceClip = entry.Clip;
        if (sourceClip == null)
        {
            return;
        }

        var samples = new float[sourceClip.samples * Math.Max(1, sourceClip.channels)];
        sourceClip.GetData(samples, 0);

        var analysis = AudioNormalization.Analyze(samples);
        var normalizationGain = ModConfig.NormalizeVoiceClips.Value
            ? AudioNormalization.CalculateGain(analysis, ModConfig.TargetRmsDb.Value)
            : 1f;

        if (ModConfig.NormalizeVoiceClips.Value && analysis.IsSilent)
        {
            Plugin.Log.LogWarning($"Voice clip '{sourceClip.name}' is near silent; skipping normalization gain.");
        }

        if (ModConfig.NormalizeVoiceClips.Value)
        {
            Plugin.Log.LogDebug(
                $"Voice clip '{sourceClip.name}' normalization: rms={LinearToDb(analysis.Rms):0.0} dBFS, activeRms={LinearToDb(analysis.ActiveRms):0.0} dBFS, peak={LinearToDb(analysis.Peak):0.0} dBFS, gain={normalizationGain:0.00}x.");
        }

        var localSamples = (float[])samples.Clone();
        AudioNormalization.ApplyGain(localSamples, normalizationGain);

        var streamSamples = (float[])localSamples.Clone();
        AudioNormalization.ApplyGain(streamSamples, ModConfig.StreamVolume.Value);

        entry.Clip = CreateClip(sourceClip, $"{sourceClip.name}_normalized", localSamples);
        entry.StreamClip = CreateClip(sourceClip, $"{sourceClip.name}_stream", streamSamples);

        Destroy(sourceClip);
    }

    private static AudioClip CreateClip(AudioClip sourceClip, string name, float[] samples)
    {
        var clip = AudioClip.Create(name, sourceClip.samples, Math.Max(1, sourceClip.channels), Math.Max(1, sourceClip.frequency), false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float LinearToDb(float value)
    {
        return value > 0f ? 20f * Mathf.Log10(value) : -120f;
    }

    private void UnloadClips()
    {
        foreach (var entry in _entries)
        {
            if (entry.Clip != null)
            {
                Destroy(entry.Clip);
            }

            if (entry.StreamClip != null && entry.StreamClip != entry.Clip)
            {
                Destroy(entry.StreamClip);
            }
        }

        _entries.Clear();
    }

    private static void WriteExamplePack(string jsonPath)
    {
        var example = new VoicePackConfig
        {
            Name = "Example",
            Entries = new List<VoiceEntryConfig>
            {
                new() { Label = "Slot 1", File = "example1.ogg" },
                new() { Label = "Slot 2", File = "example2.wav" },
            },
        };

        try
        {
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(example, Formatting.Indented));
            Plugin.Log.LogInfo($"Created example voice pack at {jsonPath}. Add audio files and restart.");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Could not write example voice pack: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        UnloadClips();
    }
}
