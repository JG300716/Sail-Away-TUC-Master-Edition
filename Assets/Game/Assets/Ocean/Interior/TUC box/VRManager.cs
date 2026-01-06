using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections;

public class VRManager : MonoBehaviour
{
    public GameObject xrOrigin;
    public GameObject flatPlayer;

    void Start()
    {
        StartCoroutine(InitXRMode());
    }

    IEnumerator InitXRMode()
    {
        // Poczekaj a¿ XR Management wystartuje
        var xrManager = XRGeneralSettings.Instance.Manager;

        if (xrManager == null)
        {
            SwitchToFlat();
            yield break;
        }

        // Czekamy a¿ XR siê zainicjalizuje
        while (!xrManager.isInitializationComplete)
            yield return null;

        // Czekamy a¿ headset stanie siê aktywny
        while (!XRSettings.isDeviceActive)
            yield return null;

        SwitchToVR();
    }

    void Update()
    {
        // Reaguje na roz³¹czenie w trakcie gry
        if (xrOrigin.activeSelf && !XRSettings.isDeviceActive)
        {
            SwitchToFlat();
        }
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
