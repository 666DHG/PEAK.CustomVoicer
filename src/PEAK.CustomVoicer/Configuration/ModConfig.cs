using BepInEx.Configuration;
using UnityEngine;

namespace PEAK.CustomVoicer.Configuration;

internal static class ModConfig
{
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<KeyCode> VoiceWheelKey = null!;
    internal static ConfigEntry<float> Volume = null!;
    internal static ConfigEntry<string> VoicePackFile = null!;

    internal static void Init(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true, "Enable the custom voice wheel.");
        VoiceWheelKey = config.Bind("General", "VoiceWheelKey", KeyCode.Semicolon, "Hold this key to open the voice wheel.");
        Volume = config.Bind("Audio", "Volume", 1f, new ConfigDescription("Playback volume.", new AcceptableValueRange<float>(0f, 1f)));
        VoicePackFile = config.Bind("General", "VoicePackFile", "voice_pack.json", "Voice pack JSON file inside the plugin folder.");
    }
}
