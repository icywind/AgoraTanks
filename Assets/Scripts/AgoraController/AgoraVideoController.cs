using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using agora_gaming_rtc;

#if (UNITY_ANDROID && UNITY_2018_3_OR_NEWER)
using UnityEngine.Android;
#endif

public class AgoraVideoController 
{
    public static AgoraVideoController instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AgoraVideoController();
            }
            return _instance;
        }
    }

	private static AgoraVideoController _instance;

    private IRtcEngine mRtcEngine;
    private AgoraApiHandlersImpl agoraAPI;

    private AgoraVideoController()
    {
        Debug_Log("Agora IRtcEngine Version : " + IRtcEngine.GetSdkVersion());
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        mRtcEngine = IRtcEngine.GetEngine(Tanks.GameSettings.s_Instance.AgoraAppId);

        Debug.Assert(mRtcEngine != null, "Can not create Agora RTC Engine instance!");
        if (mRtcEngine != null)
        {
            agoraAPI = new AgoraApiHandlersImpl(mRtcEngine);
        }
        Setup();
    }


    private void Setup()
    {

#if (UNITY_ANDROID && UNITY_2018_3_OR_NEWER)
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
        }
#endif
        mRtcEngine.SetLogFilter(LOG_FILTER.DEBUG | LOG_FILTER.INFO | LOG_FILTER.WARNING | LOG_FILTER.ERROR | LOG_FILTER.CRITICAL);
    }

    public void JoinChannel(string channelName, uint playerId = 0)
    {
        mRtcEngine.EnableVideo();
        mRtcEngine.EnableVideoObserver();
        mRtcEngine.JoinChannel(channelName, "extra", playerId); // join the channel with given match name
        Debug_Log(playerId.ToString() + " joining channel:" + channelName);
    }

    public void LeaveChannel()
    {
        Debug_Log("Leaving channel now....");
        mRtcEngine.DisableVideo();
        mRtcEngine.DisableVideoObserver();
        mRtcEngine.LeaveChannel();
    }
    
    
    void Debug_Log(string text)
    {
        Debug.LogWarning("[Agora] " + text);
    }
}
