using UnityEngine;
using agora_gaming_rtc;

public class AgoraApiHandlersImpl : IAgoraAPIHandlers
{
    private IRtcEngine mRtcEngine;

    public AgoraApiHandlersImpl(IRtcEngine engine)
    {
        mRtcEngine = engine;
        BindEngineCalls();
    }

    ~AgoraApiHandlersImpl()
    {
        mRtcEngine?.LeaveChannel();
    }

    public void JoinChannelSuccessHandler(string channelName, uint uid, int elapsed)
    {
        string joinSuccessMessage = string.Format("joinChannel callback uid: {0}, channel: {1}, version: {2}", uid, channelName, IRtcEngine.GetSdkVersion());
        Debug_Log(joinSuccessMessage);
        // AgoraPlayerController.instance.OnChannelJoins(uid);
    }

    public void LeaveChannelHandler(RtcStats stats)
    {
        if (this != null)  // because the destructor may call it
        {
            string leaveChannelMessage = string.Format("onLeaveChannel callback duration {0}, tx: {1}, rx: {2}, tx kbps: {3}, rx kbps: {4}", stats.duration, stats.txBytes, stats.rxBytes, stats.txKBitRate, stats.rxKBitRate);
            Debug_Log(leaveChannelMessage);
        }
    }

    public void UserJoinedHandler(uint uid, int elapsed)
    {
        string userJoinedMessage = string.Format("onUserJoined callback uid:{0} elapsed:{1}", uid, elapsed);
        Debug_Log(userJoinedMessage);
        // AgoraPlayerController.instance.AddAgoraPlayer(uid);
    }

    public void UserOfflineHandler(uint uid, USER_OFFLINE_REASON reason)
    {
        string userOfflineMessage = string.Format("onUserOffline callback uid {0} {1}", uid, reason);
        Debug_Log(userOfflineMessage);
    }

    public void VolumeIndicationHandler(AudioVolumeInfo[] speakers, int speakerNumber, int totalVolume)
    {
        if (speakerNumber == 0 || speakers == null)
        {
            Debug_Log(string.Format("onVolumeIndication only local {0}", totalVolume));
        }

        for (int idx = 0; idx < speakerNumber; idx++)
        {
            string volumeIndicationMessage = string.Format("{0} onVolumeIndication {1} {2}", speakerNumber, speakers[idx].uid, speakers[idx].volume);
            Debug_Log(volumeIndicationMessage);
        }
    }

    public void UserMutedHandler(uint uid, bool muted)
    {
        string userMutedMessage = string.Format("onUserMuted callback uid {0} {1}", uid, muted);
        Debug_Log(userMutedMessage);
    }

    public void SDKWarningHandler(int warn, string msg)
    {
        string description = IRtcEngine.GetErrorDescription(warn);
        string warningMessage = string.Format("onWarning callback {0} {1} {2}", warn, msg, description);
        Debug_Log(warningMessage);
    }

    public void SDKErrorHandler(int error, string msg)
    {
        string description = IRtcEngine.GetErrorDescription(error);
        string errorMessage = string.Format("onError callback {0} {1} {2}", error, msg, description);
        Debug_Log(errorMessage);
    }

    public void RtcStatsHandler(RtcStats stats)
    {
        string rtcStatsMessage = string.Format("onRtcStats callback duration {0}, tx: {1}, rx: {2}, tx kbps: {3}, rx kbps: {4}, tx(a) kbps: {5}, rx(a) kbps: {6} users {7}",
            stats.duration, stats.txBytes, stats.rxBytes, stats.txKBitRate, stats.rxKBitRate, stats.txAudioKBitRate, stats.rxAudioKBitRate, stats.users);
        //  Debug.Log(rtcStatsMessage);

        int lengthOfMixingFile = mRtcEngine.GetAudioMixingDuration();
        int currentTs = mRtcEngine.GetAudioMixingCurrentPosition();

        string mixingMessage = string.Format("Mixing File Meta {0}, {1}", lengthOfMixingFile, currentTs);
        //   Debug.Log(mixingMessage);
    }

    public void AudioRouteChangedHandler(AUDIO_ROUTE route)
    {
        string routeMessage = string.Format("onAudioRouteChanged {0}", route);
        Debug_Log(routeMessage);
    }

    public void RequestTokenHandler()
    {
        string requestKeyMessage = string.Format("OnRequestToken");
        Debug_Log(requestKeyMessage);
    }

    public void ConnectionInterruptedHandler()
    {
        string interruptedMessage = string.Format("OnConnectionInterrupted");
        Debug_Log(interruptedMessage);
    }

    public void ConnectionLostHandler()
    {
        string lostMessage = string.Format("OnConnectionLost");
        Debug_Log(lostMessage);
    }

