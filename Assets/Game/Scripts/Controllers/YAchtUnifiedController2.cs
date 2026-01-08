using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.Splines;
using Game.Scripts.Interface;

namespace Game.Scripts.Controllers
{
    public class YAchtUnifiedController2 : ControllerInterface
    {
        [Header("References")]
        [SerializeField] private YachtState yachtState;
        [SerializeField] private Rigidbody yachtRigidbody;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private GameObject steeringWheelObject;

        [Header("Steering")]
        [SerializeField] private float rudderTorque = 5f;
        [SerializeField] private float rudderStep = 30f;
        [SerializeField] private float rudderMin = -30f;
        [SerializeField] private float rudderMax = 30f;
        [SerializeField] private float minSpeedForSteering = 0.5f; // Minimalna prędkość do skręcania
        private float rudderAngle;

        [Header("Sail Control")]
        [SerializeField] private float sailStep = 30f;
        private SelectedSail selectedSail = SelectedSail.Grot;

        [Header("Grot (UnifiedSail)")]
        [SerializeField] private GameObject grotClothObject;
        [SerializeField] private UnifiedSail grotSail;
        private float grotLastAngle;

        [Header("Fok (Spline)")]
        [SerializeField] private GameObject fokClothObject;
        [SerializeField] private SplineContainer splineComponent;
        [SerializeField] private float fokSpeed = 0.02f;
        [SerializeField] private Transform fokBone;
        [SerializeField] private Transform fokShot;
        private float fokT = 0.5f;
        private Transform splineTransform;
        
        [Header("Stability (Anti-Capsizing)")]
        [SerializeField] private bool enableStabilization = true;
        [SerializeField] private float stabilizationTorque = 50f;
        [SerializeField] private float maxRollAngle = 25f; // Maksymalny przechył przed korekcją

        public override void Initialize()
        {
            if (yachtRigidbody == null)
                yachtRigidbody = GetComponent<Rigidbody>();

            if (grotSail == null && grotClothObject != null)
                grotSail = grotClothObject.GetComponentInChildren<UnifiedSail>();

            if (splineComponent != null)
                splineTransform = splineComponent.transform;

            grotClothObject?.SetActive(false);
            fokClothObject?.SetActive(false);
        }

        public override void UpdateController()
        {
            HandleSteeringInput();
            HandleSailStateInput();
            HandleSailSelectionInput();
            HandleSailControl();
            HandleCameraSwap();
            LeaveYacht();
        }

        public override void FixedUpdateController()
        {
            ApplyPhysicsSteering();
            
            if (enableStabilization)
                ApplyStabilization();
        }

        public override void EnableController()
        {
            if (yachtState.IsUnityNull()) return;
            yachtState.isDriving = true;
        }

        public override void DisableController()
        {
            if (yachtState.IsUnityNull()) return;
            yachtState.isDriving = false;
        }
        
        #region Steering

        private void HandleSteeringInput()
        {
            float before = rudderAngle;

            if (Input.GetKey(KeyCode.A)) rudderAngle -= rudderStep * Time.deltaTime;
            if (Input.GetKey(KeyCode.D)) rudderAngle += rudderStep * Time.deltaTime;

            rudderAngle = Mathf.Clamp(rudderAngle, rudderMin, rudderMax);
            steeringWheelObject?.transform.Rotate((rudderAngle - before) * 10f, 0f, 0f);
        }

        private void ApplyPhysicsSteering()
        {
            if (yachtRigidbody.IsUnityNull()) return;

            Vector3 v = yachtRigidbody.linearVelocity;
            Vector3 vXZ = new Vector3(v.x, 0, v.z);
            float speed = vXZ.magnitude;
            
            // Ster działa tylko przy ruchu
            if (speed < minSpeedForSteering)
            {
                // Opcjonalnie: resetuj rudderAngle jeśli nie ma prędkości
                // rudderAngle = Mathf.Lerp(rudderAngle, 0f, Time.fixedDeltaTime * 2f);
                return;
            }

            // Torque skalowany prędkością (szybciej = łatwiej skręcić)
            float speedFactor = Mathf.Min(speed / 5f, 1f); // Max przy 5 m/s
            float torqueMagnitude = rudderAngle * rudderTorque * speedFactor;
            
            yachtRigidbody.AddTorque(Vector3.up * torqueMagnitude, ForceMode.Force);

            if (!yachtState.IsUnityNull())
                yachtState.Deg_from_north = transform.eulerAngles.y;
        }
        
        private void ApplyStabilization()
        {
            // Pobierz obecną rotację
            Vector3 eulerAngles = transform.eulerAngles;
            
            // Normalizuj do -180..180
            float rollAngle = eulerAngles.z;
            if (rollAngle > 180f) rollAngle -= 360f;
            
            float pitchAngle = eulerAngles.x;
            if (pitchAngle > 180f) pitchAngle -= 360f;
            
            // Siła stabilizacyjna dla przechyłu (roll) - oś Z
            if (Mathf.Abs(rollAngle) > 1f)
            {
                float rollTorque = -rollAngle * stabilizationTorque * Time.fixedDeltaTime;
                
                // Jeśli przekroczono maxRollAngle, zwiększ siłę korekcyjną
                if (Mathf.Abs(rollAngle) > maxRollAngle)
                {
                    float excessRoll = Mathf.Abs(rollAngle) - maxRollAngle;
                    rollTorque *= (1f + excessRoll / 10f); // Zwiększ siłę proporcjonalnie
                }
                
                yachtRigidbody.AddTorque(Vector3.forward * rollTorque, ForceMode.Force);
            }
            
            // Siła stabilizacyjna dla kołysania (pitch) - oś X
            if (Mathf.Abs(pitchAngle) > 1f)
            {
                float pitchTorque = -pitchAngle * stabilizationTorque * 0.5f * Time.fixedDeltaTime;
                yachtRigidbody.AddTorque(Vector3.right * pitchTorque, ForceMode.Force);
            }
            
            // Dodatkowo: tłumienie prędkości kątowej dla stabilności
            Vector3 angularVel = yachtRigidbody.angularVelocity;
            
            // Tłum przechył (Z) i kołysanie (X), ale NIE obrót (Y)
            float dampingFactor = 0.95f; // 5% tłumienia na klatkę
            yachtRigidbody.angularVelocity = new Vector3(
                angularVel.x * dampingFactor,
                angularVel.y, // Nie tłum obrotu wokół Y (yaw)
                angularVel.z * dampingFactor
            );
        }

