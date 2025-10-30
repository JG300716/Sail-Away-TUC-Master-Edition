using UnityEngine;

public class BoatCameraLook : MonoBehaviour
{
    public float sensitivity = 2f;
    public float clampAngle = 80f;

    private float rotX;
    private float rotY;

    void Start()
    {
        Vector3 rot = transform.localRotation.eulerAngles;
        rotY = rot.y;
        rotX = rot.x;

        Cursor.lockState = CursorLockMode.Locked; // Hide & lock cursor
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        rotY += mouseX * sensitivity;
        rotX -= mouseY * sensitivity;
        rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);

        Quaternion localRotation = Quaternion.Euler(rotX, rotY, 0f);
        transform.localRotation = localRotation;
    }
}