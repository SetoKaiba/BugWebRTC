#if !UNITY_WEBGL
using System;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;

internal static class WebRTCSettings
{
    public const int DefaultStreamWidth = 1280;
    public const int DefaultStreamHeight = 720;

    private static Vector2Int s_StreamSize = new Vector2Int(DefaultStreamWidth, DefaultStreamHeight);
    private static RTCRtpCodecCapability s_useVideoCodec = null;

    public static Vector2Int StreamSize
    {
        get { return s_StreamSize; }
        set { s_StreamSize = value; }
    }

    public static RTCRtpCodecCapability UseVideoCodec
    {
        get { return s_useVideoCodec; }
        set { s_useVideoCodec = value; }
    }
}

public class WhipWhep : MonoBehaviour
{
    public string url;

    public RTCPeerConnection pc;
    public MediaStream stream;

    void Start()
    {
        StartCoroutine(WebRTC.Update());
    }

    public void Publish(MediaStream mediaStream)
    {
        Connect(pc =>
        {
            var init = new RTCRtpTransceiverInit
            {
                direction = RTCRtpTransceiverDirection.SendOnly
            };
            pc.AddTransceiver(TrackKind.Audio, init);
            pc.AddTransceiver(TrackKind.Video, init);

            if (mediaStream == null) return;
            foreach (var track in mediaStream.GetTracks())
            {
                this.pc.AddTrack(track);
                OnTrack(track);
            }
        });
    }

    public void Play()
    {
        Connect(pc =>
        {
            var init = new RTCRtpTransceiverInit
            {
                direction = RTCRtpTransceiverDirection.RecvOnly
            };
            pc.AddTransceiver(TrackKind.Audio, init);
            pc.AddTransceiver(TrackKind.Video, init);
        });
    }

    private void Connect(Action<RTCPeerConnection> init)
    {
        if (pc != null) Close();
        pc = new RTCPeerConnection();
        stream = new MediaStream();
        init(pc);
        pc.OnIceCandidate = OnIceCandidate;
        pc.OnIceConnectionChange = OnIceConnectionChange;
        pc.OnNegotiationNeeded = OnNegotiationNeeded;
        pc.OnTrack = OnTrack;
    }

    private void OnTrack(RTCTrackEvent e)
    {
        OnTrack(e.Track);
    }

    private void OnTrack(MediaStreamTrack track)
    {
        stream.AddTrack(track);
    }

    private void OnNegotiationNeeded()
    {
        StartCoroutine(PeerNegotiationNeeded());
    }

    private IEnumerator PeerNegotiationNeeded()
    {
        var op = pc.CreateOffer();
        yield return op;

        if (!op.IsError)
        {
            if (pc.SignalingState != RTCSignalingState.Stable)
            {
                Debug.LogError("signaling state is not stable.");
                yield break;
            }

            yield return StartCoroutine(OnCreateOfferSuccess(pc, op.Desc));
        }
        else
        {
            OnCreateSessionDescriptionError(op.Error);
        }
    }

    private IEnumerator OnCreateOfferSuccess(RTCPeerConnection pc, RTCSessionDescription desc)
    {
        Debug.Log("Offer from local");
        Debug.Log("SetLocalDescription start");
        var op = pc.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            OnSetLocalSuccess();
        }
        else
        {
            var error = op.Error;
            OnSetSessionDescriptionError(ref error);
        }

        Debug.Log(desc.sdp);
        ExchangeSdp(desc.sdp);
    }

    private void ExchangeSdp(string sdp)
    {
        Debug.Log("ExchangeSdp start");
        RestUtils.SendWebRequestText(url, result =>
            {
                StartCoroutine(OnGotAnswerSuccess(result));
                Debug.Log("ExchangeSdp complete");
            }, OnExchangeSdpError, sdp,
            contentType: "application/sdp");
    }

    private void OnIceConnectionChange(RTCIceConnectionState state)
    {
        Debug.Log($"IceConnectionState: {state}");
    }

    private void OnIceCandidate(RTCIceCandidate candidate)
    {
        Debug.Log($"ICE candidate:\n {candidate.Candidate}");
    }

    private void OnSetLocalSuccess()
    {
        Debug.Log("SetLocalDescription complete");
    }

    private void OnSetRemoteSuccess()
    {
        Debug.Log("SetRemoteDescription complete");
    }

    private IEnumerator OnGotAnswerSuccess(string sdp)
    {
        var desc = new RTCSessionDescription
        {
            type = RTCSdpType.Answer,
            sdp = sdp
        };
        Debug.Log("SetRemoteDescription start");
        var op = pc.SetRemoteDescription(ref desc);
        yield return op;
        if (!op.IsError)
        {
            OnSetRemoteSuccess();
        }
        else
        {
            var error = op.Error;
            OnSetSessionDescriptionError(ref error);
        }
    }

    private static void OnCreateSessionDescriptionError(RTCError error)
    {
        Debug.LogError($"Error Detail Type: {error.message}");
    }

    static void OnSetSessionDescriptionError(ref RTCError error)
    {
        Debug.LogError($"Error Detail Type: {error.message}");
    }

    static void OnExchangeSdpError(string url, long responseCode, string error)
    {
        Debug.LogError(url);
        Debug.LogError(responseCode);
        Debug.LogError(error);
    }

    public void Close()
    {
        pc.Close();
        pc.Dispose();
        foreach (var mediaStreamTrack in stream.GetTracks())
        {
            mediaStreamTrack.Stop();
        }

        stream.Dispose();
        pc = null;
        stream = null;
    }
}
#endif
