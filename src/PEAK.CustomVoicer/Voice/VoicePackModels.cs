using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PEAK.CustomVoicer.Voice;

[Serializable]
public class VoicePackConfig
{
    [JsonProperty("name")]
    public string Name { get; set; } = "Default";

    [JsonProperty("entries")]
    public List<VoiceEntryConfig> Entries { get; set; } = new();
}

[Serializable]
public class VoiceEntryConfig
{
    [JsonProperty("label")]
    public string Label { get; set; } = string.Empty;

    [JsonProperty("file")]
    public string File { get; set; } = string.Empty;

    [JsonProperty("subtitle")]
    public string? Subtitle { get; set; }
}

public sealed class LoadedVoiceEntry
{
    public string Label { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public UnityEngine.AudioClip? Clip { get; set; }
    public UnityEngine.AudioClip? StreamClip { get; set; }
}
