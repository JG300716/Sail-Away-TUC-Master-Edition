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
            float speed = new Vector3(v.x, 0, v.z).magnitude;
            float factor = Mathf.Max(speed / 3f, 0.5f);

            yachtRigidbody.AddTorque(Vector3.up * rudderAngle * rudderTorque * factor);
            yachtState.Deg_from_north = transform.eulerAngles.y;
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
        
        public void ApplyForce(Vector3 force)
        {
            force.y = 0; // Tylko XZ
            yachtRigidbody.AddForce(force);
        }
    
        public void ApplyForce(Vector2 force)
        {
            // Vector2 (x,y) -> Vector3 (x,0,z) - mapowanie 2D na XZ
            yachtRigidbody.AddForce(new Vector3(force.x, 0, force.y));
        }
    
        public void ApplyTorque(float torque)
        {
            // Moment obrotowy wokół osi Y (vertical)
            yachtRigidbody.AddTorque(Vector3.up * torque);
        }
    
        public Vector3 GetVelocity()
        {
            return yachtRigidbody.linearVelocity;
        }
    
        public Vector2 GetVelocity2D()
        {
            // Vector3 (x,y,z) -> Vector2 (x,z) - mapowanie XZ na 2D
            return new Vector2(yachtRigidbody.linearVelocity.x, yachtRigidbody.linearVelocity.z);
        }
    
        public float GetAngularVelocity()
        {
            // Tylko komponent Y (obrót wokół vertical)
            return yachtRigidbody.angularVelocity.y;
        }
    
        public Vector3 GetAngularVelocityVector()
        {
            return yachtRigidbody.angularVelocity;
        }

        public void ApplyTorque(Vector3 torqueVector)
        {
            yachtRigidbody.AddTorque(torqueVector);
        }

        public void ApplyTorquePitch(float torque)
        {
            yachtRigidbody.AddTorque(Vector3.right * torque);
        }

        public void ApplyTorqueRoll(float torque)
        {
            yachtRigidbody.AddTorque(Vector3.forward * torque);
        }
    
        public Vector3 GetForwardDirection()
        {
            // Forward w 3D to oś Z
            return transform.forward;
        }
    
        public Vector2 GetForwardDirection2D()
        {
            // Vector3 (x,y,z) -> Vector2 (x,z) - mapowanie XZ na 2D
            Vector3 forward = transform.forward;
            return new Vector2(forward.x, forward.z);
        }
    }
}
