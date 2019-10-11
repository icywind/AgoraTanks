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
    private IAgoraAPIHandlers agoraAPI;

    private AgoraVideoController()
    {
        Debug_Log("Agora IRtcEngine Version : " + IRtcEngine.GetSdkVersion());
        _instance = new AgoraVideoController();
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



    void Debug_Log(string text)
    {
        Debug.LogWarning("[Agora] " + text);
    }
}
