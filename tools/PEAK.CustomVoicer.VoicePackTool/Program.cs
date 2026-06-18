namespace PEAK.CustomVoicer.VoicePackTool;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Any(arg => string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)))
        {
            PrintHelp();
            return 0;
        }

        var unknownArgs = args
            .Where(arg => !string.Equals(arg, "--dry-run", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (unknownArgs.Count > 0)
        {
            Console.Error.WriteLine($"Unknown argument: {unknownArgs[0]}");
            PrintHelp();
            return 2;
        }

        var dryRun = args.Any(arg => string.Equals(arg, "--dry-run", StringComparison.OrdinalIgnoreCase));
        var directoryPath = AppContext.BaseDirectory;

        try
        {
            var result = new VoicePackSynchronizer().Synchronize(directoryPath, dryRun);
            PrintResult(result);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to synchronize voice_pack.json.");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("PEAK.CustomVoicer Voice Pack Tool");
        Console.WriteLine();
        Console.WriteLine("Place this exe next to PEAK.CustomVoicer.dll and your .wav/.ogg/.mp3 files, then run it.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  PEAK.CustomVoicer.VoicePackTool.exe [--dry-run] [--help]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --dry-run   Preview changes without writing voice_pack.json.");
        Console.WriteLine("  --help      Show this help text.");
    }

    private static void PrintResult(VoicePackSyncResult result)
    {
        Console.WriteLine("PEAK.CustomVoicer Voice Pack Tool");
        Console.WriteLine($"Folder: {result.DirectoryPath}");
        Console.WriteLine($"Voice pack: {result.JsonPath}");
        Console.WriteLine();

        if (result.AudioFiles.Count == 0)
        {
            Console.WriteLine("No supported audio files found. Add .wav, .ogg, or .mp3 files next to this tool and run it again.");
        }

        Console.WriteLine($"Audio files found: {result.AudioFiles.Count}");
        Console.WriteLine($"Kept: {result.Kept.Count}");
        Console.WriteLine($"Added: {result.Added.Count}");
        Console.WriteLine($"Removed: {result.Removed.Count}");

        PrintEntries("Added entries", result.Added);
        PrintEntries("Removed entries", result.Removed);

        Console.WriteLine();
        if (result.DryRun)
        {
            Console.WriteLine(result.Changed ? "Dry run complete. voice_pack.json was not changed." : "Dry run complete. No changes needed.");
            return;
        }

        if (!result.Changed)
        {
            Console.WriteLine("No changes needed.");
            return;
        }

        Console.WriteLine(result.Created ? "Created voice_pack.json." : "Updated voice_pack.json.");
        if (!string.IsNullOrWhiteSpace(result.BackupPath))
        {
            Console.WriteLine($"Backup created: {result.BackupPath}");
        }
    }

    private static void PrintEntries(string title, IReadOnlyList<VoicePackEntry> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"{title}:");
        foreach (var entry in entries)
        {
            Console.WriteLine($"  - {entry.File} ({entry.Label})");
        }
    }
}