    public void Debug_Log(string text)
    {
        Debug.LogWarning("[Agora] " + text);
    }

    public void ReJoinChannelSuccessHandler(string channelName, uint uid, int elapsed)
    {
        Debug_Log(string.Format("ReJoinChannelSuccessHandler - channelName:{0} uid:{1} elapsed:{2}", channelName, uid, elapsed));
    }

    public void AudioMixingFinishedHandler()
    {
        Debug_Log("AudioMixingFinishedHandler");
    }

    public void OnFirstRemoteVideoDecodedHandler(uint uid, int width, int height, int elapsed)
    {
    }

    public void OnVideoSizeChangedHandler(uint uid, int width, int height, int elapsed)
    {
    }

    public void OnClientRoleChangedHandler(int oldRole, int newRole)
    {
    }

    public void OnUserMuteVideoHandler(uint uid, bool muted)
    {
    }

    public void OnMicrophoneEnabledHandler(bool isEnabled)
    {
    }

    public void OnFirstRemoteAudioFrameHandler(uint userId, int elapsed)
    {
    }

    public void OnFirstLocalAudioFrameHandler(int elapsed)
    {
    }

    public void OnApiExecutedHandler(int err, string api, string result)
    {
    }

    public void OnLastmileQualityHandler(int quality)
    {
    }

    public void OnAudioQualityHandler(uint userId, int quality, ushort delay, ushort lost)
    {
    }

    public void OnStreamInjectedStatusHandler(string url, uint userId, int status)
    {
    }

    public void OnStreamUnpublishedHandler(string url)
    {
    }

    public void OnStreamPublishedHandler(string url, int error)
    {
    }

    public void OnStreamMessageErrorHandler(uint userId, int streamId, int code, int missed, int cached)
    {
    }

    public void OnStreamMessageHandler(uint userId, int streamId, string data, int length)
    {
    }

    public void OnConnectionBannedHandler()
    {
    }

    public void OnNetworkQualityHandler(uint uid, int txQuality, int rxQuality)
    {
    }



    protected void BindEngineCalls()
    {
        mRtcEngine.OnAudioMixingFinished += AudioMixingFinishedHandler;
        mRtcEngine.OnApiExecuted += OnApiExecutedHandler;
        mRtcEngine.OnAudioQuality += OnAudioQualityHandler;
        mRtcEngine.OnAudioRouteChanged += AudioRouteChangedHandler;
        mRtcEngine.OnClientRoleChanged += OnClientRoleChangedHandler;
        mRtcEngine.OnConnectionBanned += OnConnectionBannedHandler;
        mRtcEngine.OnConnectionInterrupted += ConnectionInterruptedHandler;
        mRtcEngine.OnConnectionLost += ConnectionLostHandler;
        mRtcEngine.OnError += SDKErrorHandler;
        mRtcEngine.OnFirstLocalAudioFrame += OnFirstLocalAudioFrameHandler;
        mRtcEngine.OnFirstRemoteAudioFrame += OnFirstRemoteAudioFrameHandler;
        mRtcEngine.OnFirstRemoteVideoDecoded += OnFirstRemoteVideoDecodedHandler;
        mRtcEngine.OnJoinChannelSuccess += JoinChannelSuccessHandler;
        mRtcEngine.OnLastmileQuality += OnLastmileQualityHandler;
        mRtcEngine.OnLeaveChannel += LeaveChannelHandler;
        mRtcEngine.OnMicrophoneEnabled += OnMicrophoneEnabledHandler;
        mRtcEngine.OnNetworkQuality += OnNetworkQualityHandler;
        mRtcEngine.OnReJoinChannelSuccess += ReJoinChannelSuccessHandler;
        mRtcEngine.OnRequestToken += RequestTokenHandler;
        mRtcEngine.OnRtcStats += RtcStatsHandler;

        mRtcEngine.OnStreamInjectedStatus += OnStreamInjectedStatusHandler;
        mRtcEngine.OnStreamMessage += OnStreamMessageHandler;
        mRtcEngine.OnStreamMessageError += OnStreamMessageErrorHandler;
        mRtcEngine.OnStreamPublished += OnStreamPublishedHandler;
        mRtcEngine.OnStreamUnpublished += OnStreamUnpublishedHandler;

        mRtcEngine.OnUserJoined += UserJoinedHandler;
        mRtcEngine.OnUserMuted += OnUserMuteVideoHandler;
        mRtcEngine.OnUserMuteVideo += OnUserMuteVideoHandler;
        mRtcEngine.OnUserOffline += UserOfflineHandler;
        mRtcEngine.OnVideoSizeChanged += OnVideoSizeChangedHandler;
        mRtcEngine.OnVolumeIndication += VolumeIndicationHandler;
        mRtcEngine.OnWarning += SDKWarningHandler;

    }
}
