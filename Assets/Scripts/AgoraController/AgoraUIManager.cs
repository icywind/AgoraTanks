using UnityEngine;
using UnityEngine.UI;

public class AgoraUIManager : MonoBehaviour
{
    void Start()
    {
        SetPosition();
    }

    void SetPosition()
    {
        transform.position = new Vector3(Screen.width - 20, Screen.height - 100, 0);
    }

    public void OnMicToggle(Toggle toggle)
    {
        Debug.Log("Toggle mic " + toggle.isOn);
        AgoraVideoController.instance.MuteMic(!toggle.isOn);
    }
    
    public void OnCamToggle(Toggle toggle)
    {
        Debug.Log("Toggle cam" + toggle.isOn);
        AgoraVideoController.instance.MuteCamera(!toggle.isOn);
    }
    
    public void OnCamSwitch(Toggle toggle)
    {
        Debug.Log("Switch cam " + toggle.isOn);
        AgoraVideoController.instance.SwitchCamera();
    }
}
