using System.Text.Json.Serialization;

namespace PEAK.CustomVoicer.VoicePackTool;

public sealed class VoicePackFile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "CustomVoicePack";

    [JsonPropertyName("entries")]
    public List<VoicePackEntry> Entries { get; set; } = new();
}
