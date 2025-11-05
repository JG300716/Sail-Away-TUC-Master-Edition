using Unity.VisualScripting;
using UnityEngine;

namespace Game.Assets
{
    public enum SelectedSail
    {
        Grot,
        Fok
    }
    
    public class YachtController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private YachtState yachtState;       // Obiekt stanu jachtu
        [SerializeField] private YachtPhysics yachtPhysics;   // Obiekt fizyki jachtu
        [SerializeField] private CameraController cameraController; // Kontroler kamery
        [SerializeField] private Camera mainCamera;            // Główna kamera
        [SerializeField] private Camera secondCamera;
        
        [Header("Steering")]
        [SerializeField] private float rudderStep = 2f;
        [SerializeField] private float rudderMin = -30f;
        [SerializeField] private float rudderMax = 30f;
        private float rudderAngle = 0f;

        [Header("Sail Control")]
        [SerializeField] private float sailStep = 2f; // stopień zmiany kąta żagla na tick
        private SelectedSail selectedSail = SelectedSail.Grot;
        private WindManager Wind => WindManager.Instance;
        
        
        void Update()
        {
            HandleSteeringInput();
            HandleSailStateInput();
            HandleSailSelectionInput();
            HandleSailAngleInput();
            HandleCameraSwap();
            ApplySteering();
            var windDir = Quaternion.Euler(0, (float)Wind.WindDegree, 0) * Vector3.forward;
            Debug.DrawLine(transform.position, transform.position + windDir * 5f, Color.cyan);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3);
        }
        
        void HandleCameraSwap()
        {
            if (!Input.GetKeyDown(KeyCode.Tab)) return;
            if (mainCamera.IsUnityNull() || secondCamera.IsUnityNull()) return;
            var isMainCameraActive = mainCamera.enabled;

            mainCamera.enabled = !isMainCameraActive;
            secondCamera.enabled = isMainCameraActive;
            cameraController.boatCamera = isMainCameraActive ? secondCamera : mainCamera;
        }
        
        #region Steering AD
        private void HandleSteeringInput()
        {
            if (Input.GetKey(KeyCode.A)) rudderAngle -= rudderStep * Time.deltaTime * 60f;
            if (Input.GetKey(KeyCode.D)) rudderAngle += rudderStep * Time.deltaTime * 60f;
            if (Input.GetKey(KeyCode.Space)) rudderAngle = 0f;
            rudderAngle = Mathf.Clamp(rudderAngle, rudderMin, rudderMax);
        }

        private void ApplySteering()
        {
            yachtState.ApplyRotation(rudderAngle * Time.deltaTime);
            transform.Rotate(0f, rudderAngle * Time.deltaTime, 0f);
        }
        #endregion

        #region Sail WS
        private void HandleSailStateInput()
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                // Stawiamy żagle
                switch (yachtState.SailState)
                {
                    case YachtSailState.No_Sail:
                        yachtState.SailState = YachtSailState.Grot_Only;
                        yachtPhysics.SheetAngleGrot = 0f;
                        break;
                    case YachtSailState.Grot_Only:
                        yachtState.SailState = YachtSailState.Grot_and_Fok;
                        yachtPhysics.SheetAngleFok = 0f;
                        break;
                }
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                // Zwijamy żagle
                switch (yachtState.SailState)
                {
                    case YachtSailState.Grot_Only:
                        yachtState.SailState = YachtSailState.No_Sail;
                        break;
                    case YachtSailState.Grot_and_Fok:
                        yachtState.SailState = YachtSailState.Grot_Only;
                        break;
                }
            }
        }
        #endregion

        #region Sail selection 1,2
        private void HandleSailSelectionInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) selectedSail = SelectedSail.Grot;
            if (Input.GetKeyDown(KeyCode.Alpha2)) selectedSail = SelectedSail.Fok;
        }
        #endregion

        #region Sail angle Q,E
        private void HandleSailAngleInput()
        {
            // Sprawdzenie czy wybrany żagiel jest postawiony
            bool canAdjust = false;
            switch (selectedSail)
            {
                case SelectedSail.Grot:
                    canAdjust = yachtState.SailState == YachtSailState.Grot_Only
                                || yachtState.SailState == YachtSailState.Grot_and_Fok;
                    break;
                case SelectedSail.Fok:
                    canAdjust = yachtState.SailState == YachtSailState.Fok_Only
                                || yachtState.SailState == YachtSailState.Grot_and_Fok;
                    break;
            }

            if (!canAdjust) return;

            // Regulacja kąta żagla
            if (Input.GetKey(KeyCode.Q))
            {
                if (selectedSail == SelectedSail.Grot)
                    yachtPhysics.SheetAngleGrot -= sailStep * Time.deltaTime * 60f;
                else
                    yachtPhysics.SheetAngleFok -= sailStep * Time.deltaTime * 60f;
            }

            if (Input.GetKey(KeyCode.E))
            {
                if (selectedSail == SelectedSail.Grot)
                    yachtPhysics.SheetAngleGrot += sailStep * Time.deltaTime * 60f;
                else
                    yachtPhysics.SheetAngleFok += sailStep * Time.deltaTime * 60f;
            }

            // Ograniczenie kątów żagli [-90°, 90°]
            yachtPhysics.SheetAngleGrot = Mathf.Clamp(yachtPhysics.SheetAngleGrot, -90f, 90f);
            yachtPhysics.SheetAngleFok = Mathf.Clamp(yachtPhysics.SheetAngleFok, -90f, 90f);
        }
        #endregion
    }

}