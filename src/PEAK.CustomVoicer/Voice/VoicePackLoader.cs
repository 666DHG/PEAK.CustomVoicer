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

    private void UnloadClips()
    {
        foreach (var entry in _entries)
        {
            if (entry.Clip != null)
            {
                Destroy(entry.Clip);
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
