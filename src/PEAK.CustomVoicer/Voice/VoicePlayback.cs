using System.Collections.Generic;
using PEAK.CustomVoicer.Configuration;
using PEAK.CustomVoicer.Networking;
using UnityEngine;

namespace PEAK.CustomVoicer.Voice;

public static class VoicePlayback
{
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

        PlayLocalOnly(character, entry.Clip, entry.Subtitle);

        var streamed = CustomVoiceStreamer.Instance != null &&
                       CustomVoiceStreamer.Instance.TryStream(character, entry.Clip, entry.Subtitle);

        if (!streamed)
        {
            Plugin.Log.LogDebug($"Played '{entry.Clip.name}' locally only.");
        }
    }

    private static void PlayLocalOnly(Character character, AudioClip clip, string? subtitle)
    {
        var source = GetOrCreateSource(character);
        source.volume = ModConfig.Volume.Value;
        source.spatialBlend = 1f;
        source.minDistance = 2f;
        source.maxDistance = 40f;
        source.rolloffMode = AudioRolloffMode.Linear;
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

        var source = character.GetComponent<AudioSource>();
        if (source == null)
        {
            source = character.gameObject.AddComponent<AudioSource>();
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
