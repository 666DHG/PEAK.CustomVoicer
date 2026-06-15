using System.Collections.Generic;
using PEAK.CustomVoicer.Configuration;
using PEAK.CustomVoicer.Networking;
using UnityEngine;

namespace PEAK.CustomVoicer.Voice;

public static class VoicePlayback
{
    private const string LocalMonitorObjectName = "PEAK.CustomVoicer_LocalMonitor";

    private static readonly Dictionary<int, AudioSource> SourcesByActor = new();

    public static void PlayFromLocalSelection(int globalIndex)
    {
        if (!ModConfig.Enabled.Value)
        {
            return;
        }

        var character = Character.localCharacter;
        if (character == null)
        {
            Plugin.Log.LogWarning("Cannot play voice: no local character.");
            return;
        }

        var entry = VoicePackLoader.Instance?.GetEntry(globalIndex);
        if (entry?.Clip == null)
        {
            Plugin.Log.LogWarning($"No clip for voice index {globalIndex}.");
            return;
        }

        PlayLocalMonitor(character, entry.Clip, entry.Subtitle);

        var streamed = CustomVoiceStreamer.Instance != null &&
                       CustomVoiceStreamer.Instance.TryStream(character, entry.Clip, entry.Subtitle);

        if (!streamed)
        {
            Plugin.Log.LogDebug($"Played '{entry.Clip.name}' locally without Photon streaming.");
        }
    }

    private static void PlayLocalMonitor(Character character, AudioClip clip, string? subtitle)
    {
        var source = GetOrCreateSource(character);
        source.volume = ModConfig.Volume.Value;
        source.spatialBlend = 0f;
        source.panStereo = 0f;
        source.PlayOneShot(clip);

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            Plugin.Log.LogInfo($"[CustomVoicer] {subtitle}");
        }
    }

    private static AudioSource GetOrCreateSource(Character character)
    {
        var actor = GetActorNumber(character);
        if (actor >= 0 && SourcesByActor.TryGetValue(actor, out var existing) && existing != null)
        {
            return existing;
        }

        var sourceTransform = character.transform.Find(LocalMonitorObjectName);
        var source = sourceTransform != null ? sourceTransform.GetComponent<AudioSource>() : null;
        if (source == null)
        {
            var sourceObject = sourceTransform != null
                ? sourceTransform.gameObject
                : new GameObject(LocalMonitorObjectName);

            sourceObject.transform.SetParent(character.transform, false);
            source = sourceObject.GetComponent<AudioSource>() ?? sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
        }

        if (actor >= 0)
        {
            SourcesByActor[actor] = source;
        }

        return source;
    }

    private static int GetActorNumber(Character character)
    {
        var photonView = character.GetComponent<Photon.Pun.PhotonView>();
        return photonView?.OwnerActorNr ?? -1;
    }
}
