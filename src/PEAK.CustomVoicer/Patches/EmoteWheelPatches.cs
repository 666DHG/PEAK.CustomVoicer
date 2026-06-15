using System;
using System.Reflection;
using HarmonyLib;
using PEAK.CustomVoicer.Configuration;
using PEAK.CustomVoicer.Voice;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PEAK.CustomVoicer.Patches;

[HarmonyPatch(typeof(EmoteWheel), "Choose")]
internal static class EmoteWheelChoosePatch
{
    private static bool Prefix(EmoteWheel __instance)
    {
        if (!VoiceWheelState.IsActive || !VoiceWheelState.WheelVisible)
        {
            return true;
        }

        var chosenField = AccessTools.Field(typeof(EmoteWheel), "chosenEmoteData");
        var chosen = chosenField?.GetValue(__instance) as EmoteWheelData;
        if (chosen == null)
        {
            return true;
        }

        var globalIndex = Array.IndexOf(__instance.data, chosen);
        if (globalIndex < 0)
        {
            return true;
        }

        VoicePlayback.PlayFromLocalSelection(globalIndex);
        return false;
    }
}

[HarmonyPatch(typeof(EmoteWheel), "Hover")]
internal static class EmoteWheelHoverPatch
{
    private static bool Prefix(EmoteWheel __instance, EmoteWheelData emoteWheelData)
    {
        if (!VoiceWheelState.IsActive || !VoiceWheelState.WheelVisible)
        {
            return true;
        }

        try
        {
            var chosenField = AccessTools.Field(typeof(EmoteWheel), "chosenEmoteData");
            chosenField?.SetValue(__instance, emoteWheelData);

            if (__instance.selectedEmoteName == null || emoteWheelData == null)
            {
                return false;
            }

            var globalIndex = Array.IndexOf(__instance.data, emoteWheelData);
            var label = VoiceWheelController.GetLabelForIndex(globalIndex);
            __instance.selectedEmoteName.text = label;
            return false;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Voice wheel hover failed: {ex.Message}");
            return true;
        }
    }
}

[HarmonyPatch(typeof(EmoteWheel), "InitWheel")]
internal static class EmoteWheelInitWheelPatch
{
    private static bool Prefix(EmoteWheel __instance)
    {
        if (!VoiceWheelState.IsActive || VoiceWheelState.VanillaWheelActive)
        {
            return true;
        }

        var chosenField = AccessTools.Field(typeof(EmoteWheel), "chosenEmoteData");
        chosenField?.SetValue(__instance, null);

        var slices = __instance.slices;
        var data = __instance.data;
        for (var i = 0; i < slices.Length; i++)
        {
            slices[i].Init(data[i], __instance);
        }

        if (__instance.selectedEmoteName != null)
        {
            __instance.selectedEmoteName.text = string.Empty;
        }

        return false;
    }
}

[HarmonyPatch(typeof(EmoteWheelSlice), "Init")]
internal static class EmoteWheelSliceInitPatch
{
    private const string LabelObjectName = "CustomVoicerLabel";

    private static void Postfix(EmoteWheelSlice __instance, EmoteWheelData data, EmoteWheel wheel)
    {
        if (!VoiceWheelState.IsActive || VoiceWheelState.VanillaWheelActive)
        {
            return;
        }

        try
        {
            if (data == null)
            {
                return;
            }

            var sliceIndex = Array.IndexOf(wheel.slices, __instance);
            if (sliceIndex < 0)
            {
                return;
            }

            var globalIndex = Array.IndexOf(wheel.data, data);
            var label = VoiceWheelController.GetLabelForIndex(globalIndex);
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            var existing = __instance.transform.Find(LabelObjectName);
            if (existing != null)
            {
                return;
            }

            if (__instance.image != null)
            {
                __instance.image.gameObject.SetActive(false);
            }

            var labelObject = new GameObject(LabelObjectName);
            labelObject.transform.SetParent(__instance.transform, false);

            var text = labelObject.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 24f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.outlineColor = Color.black;
            text.outlineWidth = 0.4f;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            var rect = text.GetComponent<RectTransform>();
            if (__instance.image != null)
            {
                var imageRect = __instance.image.GetComponent<RectTransform>();
                rect.anchorMin = imageRect.anchorMin;
                rect.anchorMax = imageRect.anchorMax;
                rect.sizeDelta = imageRect.sizeDelta * 1.15f;
                rect.anchoredPosition = imageRect.anchoredPosition;
                rect.localScale = imageRect.localScale;
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(100f, 40f);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Voice wheel slice init failed: {ex.Message}");
        }
    }
}

[HarmonyPatch(typeof(GUIManager), "UpdateEmoteWheel")]
internal static class GUIManagerUpdateEmoteWheelPatch
{
    private static readonly MethodInfo TabNextMethod =
        AccessTools.Method(typeof(EmoteWheel), "TabNext")!;

    private static readonly MethodInfo TabPrevMethod =
        AccessTools.Method(typeof(EmoteWheel), "TabPrev")!;

    private static void Postfix(GUIManager __instance)
    {
        VoiceWheelState.VanillaWheelActive = __instance.wheelActive;

        if (VoiceWheelState.IsActive && VoiceWheelState.WheelVisible)
        {
            var property = typeof(GUIManager).GetProperty(
                "windowBlockingInput",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            property?.GetSetMethod(nonPublic: true)?.Invoke(__instance, new object[] { true });
            VoiceWheelState.WindowBlockingInput = true;

            if (__instance.emoteWheel.activeSelf)
            {
                var scroll = Input.mouseScrollDelta.y;
                var emoteWheel = __instance.emoteWheel.GetComponent<EmoteWheel>();
                if (scroll < 0f)
                {
                    TabNextMethod.Invoke(emoteWheel, null);
                }
                else if (scroll > 0f)
                {
                    TabPrevMethod.Invoke(emoteWheel, null);
                }
            }
        }
        else
        {
            VoiceWheelState.WindowBlockingInput = __instance.windowBlockingInput;
        }
    }
}
