using System;
using System.Linq;
using HarmonyLib;
using PEAK.CustomVoicer.Configuration;
using PEAK.CustomVoicer.Voice;
using UnityEngine;

namespace PEAK.CustomVoicer.Voice;

public sealed class VoiceWheelController : MonoBehaviour
{
    public static VoiceWheelController? Instance { get; private set; }

    private static readonly System.Reflection.FieldInfo? PageField =
        AccessTools.Field(typeof(EmoteWheel), "page");

    private EmoteWheel? _emoteWheel;
    private WheelBackup? _backup;
    private int _voicePage;

    private sealed class WheelBackup
    {
        public int Pages { get; set; }
        public int Page { get; set; }
        public EmoteWheelData[] Data { get; set; } = Array.Empty<EmoteWheelData>();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!ModConfig.Enabled.Value)
        {
            return;
        }

        if (!VoicePackLoader.Instance.IsLoaded)
        {
            return;
        }

        EnsureEmoteWheel();
        if (_emoteWheel == null)
        {
            return;
        }

        if (ShouldBlockWheel())
        {
            if (VoiceWheelState.WheelVisible)
            {
                CloseWheel();
            }

            return;
        }

        var key = ModConfig.VoiceWheelKey.Value;

        if (Input.GetKeyDown(key) && !VoiceWheelState.WheelVisible && !VoiceWheelState.VanillaWheelActive)
        {
            OpenWheel();
        }

        if (Input.GetKeyUp(key) && VoiceWheelState.WheelVisible)
        {
            CloseWheel();
        }

        if (VoiceWheelState.WheelVisible && Input.GetKeyDown(KeyCode.R))
        {
            CloseWheel();
        }
    }

    public static string GetLabelForIndex(int globalIndex)
    {
        var entry = VoicePackLoader.Instance?.GetEntry(globalIndex);
        return entry?.Label ?? string.Empty;
    }

    private void EnsureEmoteWheel()
    {
        if (_emoteWheel != null)
        {
            return;
        }

        _emoteWheel = UnityEngine.Object.FindFirstObjectByType<EmoteWheel>();
        if (_emoteWheel != null)
        {
            return;
        }

        var guiManager = UnityEngine.Object.FindFirstObjectByType<GUIManager>();
        if (guiManager?.emoteWheel != null)
        {
            _emoteWheel = guiManager.emoteWheel.GetComponent<EmoteWheel>();
        }
    }

    private static bool ShouldBlockWheel()
    {
        if (Character.localCharacter == null)
        {
            return true;
        }

        if (IsGhost(Character.localCharacter))
        {
            return true;
        }

        return false;
    }

    private static bool IsGhost(Character character)
    {
        try
        {
            var deadField = AccessToolsField(character, "dead") ?? AccessToolsField(character, "isDead");
            if (deadField is bool dead && dead)
            {
                return true;
            }

            var ghostField = AccessToolsField(character, "isGhost") ?? AccessToolsField(character, "ghost");
            if (ghostField is bool ghost && ghost)
            {
                return true;
            }

            var data = AccessToolsField(character, "data");
            if (data != null)
            {
                var dataGhost = AccessToolsField(data, "isGhost") ?? AccessToolsField(data, "ghost");
                if (dataGhost is bool dataGhostValue && dataGhostValue)
                {
                    return true;
                }

                var dataDead = AccessToolsField(data, "dead") ?? AccessToolsField(data, "isDead");
                if (dataDead is bool dataDeadValue && dataDeadValue)
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogDebug($"Ghost check failed: {ex.Message}");
        }

        return false;
    }

    private static object? AccessToolsField(object target, string name)
    {
        var field = target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        return field?.GetValue(target);
    }

    private void OpenWheel()
    {
        if (_emoteWheel == null)
        {
            return;
        }

        ConfigureWheelData();
        VoiceWheelState.IsActive = true;
        VoiceWheelState.WheelVisible = true;

        if (_emoteWheel.gameObject.activeSelf)
        {
            _emoteWheel.InitWheel();
        }
        else
        {
            _emoteWheel.gameObject.SetActive(true);
        }
    }

    private void CloseWheel()
    {
        if (_emoteWheel == null)
        {
            VoiceWheelState.Reset();
            return;
        }

        if (_emoteWheel.gameObject.activeSelf)
        {
            _emoteWheel.gameObject.SetActive(false);
        }

        RestoreWheelUi();
        RestoreWheelData();

        VoiceWheelState.Reset();
    }

    private void ConfigureWheelData()
    {
        if (_emoteWheel == null)
        {
            return;
        }

        _backup = new WheelBackup
        {
            Pages = _emoteWheel.pages,
            Page = GetWheelPage(_emoteWheel),
            Data = _emoteWheel.data?.ToArray() ?? Array.Empty<EmoteWheelData>(),
        };

        var entries = VoicePackLoader.Instance.Entries;
        var pageCount = Math.Max(1, (entries.Count + VoiceWheelState.SlicesPerPage - 1) / VoiceWheelState.SlicesPerPage);
        var totalSlots = pageCount * VoiceWheelState.SlicesPerPage;
        _voicePage = Mathf.Clamp(_voicePage, 0, pageCount - 1);

        _emoteWheel.pages = pageCount;
        _emoteWheel.data = new EmoteWheelData[totalSlots];
        SetWheelPage(_emoteWheel, _voicePage);

        for (var i = 0; i < totalSlots; i++)
        {
            if (i < entries.Count)
            {
                var data = ScriptableObject.CreateInstance<EmoteWheelData>();
                data.emoteName = entries[i].Label;
                data.anim = entries[i].Label;
                _emoteWheel.data[i] = data;
            }
            else
            {
                _emoteWheel.data[i] = null!;
            }
        }
    }

    private void RestoreWheelData()
    {
        if (_emoteWheel == null || _backup == null)
        {
            return;
        }

        _emoteWheel.pages = _backup.Pages;
        _emoteWheel.data = _backup.Data;
        SetWheelPage(_emoteWheel, _backup.Page);
        _backup = null;
    }

    public static int GetWheelPage(EmoteWheel wheel)
    {
        return PageField?.GetValue(wheel) is int page ? page : 0;
    }

    public static void SetWheelPage(EmoteWheel wheel, int page)
    {
        PageField?.SetValue(wheel, page);
    }

    public static int GetVoicePage(EmoteWheel wheel)
    {
        var controller = FindControllerFor(wheel);
        return controller?._voicePage ?? 0;
    }

    public static void SaveVoicePage(EmoteWheel wheel, int page)
    {
        var controller = FindControllerFor(wheel);
        if (controller == null)
        {
            return;
        }

        controller._voicePage = Mathf.Clamp(page, 0, Math.Max(0, wheel.pages - 1));
    }

    private static VoiceWheelController? FindControllerFor(EmoteWheel wheel)
    {
        var controller = Instance;
        return controller != null && controller._emoteWheel == wheel ? controller : null;
    }

    private void RestoreWheelUi()
    {
        if (_emoteWheel == null)
        {
            return;
        }

        try
        {
            if (_emoteWheel.selectedEmoteName != null)
            {
                _emoteWheel.selectedEmoteName.text = string.Empty;
            }

            foreach (var slice in _emoteWheel.slices)
            {
                if (slice == null)
                {
                    continue;
                }

                if (slice.image != null)
                {
                    slice.image.gameObject.SetActive(true);
                }

                var label = slice.transform.Find("CustomVoicerLabel");
                if (label != null)
                {
                    Destroy(label.gameObject);
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to restore emote wheel UI: {ex.Message}");
        }
    }
}
