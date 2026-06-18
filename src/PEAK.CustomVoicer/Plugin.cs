using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PEAK.CustomVoicer.Configuration;
using PEAK.CustomVoicer.Networking;
using PEAK.CustomVoicer.Voice;
using UnityEngine;

namespace PEAK.CustomVoicer;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string PLUGIN_GUID = "com.paradoxyz.peak.customvoicer";
    public const string PLUGIN_NAME = "PEAK CustomVoicer";
    public const string PLUGIN_VERSION = GeneratedPluginVersion.Value;

    internal static ManualLogSource Log = null!;
    internal static Plugin Instance = null!;

    private Harmony? _harmony;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        ModConfig.Init(Config);

        var bootstrap = new GameObject("PEAK.CustomVoicer_Bootstrap");
        DontDestroyOnLoad(bootstrap);
        bootstrap.AddComponent<VoicePackLoader>();
        bootstrap.AddComponent<VoiceWheelController>();
        bootstrap.AddComponent<CustomVoiceStreamer>();

        _harmony = new Harmony(PLUGIN_GUID);
        _harmony.PatchAll();

        Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded.");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
