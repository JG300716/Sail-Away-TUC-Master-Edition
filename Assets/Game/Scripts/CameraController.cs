using UnityEngine;

namespace Game.Scripts
{

    public class CameraController : MonoBehaviour
    {
        public float sensitivity = 2f;
        public float clampAngle = 80f;

        private float rotX;
        private float rotY;

        public Camera boatCamera;

        void Start()
        {
            if (boatCamera == null)
            {
                boatCamera = Camera.main;
            }

            Vector3 rot = boatCamera.transform.localRotation.eulerAngles;
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
            boatCamera.transform.localRotation = localRotation;
        }
    }
}