        #endregion

        #region Sail State W / S

        private void HandleSailStateInput()
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                if (yachtState.SailState == YachtSailState.No_Sail)
                {
                    yachtState.SailState = YachtSailState.Grot_Only;
                    grotClothObject?.SetActive(true);
                    grotSail?.SetSailAngle(grotLastAngle);
                }
                else if (yachtState.SailState == YachtSailState.Grot_Only)
                {
                    yachtState.SailState = YachtSailState.Grot_and_Fok;
                    fokClothObject?.SetActive(true);
                }
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                if (yachtState.SailState == YachtSailState.Grot_and_Fok)
                {
                    yachtState.SailState = YachtSailState.Grot_Only;
                    fokClothObject?.SetActive(false);
                }
                else if (yachtState.SailState == YachtSailState.Grot_Only)
                {
                    grotLastAngle = grotSail?.GetCurrentAngle() ?? 0f;
                    yachtState.SailState = YachtSailState.No_Sail;
                    grotClothObject?.SetActive(false);
                }
            }
        }

        #endregion

        #region Sail Selection 1 / 2

        private void HandleSailSelectionInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                selectedSail = SelectedSail.Grot;

            if (Input.GetKeyDown(KeyCode.Alpha2))
                selectedSail = SelectedSail.Fok;
        }

        #endregion

        #region Sail Control Q / E

        private void HandleSailControl()
        {
            switch (selectedSail)
            {
                case SelectedSail.Grot:
                    if (grotSail == null || !grotSail.CanControl()) return;

                    if (Input.GetKey(KeyCode.Q))
                        grotSail.RotateSail(sailStep * Time.deltaTime);

                    if (Input.GetKey(KeyCode.E))
                        grotSail.RotateSail(-sailStep * Time.deltaTime);

                    grotLastAngle = grotSail.GetCurrentAngle();
                    break;

                case SelectedSail.Fok:
                    if (splineComponent == null || fokShot == null || fokBone == null) return;

                    if (Input.GetKey(KeyCode.Q)) fokT += fokSpeed * Time.deltaTime;
                    if (Input.GetKey(KeyCode.E)) fokT -= fokSpeed * Time.deltaTime;

                    fokT = Mathf.Clamp01(fokT);

                    var spline = splineComponent.Spline;
                    fokShot.position = splineTransform.TransformPoint(spline.EvaluatePosition(fokT));
                    fokShot.rotation = Quaternion.LookRotation(
                        splineTransform.TransformDirection(spline.EvaluateTangent(fokT)),
                        Vector3.up);

                    fokBone.SetPositionAndRotation(fokShot.position, fokShot.rotation);
                    break;
            }
        }

        #endregion

        #region Misc

        private void HandleCameraSwap()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                cameraController?.ChangeCamera();
        }

        private void LeaveYacht()
        {
            if (Input.GetKeyDown(KeyCode.F))
                GameManager.UnsteerYacht();
        }

        #endregion
        
        #region Physics API
        
        public void ApplyForce(Vector3 force)
        {
            force.y = 0; // Tylko XZ
            yachtRigidbody.AddForce(force, ForceMode.Force);
        }
    
        public void ApplyForce(Vector2 force)
        {
            yachtRigidbody.AddForce(new Vector3(force.x, 0, force.y), ForceMode.Force);
        }
    
        public void ApplyTorque(float torque)
        {
            yachtRigidbody.AddTorque(Vector3.up * torque, ForceMode.Force);
        }
    
        public Vector3 GetVelocity()
        {
            return yachtRigidbody.linearVelocity;
        }
    
        public Vector2 GetVelocity2D()
        {
            return new Vector2(yachtRigidbody.linearVelocity.x, yachtRigidbody.linearVelocity.z);
        }
    
        public float GetAngularVelocity()
        {
            return yachtRigidbody.angularVelocity.y;
        }
    
        public Vector3 GetAngularVelocityVector()
        {
            return yachtRigidbody.angularVelocity;
        }

        public void ApplyTorque(Vector3 torqueVector)
        {
            yachtRigidbody.AddTorque(torqueVector, ForceMode.Force);
        }

        public void ApplyTorquePitch(float torque)
        {
            yachtRigidbody.AddTorque(Vector3.right * torque, ForceMode.Force);
        }

        public void ApplyTorqueRoll(float torque)
        {
            yachtRigidbody.AddTorque(Vector3.forward * torque, ForceMode.Force);
        }
    
        public Vector3 GetForwardDirection()
        {
            return transform.forward;
        }
    
        public Vector2 GetForwardDirection2D()
        {
            Vector3 forward = transform.forward;
            return new Vector2(forward.x, forward.z);
        }
        
        #endregion
    }
}