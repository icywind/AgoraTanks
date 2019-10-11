using System;
using agora_gaming_rtc;

public interface IAgoraAPIHandlers
{
      void JoinChannelSuccessHandler(string channelName, uint uid, int elapsed);

      void ReJoinChannelSuccessHandler(string channelName, uint uid, int elapsed);

      void ConnectionLostHandler();

      void ConnectionInterruptedHandler();

      void RequestTokenHandler();

      void UserJoinedHandler(uint uid, int elapsed);

      void UserOfflineHandler(uint uid, USER_OFFLINE_REASON reason);

      void LeaveChannelHandler(RtcStats stats);

      void VolumeIndicationHandler(AudioVolumeInfo[] speakers, int speakerNumber, int totalVolume);

      void UserMutedHandler(uint uid, bool muted);

      void SDKWarningHandler(int warn, string msg);

      void SDKErrorHandler(int error, string msg);

      void RtcStatsHandler(RtcStats stats);

      void AudioMixingFinishedHandler();

      void AudioRouteChangedHandler(AUDIO_ROUTE route);

      void OnFirstRemoteVideoDecodedHandler(uint uid, int width, int height, int elapsed);

      void OnVideoSizeChangedHandler(uint uid, int width, int height, int elapsed);

      void OnClientRoleChangedHandler(int oldRole, int newRole);

      void OnUserMuteVideoHandler(uint uid, bool muted);

      void OnMicrophoneEnabledHandler(bool isEnabled);

      void OnFirstRemoteAudioFrameHandler(uint userId, int elapsed);

      void OnFirstLocalAudioFrameHandler(int elapsed);

      void OnApiExecutedHandler(int err, string api, string result);

      void OnLastmileQualityHandler(int quality);

      void OnAudioQualityHandler(uint userId, int quality, ushort delay, ushort lost);

      void OnStreamInjectedStatusHandler(string url, uint userId, int status);

      void OnStreamUnpublishedHandler(string url);

      void OnStreamPublishedHandler(string url, int error);

      void OnStreamMessageErrorHandler(uint userId, int streamId, int code, int missed, int cached);

      void OnStreamMessageHandler(uint userId, int streamId, string data, int length);

      void OnConnectionBannedHandler();

      void OnNetworkQualityHandler(uint uid, int txQuality, int rxQuality);
}
