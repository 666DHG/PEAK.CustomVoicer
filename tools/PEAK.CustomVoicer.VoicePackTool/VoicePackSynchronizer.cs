using System.Text;
using System.Text.Json;

namespace PEAK.CustomVoicer.VoicePackTool;

public sealed class VoicePackSynchronizer
{
    public const string DefaultPackName = "CustomVoicePack";
    public const string VoicePackFileName = "voice_pack.json";

    private static readonly HashSet<string> SupportedAudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav",
        ".ogg",
        ".mp3",
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly TimeProvider _timeProvider;

    public VoicePackSynchronizer()
        : this(TimeProvider.System)
    {
    }

    public VoicePackSynchronizer(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public VoicePackSyncResult Synchronize(string directoryPath, bool dryRun = false)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path is required.", nameof(directoryPath));
        }

        var fullDirectoryPath = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(fullDirectoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {fullDirectoryPath}");
        }

        var jsonPath = Path.Combine(fullDirectoryPath, VoicePackFileName);
        var jsonExists = File.Exists(jsonPath);
        var currentPack = jsonExists ? ReadVoicePack(jsonPath) : new VoicePackFile { Name = DefaultPackName };
        currentPack.Entries ??= new List<VoicePackEntry>();
        var audioFiles = FindAudioFiles(fullDirectoryPath);
        var audioByName = audioFiles.ToDictionary(file => file, StringComparer.OrdinalIgnoreCase);
        var kept = new List<VoicePackEntry>();
        var removed = new List<VoicePackEntry>();
        var usedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in currentPack.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.File) || !audioByName.ContainsKey(entry.File))
            {
                removed.Add(CloneEntry(entry));
                continue;
            }

            kept.Add(CloneEntry(entry));
            usedFiles.Add(entry.File);
        }

        var added = audioFiles
            .Where(file => !usedFiles.Contains(file))
            .Select(file => new VoicePackEntry
            {
                Label = Path.GetFileNameWithoutExtension(file),
                File = file,
            })
            .ToList();

        var nextPack = new VoicePackFile
        {
            Name = string.IsNullOrWhiteSpace(currentPack.Name) ? DefaultPackName : currentPack.Name,
            Entries = kept.Concat(added).Select(CloneEntry).ToList(),
        };

        var existingText = jsonExists ? File.ReadAllText(jsonPath, Encoding.UTF8) : null;
        var nextText = Serialize(nextPack);
        var changed = !jsonExists || !string.Equals(NormalizeJson(existingText!), NormalizeJson(nextText), StringComparison.Ordinal);
        string? backupPath = null;

        if (changed && !dryRun)
        {
            if (jsonExists)
            {
                backupPath = CreateBackup(jsonPath);
            }

            File.WriteAllText(jsonPath, nextText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        return new VoicePackSyncResult
        {
            DirectoryPath = fullDirectoryPath,
            JsonPath = jsonPath,
            BackupPath = backupPath,
            DryRun = dryRun,
            Changed = changed,
            Created = !jsonExists,
            Kept = kept,
            Added = added,
            Removed = removed,
            AudioFiles = audioFiles,
        };
    }

    private static VoicePackFile ReadVoicePack(string jsonPath)
    {
        var pack = JsonSerializer.Deserialize<VoicePackFile>(File.ReadAllText(jsonPath, Encoding.UTF8), JsonOptions);
        return pack ?? new VoicePackFile { Name = DefaultPackName };
    }

    private static List<string> FindAudioFiles(string directoryPath)
    {
        return Directory.EnumerateFiles(directoryPath, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(file => !string.IsNullOrWhiteSpace(file))
            .Where(file => SupportedAudioExtensions.Contains(Path.GetExtension(file!)))
            .Select(file => file!)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ThenBy(file => file, StringComparer.Ordinal)
            .ToList();
    }

    private string CreateBackup(string jsonPath)
    {
        var timestamp = _timeProvider.GetLocalNow().ToString("yyyyMMdd-HHmmss");
        var backupPath = $"{jsonPath}.bak.{timestamp}";
        var suffix = 1;
        while (File.Exists(backupPath))
        {
            backupPath = $"{jsonPath}.bak.{timestamp}.{suffix}";
            suffix++;
        }

        File.Copy(jsonPath, backupPath);
        return backupPath;
    }

    private static string Serialize(VoicePackFile pack)
    {
        return JsonSerializer.Serialize(pack, JsonOptions) + Environment.NewLine;
    }

    private static string NormalizeJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement, JsonOptions);
    }

    private static VoicePackEntry CloneEntry(VoicePackEntry entry)
    {
        return new VoicePackEntry
        {
            Label = entry.Label,
            File = entry.File,
            Subtitle = entry.Subtitle,
        };
    }
}
