using System.Collections;
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
    private Coroutine? _stopRoutine;

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

        _streamObject = new GameObject("PEAK.CustomVoicer_StreamRecorder");
        _streamObject.transform.SetParent(character.transform, false);

        _streamRecorder = _streamObject.AddComponent<Recorder>();
        var primaryRecorder = VoiceRecorderResolver.GetPrimaryRecorder();
        ConfigureStreamRecorder(_streamRecorder, clip, character, primaryRecorder);

        client.AddRecorder(_streamRecorder);
        _streamRecorder.RecordingEnabled = true;
        _streamRecorder.TransmitEnabled = true;

        Plugin.Log.LogInfo(
            $"Streaming '{clip.name}' via secondary Photon Voice recorder ({clip.length:0.0}s, userData={_streamRecorder.UserData ?? "null"}).");
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
        recorder.TargetPlayers = primaryRecorder.TargetPlayers;
        recorder.ReliableMode = primaryRecorder.ReliableMode;
        recorder.Encrypt = primaryRecorder.Encrypt;
        recorder.SamplingRate = primaryRecorder.SamplingRate;
        recorder.FrameDuration = primaryRecorder.FrameDuration;
        recorder.Bitrate = primaryRecorder.Bitrate;
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
