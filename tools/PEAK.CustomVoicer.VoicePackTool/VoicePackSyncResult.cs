namespace PEAK.CustomVoicer.VoicePackTool;

public sealed class VoicePackSyncResult
{
    public string DirectoryPath { get; init; } = string.Empty;
    public string JsonPath { get; init; } = string.Empty;
    public string? BackupPath { get; init; }
    public bool DryRun { get; init; }
    public bool Changed { get; init; }
    public bool Created { get; init; }
    public IReadOnlyList<VoicePackEntry> Kept { get; init; } = Array.Empty<VoicePackEntry>();
    public IReadOnlyList<VoicePackEntry> Added { get; init; } = Array.Empty<VoicePackEntry>();
    public IReadOnlyList<VoicePackEntry> Removed { get; init; } = Array.Empty<VoicePackEntry>();
    public IReadOnlyList<string> AudioFiles { get; init; } = Array.Empty<string>();
}
