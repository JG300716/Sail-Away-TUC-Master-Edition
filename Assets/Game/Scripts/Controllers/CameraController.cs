using UnityEngine;
using Game.Scripts.Interface;
using Unity.VisualScripting;

namespace Game.Scripts.Controllers
{

    public class CameraController : ControllerInterface
    {
        public float sensitivity = 2f;
        public float clampAngle = 80f;

        private float rotX;
        private float rotY;
        
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera topCamera;

        private Camera currentCamera;

        public override void Initialize()
        {
            if (mainCamera.IsUnityNull() || topCamera.IsUnityNull())
                throw new System.Exception("Cameras not assigned in CameraController.");

            currentCamera = mainCamera;
            Vector3 rot = currentCamera.transform.localRotation.eulerAngles;
            rotY = rot.y;
            rotX = rot.x;

            Cursor.lockState = CursorLockMode.Locked; // Hide & lock cursor

        }
        public override void EnableController()
        {
            if (mainCamera.IsUnityNull() || topCamera.IsUnityNull())
                throw new System.Exception("Cameras not assigned in CameraController.");
            currentCamera = mainCamera;
            mainCamera.enabled = true;
            topCamera.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        public override void DisableController()
        {
            if (mainCamera.IsUnityNull() || topCamera.IsUnityNull())
                throw new System.Exception("Cameras not assigned in CameraController.");
            mainCamera.enabled = false;
            topCamera.enabled = false;
            Cursor.lockState = CursorLockMode.None;
        }
        public override void UpdateController()
        {
            if (currentCamera.IsUnityNull()) return;
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            rotY += mouseX * sensitivity;
            rotX -= mouseY * sensitivity;
            rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);

            Quaternion localRotation = Quaternion.Euler(rotX, rotY, 0f);
            currentCamera.transform.localRotation = localRotation;
        }
        public override void FixedUpdateController(){}

        public void ChangeCamera()
        {
            if (currentCamera.IsUnityNull() || mainCamera.IsUnityNull() || topCamera.IsUnityNull()) return;
            bool isMain = currentCamera == mainCamera;
            currentCamera.enabled = false;
            currentCamera = isMain ? topCamera : mainCamera;
            currentCamera.enabled = true;
        }
    }
}