using UnityEngine;

namespace Game.Scripts.Controllers
{

    public class CameraController : ControllerInterface
    {
        public float sensitivity = 2f;
        public float clampAngle = 80f;

        private float rotX;
        private float rotY;

        public Camera boatCamera;

        public override void Initialize()
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
        public override void EnableController()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        public override void DisableController()
        {
            Cursor.lockState = CursorLockMode.None;
        }
        public override void UpdateController()
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