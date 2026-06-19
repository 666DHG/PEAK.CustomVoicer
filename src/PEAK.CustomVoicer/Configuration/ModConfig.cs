using BepInEx.Configuration;
using UnityEngine;

namespace PEAK.CustomVoicer.Configuration;

internal static class ModConfig
{
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<KeyCode> VoiceWheelKey = null!;
    internal static ConfigEntry<float> Volume = null!;
    internal static ConfigEntry<bool> NormalizeVoiceClips = null!;
    internal static ConfigEntry<float> TargetRmsDb = null!;
    internal static ConfigEntry<float> StreamVolume = null!;
    internal static ConfigEntry<string> VoicePackFile = null!;

    internal static void Init(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true, "Enable the custom voice wheel.");
        VoiceWheelKey = config.Bind("General", "VoiceWheelKey", KeyCode.Semicolon, "Hold this key to open the voice wheel.");
        Volume = config.Bind("Audio", "Volume", 1f, new ConfigDescription("Playback volume.", new AcceptableValueRange<float>(0f, 1f)));
        NormalizeVoiceClips = config.Bind("Audio", "NormalizeVoiceClips", true, "Normalize loaded voice clips to a target RMS loudness before playback and streaming.");
        TargetRmsDb = config.Bind("Audio", "TargetRmsDb", -18f, new ConfigDescription("Target RMS loudness in dBFS used when NormalizeVoiceClips is enabled.", new AcceptableValueRange<float>(-36f, -6f)));
        StreamVolume = config.Bind("Audio", "StreamVolume", 0.8f, new ConfigDescription("Volume applied to clips sent through Photon Voice.", new AcceptableValueRange<float>(0f, 1f)));
        VoicePackFile = config.Bind("General", "VoicePackFile", "voice_pack.json", "Voice pack JSON file inside the plugin folder.");
    }
}
