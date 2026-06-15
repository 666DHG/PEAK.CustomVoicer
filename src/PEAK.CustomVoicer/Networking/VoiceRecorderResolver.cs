using Photon.Pun;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using UnityEngine;

namespace PEAK.CustomVoicer.Networking;

internal static class VoiceRecorderResolver
{
    public static PunVoiceClient? GetVoiceClient()
    {
        return UnityEngine.Object.FindFirstObjectByType<PunVoiceClient>();
    }

    public static PhotonVoiceView? GetLocalVoiceView()
    {
        var character = Character.localCharacter;
        if (character == null)
        {
            return null;
        }

        return character.GetComponent<PhotonVoiceView>() ??
               character.GetComponentInChildren<PhotonVoiceView>(true);
    }

    public static Recorder? GetPrimaryRecorder()
    {
        var voiceViewRecorder = GetLocalVoiceView()?.RecorderInUse;
        if (voiceViewRecorder != null)
        {
            return voiceViewRecorder;
        }

        return GetVoiceClient()?.PrimaryRecorder;
    }

    public static object? GetStreamUserData(Character character)
    {
        var primaryRecorderUserData = GetPrimaryRecorder()?.UserData;
        if (primaryRecorderUserData != null)
        {
            return primaryRecorderUserData;
        }

        var voiceView = GetLocalVoiceView();
        var voiceViewPhoton = voiceView != null ? voiceView.GetComponent<PhotonView>() : null;
        if (voiceViewPhoton != null)
        {
            return voiceViewPhoton.ViewID;
        }

        var view = character.GetComponent<PhotonView>();
        return view != null ? (object)view.ViewID : character.gameObject.GetInstanceID();
    }

    public static bool IsVoiceReady()
    {
        if (!PhotonNetwork.InRoom)
        {
            return false;
        }

        var client = GetVoiceClient();
        return client != null && client.Client != null && client.Client.InRoom;
    }
}
