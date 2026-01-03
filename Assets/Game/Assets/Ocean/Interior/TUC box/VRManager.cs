using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

public class VRManager : MonoBehaviour
{
    public GameObject xrOrigin;
    public GameObject flatPlayer;

    void Awake()
    {
        UpdateMode();
    }

    void Update()
    {
        // Reaguje na roz³¹czenie w trakcie gry
        if (xrOrigin.activeSelf && !XRSettings.isDeviceActive)
        {
            SwitchToFlat();
        }
    }

    void UpdateMode()
    {
        if (IsVRReady())
            SwitchToVR();
        else
            SwitchToFlat();
    }

    bool IsVRReady()
    {
        var xrManager = XRGeneralSettings.Instance?.Manager;
        return xrManager != null &&
               xrManager.isInitializationComplete &&
               XRSettings.isDeviceActive;
    }

    void SwitchToVR()
    {
        xrOrigin.SetActive(true);
        flatPlayer.SetActive(false);
        Debug.Log("VR MODE");
    }

    void SwitchToFlat()
    {
        xrOrigin.SetActive(false);
        flatPlayer.SetActive(true);
        Debug.Log("FLAT MODE");
    }
}