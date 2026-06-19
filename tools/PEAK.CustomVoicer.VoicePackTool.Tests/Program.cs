using System.Text;
using System.Text.Json;
using PEAK.CustomVoicer.Voice;
using PEAK.CustomVoicer.VoicePackTool;

namespace PEAK.CustomVoicer.VoicePackTool.Tests;

internal static class Program
{
    private static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("Creates voice_pack.json when missing", CreatesVoicePackWhenMissing),
            ("Preserves existing name label and subtitle", PreservesExistingFields),
            ("Appends new audio files", AppendsNewAudio),
            ("Removes missing audio and creates backup", RemovesMissingAudioAndCreatesBackup),
            ("Recognizes mixed-case extensions", RecognizesMixedCaseExtensions),
            ("Dry run does not write files", DryRunDoesNotWrite),
            ("No audio creates empty pack with clear result", NoAudioCreatesEmptyPack),
            ("Normalization attenuates loud clips", NormalizationAttenuatesLoudClips),
            ("Normalization boosts quiet clips within max gain", NormalizationBoostsQuietClipsWithinMaxGain),
            ("Normalization is not blocked by one transient peak", NormalizationIsNotBlockedByOneTransientPeak),
            ("Normalization respects peak limit", NormalizationRespectsPeakLimit),
            ("Normalization leaves silent clips stable", NormalizationLeavesSilentClipsStable),
        };

        foreach (var test in tests)
        {
            using var temp = new TemporaryDirectory();
            TestContext.CurrentDirectory = temp.Path;
            test.Run();
            Console.WriteLine($"PASS {test.Name}");
        }

        return 0;
    }

    private static void CreatesVoicePackWhenMissing()
    {
        Touch("hello.ogg");

        var result = Synchronize();
        var pack = ReadPack();

        Assert.True(result.Created, "Expected pack to be created.");
        Assert.True(result.Changed, "Expected created pack to be reported as changed.");
        Assert.Equal("CustomVoicePack", pack.Name);
        Assert.Equal(1, pack.Entries.Count);
        Assert.Equal("hello", pack.Entries[0].Label);
        Assert.Equal("hello.ogg", pack.Entries[0].File);
        Assert.Null(pack.Entries[0].Subtitle);
    }

    private static void PreservesExistingFields()
    {
        Touch("hello.ogg");
        WritePack("""
            {
              "name": "MyPack",
              "entries": [
                { "label": "Hello Label", "file": "hello.ogg", "subtitle": "Hi!" }
              ]
            }
            """);

        Synchronize();
        var pack = ReadPack();

        Assert.Equal("MyPack", pack.Name);
        Assert.Equal("Hello Label", pack.Entries[0].Label);
        Assert.Equal("Hi!", pack.Entries[0].Subtitle);
    }

    private static void AppendsNewAudio()
    {
        Touch("hello.ogg");
        Touch("meme.wav");
        WritePack("""
            {
              "name": "MyPack",
              "entries": [
                { "label": "Hello Label", "file": "hello.ogg" }
              ]
            }
            """);

        var result = Synchronize();
        var pack = ReadPack();

        Assert.Equal(1, result.Added.Count);
        Assert.Equal(2, pack.Entries.Count);
        Assert.Equal("hello.ogg", pack.Entries[0].File);
        Assert.Equal("meme.wav", pack.Entries[1].File);
        Assert.Equal("meme", pack.Entries[1].Label);
    }

    private static void RemovesMissingAudioAndCreatesBackup()
    {
        Touch("hello.ogg");
        WritePack("""
            {
              "name": "MyPack",
              "entries": [
                { "label": "Hello Label", "file": "hello.ogg" },
                { "label": "Gone", "file": "gone.mp3", "subtitle": "bye" }
              ]
            }
            """);

        var result = Synchronize();
        var pack = ReadPack();

        Assert.Equal(1, result.Removed.Count);
        Assert.Equal("gone.mp3", result.Removed[0].File);
        Assert.NotNull(result.BackupPath);
        Assert.True(File.Exists(result.BackupPath!), "Expected backup file to exist.");
        Assert.Equal(1, pack.Entries.Count);
        Assert.Equal("hello.ogg", pack.Entries[0].File);
    }

    private static void RecognizesMixedCaseExtensions()
    {
        Touch("LOUD.MP3");
        Touch("soft.WaV");
        Touch("voice.OGG");

        Synchronize();
        var pack = ReadPack();

        Assert.Equal(3, pack.Entries.Count);
        Assert.SequenceEqual(new[] { "LOUD.MP3", "soft.WaV", "voice.OGG" }, pack.Entries.Select(entry => entry.File).ToArray());
    }

    private static void DryRunDoesNotWrite()
    {
        Touch("hello.ogg");

        var result = Synchronize(dryRun: true);

        Assert.True(result.Changed, "Expected dry run to detect pending changes.");
        Assert.False(File.Exists(Path.Combine(TestContext.CurrentDirectory, VoicePackSynchronizer.VoicePackFileName)), "Dry run should not write voice_pack.json.");
    }

    private static void NoAudioCreatesEmptyPack()
    {
        var result = Synchronize();
        var pack = ReadPack();

        Assert.Equal(0, result.AudioFiles.Count);
        Assert.Equal(0, result.Added.Count);
        Assert.Equal(0, pack.Entries.Count);
    }

    private static void NormalizationAttenuatesLoudClips()
    {
        var samples = Enumerable.Repeat(0.5f, 64).ToArray();

        var gain = AudioNormalization.CalculateGain(samples, AudioNormalization.DefaultTargetRmsDb);

        Assert.True(gain < 1f, $"Expected loud clip gain below 1, got {gain}.");
        Assert.Near(AudioNormalization.DbToLinear(AudioNormalization.DefaultTargetRmsDb), 0.5f * gain, 0.0001f);
    }

    private static void NormalizationBoostsQuietClipsWithinMaxGain()
    {
        var samples = Enumerable.Repeat(0.01f, 64).ToArray();

        var gain = AudioNormalization.CalculateGain(samples, AudioNormalization.DefaultTargetRmsDb);

        Assert.Equal(AudioNormalization.DefaultMaxGain, gain);
    }

    private static void NormalizationRespectsPeakLimit()
    {
        var samples = Enumerable.Repeat(0.01f, 63).Append(0.9f).ToArray();

        var gain = AudioNormalization.CalculateGain(samples, AudioNormalization.DefaultTargetRmsDb);
        AudioNormalization.ApplyGain(samples, gain);

        Assert.True(samples.Max(Math.Abs) <= AudioNormalization.DefaultPeakLimit + 0.0001f, "Expected peak limit to be respected.");
    }

    private static void NormalizationIsNotBlockedByOneTransientPeak()
    {
        var samples = Enumerable.Repeat(0.01f, 63).Append(0.9f).ToArray();

        var gain = AudioNormalization.CalculateGain(samples, AudioNormalization.DefaultTargetRmsDb);

        Assert.True(gain > 1f, $"Expected mostly quiet clip to still be boosted, got {gain}.");
    }

    private static void NormalizationLeavesSilentClipsStable()
    {
        var samples = new float[64];

        var gain = AudioNormalization.CalculateGain(samples, AudioNormalization.DefaultTargetRmsDb);
        AudioNormalization.ApplyGain(samples, gain);

        Assert.Equal(1f, gain);
        Assert.True(samples.All(sample => sample == 0f), "Expected silent clip to remain silent.");
        Assert.False(float.IsNaN(gain), "Expected gain not to be NaN.");
        Assert.False(float.IsInfinity(gain), "Expected gain not to be infinity.");
    }

    private static VoicePackSyncResult Synchronize(bool dryRun = false)
    {
        return new VoicePackSynchronizer(new FixedTimeProvider(new DateTimeOffset(2026, 6, 18, 16, 30, 0, TimeSpan.Zero)))
            .Synchronize(TestContext.CurrentDirectory, dryRun);
    }

    private static void Touch(string fileName)
    {
        File.WriteAllBytes(Path.Combine(TestContext.CurrentDirectory, fileName), Array.Empty<byte>());
    }

    private static void WritePack(string json)
    {
        File.WriteAllText(Path.Combine(TestContext.CurrentDirectory, VoicePackSynchronizer.VoicePackFileName), json, new UTF8Encoding(false));
    }

    private static VoicePackFile ReadPack()
    {
        var json = File.ReadAllText(Path.Combine(TestContext.CurrentDirectory, VoicePackSynchronizer.VoicePackFileName), Encoding.UTF8);
        return JsonSerializer.Deserialize<VoicePackFile>(json) ?? throw new InvalidOperationException("Could not read test pack.");
    }
}

internal static class TestContext
{
    public static string CurrentDirectory { get; set; } = string.Empty;
}

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"peak-customvoicer-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        Directory.Delete(Path, recursive: true);
    }
}

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _now;
    }
}

internal static class Assert
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void False(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }

    public static void SequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException($"Expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}].");
        }
    }

    public static void Null(object? value)
    {
        if (value != null)
        {
            throw new InvalidOperationException($"Expected null, got '{value}'.");
        }
    }

    public static void NotNull(object? value)
    {
        if (value == null)
        {
            throw new InvalidOperationException("Expected non-null value.");
        }
    }

    public static void Near(float expected, float actual, float tolerance)
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException($"Expected '{expected}' +/- {tolerance}, got '{actual}'.");
        }
    }
}
