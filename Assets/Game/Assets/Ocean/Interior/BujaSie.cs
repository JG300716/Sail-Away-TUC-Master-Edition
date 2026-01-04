using UnityEngine;

public class BoatBob : MonoBehaviour
{
    [Header("Boat Bob Settings")]
    public float maxTilt = 2f;        // maksymalny k¹t w stopniach (±)
    public float speed = 1f;          // prêdkoœæ bujania

    private float xOffset;
    private float zOffset;

    private Vector3 initialRotation;  // pocz¹tkowa rotacja obiektu

    private void Start()
    {
        // zapamiêtaj pocz¹tkow¹ rotacjê
        initialRotation = transform.localEulerAngles;

        // losowe przesuniêcie fazy, ¿eby nie by³o idealnie zsynchronizowane
        xOffset = Random.Range(0f, 10f);
        zOffset = Random.Range(0f, 10f);
    }

    private void Update()
    {
        float xBob = Mathf.Sin(Time.time * speed + xOffset) * maxTilt;
        float zBob = Mathf.Sin(Time.time * speed * 0.7f + zOffset) * maxTilt;

        // dodaj bujanie do pocz¹tkowej rotacji
        transform.localRotation = Quaternion.Euler(
            initialRotation.x + xBob,
            initialRotation.y,
            initialRotation.z + zBob
        );
    }
}