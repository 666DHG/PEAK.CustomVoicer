using System;
using System.Collections;
using System.Reflection;
using Photon.Voice;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using UnityEngine;

namespace PEAK.CustomVoicer.Networking;

/// <summary>
/// Streams custom clips through a dedicated second Photon Voice Recorder so the primary mic recorder is untouched.
/// </summary>
public sealed class CustomVoiceStreamer : MonoBehaviour
{
    public static CustomVoiceStreamer Instance { get; private set; } = null!;

    private GameObject? _streamObject;
    private Recorder? _streamRecorder;
    private Recorder? _primaryStreamRecorder;
    private RecorderBackup? _primaryBackup;
    private VoiceClipMixProcessor? _mixProcessor;
    private Coroutine? _stopRoutine;

    private sealed class RecorderBackup
    {
        public Recorder.InputSourceType SourceType { get; set; }
        public AudioClip? AudioClip { get; set; }
        public bool LoopAudioClip { get; set; }
        public System.Func<Photon.Voice.IAudioDesc>? InputFactory { get; set; }
        public bool VoiceDetection { get; set; }
        public bool TransmitEnabled { get; set; }
        public bool RecordingEnabled { get; set; }
    }

    private static readonly MethodInfo? AddFloatPostProcessorMethod =
        typeof(LocalVoiceAudioFloat).GetMethod(nameof(LocalVoiceAudioFloat.AddPostProcessor));

    private static readonly MethodInfo? RemoveFloatProcessorMethod =
        typeof(LocalVoiceAudioFloat).GetMethod(nameof(LocalVoiceAudioFloat.RemoveProcessor));

    private void Awake()
    {
        Instance = this;
    }

    public bool TryStream(Character character, AudioClip clip, string? subtitle)
    {
        if (clip == null || character == null)
        {
            return false;
        }

        if (!VoiceRecorderResolver.IsVoiceReady())
        {
            Plugin.Log.LogDebug("Photon Voice not ready; falling back to local playback only.");
            return false;
        }

        var client = VoiceRecorderResolver.GetVoiceClient();
        if (client == null)
        {
            Plugin.Log.LogWarning("PunVoiceClient not found.");
            return false;
        }

        StopCurrentStream();

        var primaryRecorder = VoiceRecorderResolver.GetPrimaryRecorder();
        if (primaryRecorder != null && TryMixViaPrimaryRecorder(primaryRecorder, clip))
        {
            Plugin.Log.LogInfo(
                $"Mixing '{clip.name}' into primary Photon Voice recorder ({clip.length:0.0}s, userData={primaryRecorder.UserData ?? "null"}, group={primaryRecorder.InterestGroup}, targets={FormatTargets(primaryRecorder.TargetPlayers)}).");
            _stopRoutine = StartCoroutine(StopAfterClip(clip.length + 0.2f));
            return true;
        }

        if (primaryRecorder != null)
        {
            StreamViaPrimaryRecorder(primaryRecorder, clip);
            Plugin.Log.LogInfo(
                $"Streaming '{clip.name}' via primary Photon Voice recorder fallback ({clip.length:0.0}s, userData={primaryRecorder.UserData ?? "null"}, group={primaryRecorder.InterestGroup}, targets={FormatTargets(primaryRecorder.TargetPlayers)}).");
            _stopRoutine = StartCoroutine(StopAfterClip(clip.length + 0.2f));
            return true;
        }

        _streamObject = new GameObject("PEAK.CustomVoicer_StreamRecorder");
        _streamObject.transform.SetParent(character.transform, false);

        _streamRecorder = _streamObject.AddComponent<Recorder>();
        ConfigureStreamRecorder(_streamRecorder, clip, character, primaryRecorder);

        client.AddRecorder(_streamRecorder);
        _streamRecorder.RecordingEnabled = true;
        _streamRecorder.TransmitEnabled = true;

        Plugin.Log.LogInfo(
            $"Streaming '{clip.name}' via secondary Photon Voice recorder ({clip.length:0.0}s, userData={_streamRecorder.UserData ?? "null"}, group={_streamRecorder.InterestGroup}, targets={FormatTargets(_streamRecorder.TargetPlayers)}).");
        _stopRoutine = StartCoroutine(StopAfterClip(clip.length + 0.2f));
        return true;
    }

    public void StopCurrentStream()
    {
        if (_stopRoutine != null)
        {
            StopCoroutine(_stopRoutine);
            _stopRoutine = null;
        }

        if (_streamRecorder != null)
        {
            var client = VoiceRecorderResolver.GetVoiceClient();
            _streamRecorder.TransmitEnabled = false;
            _streamRecorder.RecordingEnabled = false;
            client?.RemoveRecorder(_streamRecorder);
            _streamRecorder = null;
        }

        RestorePrimaryRecorder();
        RemovePrimaryMixProcessor();

        if (_streamObject != null)
        {
            Destroy(_streamObject);
            _streamObject = null;
        }
    }

