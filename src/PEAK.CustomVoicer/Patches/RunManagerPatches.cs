using HarmonyLib;
using PEAK.CustomVoicer.Networking;
using PEAK.CustomVoicer.Voice;

namespace PEAK.CustomVoicer.Patches;

[HarmonyPatch(typeof(RunManager), "Start")]
internal static class RunManagerStartPatch
{
    private static void Postfix()
    {
        CustomVoiceStreamer.Instance?.StopCurrentStream();

        if (VoiceWheelState.WheelVisible)
        {
            VoiceWheelState.Reset();
        }
    }
}
