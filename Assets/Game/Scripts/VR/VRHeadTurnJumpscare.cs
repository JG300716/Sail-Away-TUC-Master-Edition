using UnityEngine;

public class VRHeadTurnJumpscare : MonoBehaviour
{
    [Header("References")]
    public Transform head;        // Camera (HMD)
    public GameObject skull;      // Jumpscare object

    [Header("Settings")]
    public float triggerAngle = 90f;
    public float skullDistance = 1.2f;

    private float startYaw;
    private bool triggered = false;

    void Start()
    {
        // zapamiêtaj pocz¹tkowy kierunek patrzenia
        startYaw = GetYaw(head.forward);

        skull.SetActive(false);
    }

    void Update()
    {
        if (triggered) return;

        float currentYaw = GetYaw(head.forward);
        float deltaYaw = Mathf.DeltaAngle(startYaw, currentYaw);

        // gracz obróci³ g³owê w LEWO
        if (deltaYaw <= -triggerAngle)
        {
            SpawnSkull();
            triggered = true;
        }
    }

    void SpawnSkull()
    {
        Debug.Log("JUMPSCARE!");
        skull.SetActive(true);
    }

    float GetYaw(Vector3 dir)
    {
        dir.y = 0;
        return Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
    }
}
