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
    private static TMP_FontAsset? _fallbackFont;

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

            if (__instance.image != null)
            {
                __instance.image.gameObject.SetActive(false);
            }

            var existing = __instance.transform.Find(LabelObjectName);
            var labelObject = existing != null ? existing.gameObject : new GameObject(LabelObjectName);
            if (existing == null)
            {
                labelObject.transform.SetParent(__instance.transform, false);
            }

            var text = labelObject.GetComponent<TextMeshProUGUI>() ?? labelObject.AddComponent<TextMeshProUGUI>();
            text.text = label;
            ApplyGameTextStyle(text, wheel, label);
            text.fontSize = 34f;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 38f;
            text.enableAutoSizing = true;
            text.color = new Color(0.12f, 0.10f, 0.08f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            text.outlineColor = new Color(1f, 0.96f, 0.82f, 1f);
            text.outlineWidth = 0.24f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;

            var rect = text.GetComponent<RectTransform>();
            if (__instance.image != null)
            {
                var imageRect = __instance.image.GetComponent<RectTransform>();
                rect.anchorMin = imageRect.anchorMin;
                rect.anchorMax = imageRect.anchorMax;
                rect.sizeDelta = imageRect.sizeDelta * 1.45f;
                rect.anchoredPosition = imageRect.anchoredPosition;
                rect.localScale = imageRect.localScale;
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(140f, 56f);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Voice wheel slice init failed: {ex.Message}");
        }
    }

    private static void ApplyGameTextStyle(TextMeshProUGUI text, EmoteWheel wheel, string label)
    {
        var template = wheel.selectedEmoteName;
        var font = ResolveFont(template?.font, label);
        if (font != null)
        {
            text.font = font;
        }

        if (template?.fontSharedMaterial != null && text.font == template.font)
        {
            text.fontSharedMaterial = template.fontSharedMaterial;
        }
    }

    private static TMP_FontAsset? ResolveFont(TMP_FontAsset? preferred, string label)
    {
        if (CanRender(preferred, label))
        {
            return preferred;
        }

        if (CanRender(_fallbackFont, label))
        {
            return _fallbackFont;
        }

        foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (CanRender(font, label))
            {
                _fallbackFont = font;
                Plugin.Log.LogInfo($"Using TMP font '{font.name}' for custom voice labels.");
                return font;
            }
        }

        Plugin.Log.LogWarning($"No loaded TMP font can render label '{label}'.");
        return preferred;
    }

    private static bool CanRender(TMP_FontAsset? font, string text)
    {
        if (font == null || string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                continue;
            }

            if (!font.HasCharacter(ch, searchFallbacks: true, tryAddCharacter: true))
            {
                return false;
            }
        }

        return true;
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
