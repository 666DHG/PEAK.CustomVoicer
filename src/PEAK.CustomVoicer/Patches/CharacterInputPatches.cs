using HarmonyLib;
using PEAK.CustomVoicer.Voice;

namespace PEAK.CustomVoicer.Patches;

[HarmonyPatch(typeof(CharacterInput), "Sample")]
internal static class CharacterInputSamplePatch
{
    private static bool Prefix(CharacterInput __instance)
    {
        if (!VoiceWheelState.IsActive || !VoiceWheelState.WheelVisible)
        {
            return true;
        }

        AccessTools.Method(typeof(CharacterInput), "ResetInput")?.Invoke(__instance, null);
        return false;
    }
}