    private static void ConfigureStreamRecorder(Recorder recorder, AudioClip clip, Character character, Recorder? primaryRecorder)
    {
        recorder.SourceType = Recorder.InputSourceType.AudioClip;
        recorder.AudioClip = clip;
        recorder.LoopAudioClip = false;
        recorder.UserData = VoiceRecorderResolver.GetStreamUserData(character);
        recorder.VoiceDetection = false;
        recorder.DebugEchoMode = false;
        recorder.TransmitEnabled = false;
        recorder.RecordingEnabled = false;

        if (primaryRecorder == null)
        {
            Plugin.Log.LogWarning("Primary Photon Voice recorder not found; custom stream will use default voice routing settings.");
            return;
        }

        recorder.InterestGroup = primaryRecorder.InterestGroup;

        var targetPlayers = primaryRecorder.TargetPlayers;
        if (targetPlayers != null && targetPlayers.Length > 0)
        {
            recorder.TargetPlayers = targetPlayers;
        }

        recorder.ReliableMode = primaryRecorder.ReliableMode;
        recorder.Encrypt = primaryRecorder.Encrypt;
        recorder.SamplingRate = primaryRecorder.SamplingRate;
        recorder.FrameDuration = primaryRecorder.FrameDuration;
        recorder.Bitrate = primaryRecorder.Bitrate;
    }

    private bool TryMixViaPrimaryRecorder(Recorder recorder, AudioClip clip)
    {
        var voiceAudio = GetVoiceAudio(recorder) as LocalVoiceAudioFloat;
        if (voiceAudio == null || AddFloatPostProcessorMethod == null)
        {
            Plugin.Log.LogWarning("Primary Photon Voice recorder does not expose a float LocalVoiceAudio; falling back to source switch.");
            return false;
        }

        _primaryStreamRecorder = recorder;
        _primaryBackup = new RecorderBackup
        {
            SourceType = recorder.SourceType,
            AudioClip = recorder.AudioClip,
            LoopAudioClip = recorder.LoopAudioClip,
            InputFactory = recorder.InputFactory,
            VoiceDetection = recorder.VoiceDetection,
            TransmitEnabled = recorder.TransmitEnabled,
            RecordingEnabled = recorder.RecordingEnabled,
        };

        _mixProcessor = new VoiceClipMixProcessor(
            clip,
            Math.Max(1, voiceAudio.Info.Channels),
            Math.Max(1, voiceAudio.Info.SamplingRate),
            1f);

        recorder.VoiceDetection = false;
        recorder.TransmitEnabled = true;
        recorder.RecordingEnabled = true;
        AddFloatPostProcessorMethod.Invoke(voiceAudio, new object[] { new IProcessor<float>[] { _mixProcessor } });
        return true;
    }

    private void StreamViaPrimaryRecorder(Recorder recorder, AudioClip clip)
    {
        _primaryStreamRecorder = recorder;
        _primaryBackup = new RecorderBackup
        {
            SourceType = recorder.SourceType,
            AudioClip = recorder.AudioClip,
            LoopAudioClip = recorder.LoopAudioClip,
            InputFactory = recorder.InputFactory,
            VoiceDetection = recorder.VoiceDetection,
            TransmitEnabled = recorder.TransmitEnabled,
            RecordingEnabled = recorder.RecordingEnabled,
        };

        recorder.RecordingEnabled = false;
        recorder.SourceType = Recorder.InputSourceType.AudioClip;
        recorder.AudioClip = clip;
        recorder.LoopAudioClip = false;
        recorder.VoiceDetection = false;
        recorder.TransmitEnabled = true;
        recorder.RecordingEnabled = true;
    }

    private void RestorePrimaryRecorder()
    {
        if (_primaryStreamRecorder == null || _primaryBackup == null)
        {
            return;
        }

        var recorder = _primaryStreamRecorder;
        var backup = _primaryBackup;
        _primaryStreamRecorder = null;
        _primaryBackup = null;

        recorder.RecordingEnabled = false;
        recorder.SourceType = backup.SourceType;
        recorder.AudioClip = backup.AudioClip;
        recorder.LoopAudioClip = backup.LoopAudioClip;
        recorder.InputFactory = backup.InputFactory;
        recorder.VoiceDetection = backup.VoiceDetection;
        recorder.TransmitEnabled = backup.TransmitEnabled;
        recorder.RecordingEnabled = backup.RecordingEnabled;
    }

    private void RemovePrimaryMixProcessor()
    {
        if (_primaryStreamRecorder == null || _primaryBackup == null || _mixProcessor == null)
        {
            return;
        }

        var recorder = _primaryStreamRecorder;
        var backup = _primaryBackup;
        var processor = _mixProcessor;
        _primaryStreamRecorder = null;
        _primaryBackup = null;
        _mixProcessor = null;

        var voiceAudio = GetVoiceAudio(recorder) as LocalVoiceAudioFloat;
        if (voiceAudio != null && RemoveFloatProcessorMethod != null)
        {
            RemoveFloatProcessorMethod.Invoke(voiceAudio, new object[] { new IProcessor<float>[] { processor } });
        }

        recorder.VoiceDetection = backup.VoiceDetection;
        recorder.TransmitEnabled = backup.TransmitEnabled;
        recorder.RecordingEnabled = backup.RecordingEnabled;
    }

    private static ILocalVoiceAudio? GetVoiceAudio(Recorder recorder)
    {
        var property = typeof(Recorder).GetProperty(
            "voiceAudio",
            BindingFlags.Instance | BindingFlags.NonPublic);

        return property?.GetValue(recorder) as ILocalVoiceAudio;
    }

    private static string FormatTargets(int[]? targetPlayers)
    {
        if (targetPlayers == null)
        {
            return "all";
        }

        return targetPlayers.Length == 0 ? "all" : string.Join(",", targetPlayers);
    }

    private IEnumerator StopAfterClip(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        StopCurrentStream();
        _stopRoutine = null;
    }

    private void OnDestroy()
    {
        StopCurrentStream();
    }
}
