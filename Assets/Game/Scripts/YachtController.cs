using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

namespace Game.Scripts
{
    public enum SelectedSail
    {
        Grot,
        Fok
    }
    
    public class YachtController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private YachtState yachtState;
        [SerializeField] private Rigidbody yachtRigidbody; // DODANE
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera secondCamera;
        
        [Header("Steering - PHYSICS BASED")]
        [SerializeField] private float rudderTorque = 5f; // Moment obrotowy steru
        [SerializeField] private float rudderStep = 30f; // Kąt steru [°/s]
        [SerializeField] private float rudderMin = -30f;
        [SerializeField] private float rudderMax = 30f;
        [SerializeField] private float minSpeedForSteering = 0.5f; // Minimalna prędkość do skręcania
        private float rudderAngle = 0f;

        [Header("Sail Control")]
        [SerializeField] private float sailStep = 30f;
        private SelectedSail selectedSail = SelectedSail.Grot;
        
        [Header("Cloth Sails")]
        [SerializeField] private GameObject grotClothObject;
        [SerializeField] private GameObject fokClothObject;
        [SerializeField] private SailClothPhysics grotClothPhysics;
        [SerializeField] private SailClothPhysics fokClothPhysics;
        
        [Header("Boom Control")]
        [SerializeField] private ConfigurableJoint grotBoomJoint;
        [SerializeField] private float boomMinAngle = -90f;
        [SerializeField] private float boomMaxAngle = 90f;
        
        [Header("Fok Shot Path")]
        [SerializeField] private SplineContainer splineComponent;
        [SerializeField] private float speed = 0.02f;
        [SerializeField] private float t;
        [SerializeField] private Transform FokBone;
        [SerializeField] private Transform FokShot;
        private Transform splineTransform;

        private float currentGrotBoomAngle = 0f;
        private float currentFokBoomAngle = 0f;
        
        private WindManager Wind => WindManager.Instance;
        
        void Start()
        {
            // Pobierz Rigidbody
            if (yachtRigidbody == null)
                yachtRigidbody = GetComponent<Rigidbody>();
            
            // Inicjalizacja - ukryj żagle
            if (grotClothObject != null)
                grotClothObject.SetActive(false);
            
            if (fokClothObject != null)
                fokClothObject.SetActive(false);
            
            if (grotClothPhysics != null)
                grotClothPhysics.enabled = false;
            
            if (fokClothPhysics != null)
                fokClothPhysics.enabled = false;
                
            if (splineComponent != null)
                splineTransform = splineComponent.transform;
            
            InitializeBoomJoints();
        }
        
        void Update()
        {
            HandleSteeringInput();
            HandleSailStateInput();
            HandleSailSelectionInput();
            HandleBoomAngleInput();
            HandleCameraSwap();
            HandleChestOpening();
            // Debug: Wiatr
            if (!Wind .IsUnityNull())
            {
                var windDir = Quaternion.Euler(0, (float)Wind.WindDegree, 0) * Vector3.forward;
                Debug.DrawLine(transform.position, transform.position + windDir * 5f, Color.cyan);
            }
        }

        void HandleChestOpening()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Time.timeScale = 0f;
                //Physics.autoSimulation = false;

                                
                
                Time.timeScale = 1f;
                //Physics.autoSimulation = true;
            }
        }
        
        void FixedUpdate()
        {
            // STEROWANIE PRZEZ FIZYKĘ w FixedUpdate
            ApplyPhysicsSteering();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3);
        }
        
        #region Boom Joint Setup
        
        private void InitializeBoomJoints()
        {
            if (grotBoomJoint != null)
            {
                SetSailJointLimits(grotBoomJoint, boomMinAngle, boomMaxAngle);
                currentGrotBoomAngle = 0f;
            }
        }
        
        private void SetSailJointLimits(ConfigurableJoint joint, float minAngle, float maxAngle)
        {
            SoftJointLimit lowLimit = joint.lowAngularXLimit;
            lowLimit.limit = minAngle;
            joint.lowAngularXLimit = lowLimit;
            
            SoftJointLimit highLimit = joint.highAngularXLimit;
            highLimit.limit = maxAngle;
            joint.highAngularXLimit = highLimit;
            
            SoftJointLimit yLimit = joint.angularYLimit;
            yLimit.limit = maxAngle;
            joint.angularYLimit = yLimit;
        }
        
        #endregion
        
        #region Camera
        
        void HandleCameraSwap()
        {
            if (!Input.GetKeyDown(KeyCode.Tab)) return;
            if (mainCamera.IsUnityNull() || secondCamera.IsUnityNull()) return;
            
            var isMainCameraActive = mainCamera.enabled;
            mainCamera.enabled = !isMainCameraActive;
            secondCamera.enabled = isMainCameraActive;
            cameraController.boatCamera = isMainCameraActive ? secondCamera : mainCamera;
        }
        
        #endregion
        
        #region Steering AD - PHYSICS BASED
        
        private void HandleSteeringInput()
        {
            // Aktualizuj kąt steru (nie rotuj jeszcze jachtu!)
            if (Input.GetKey(KeyCode.A)) rudderAngle -= rudderStep * Time.deltaTime;
            if (Input.GetKey(KeyCode.D)) rudderAngle += rudderStep * Time.deltaTime;
            if (Input.GetKey(KeyCode.Space)) rudderAngle = 0f;
            
            rudderAngle = Mathf.Clamp(rudderAngle, rudderMin, rudderMax);
        }

        private void ApplyPhysicsSteering()
        {
            if (yachtRigidbody .IsUnityNull()) return;
    
            Vector3 velocity = yachtRigidbody.linearVelocity;
            Vector3 velocityXZ = new Vector3(velocity.x, 0, velocity.z);
            float forwardSpeed = velocityXZ.magnitude;

            float speedFactor = Mathf.Max(forwardSpeed / 3f, 0.5f); // Min 50% mocy
            float torqueMagnitude = rudderAngle * rudderTorque * speedFactor * 10f; // x10 multiplier!
    
            Vector3 torque = Vector3.up * torqueMagnitude; // USUŃ Mathf.Deg2Rad!
            
            yachtRigidbody.AddTorque(torque, ForceMode.Force);
    
            if (!yachtState .IsUnityNull())
            {
                yachtState.Deg_from_north = transform.eulerAngles.y;
            }
        }
        
        #endregion

        #region Sail State WS
        
        private void HandleSailStateInput()
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                switch (yachtState.SailState)
                {
                    case YachtSailState.No_Sail:
                        yachtState.SailState = YachtSailState.Grot_Only;
                        
                        if (!grotClothObject .IsUnityNull())
                            grotClothObject.SetActive(true);
                        
                        if (!grotClothPhysics .IsUnityNull())
                            grotClothPhysics.enabled = true;
                        
                        currentGrotBoomAngle = 0f;
                        SetBoomTargetAngle(grotBoomJoint, currentGrotBoomAngle);
                        break;
                    
                    case YachtSailState.Grot_Only:
                        yachtState.SailState = YachtSailState.Grot_and_Fok;
                        
                        if (!fokClothObject .IsUnityNull())
                            fokClothObject.SetActive(true);
                        
                        if (!fokClothPhysics .IsUnityNull())
                            fokClothPhysics.enabled = true;
                        break;
                }
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                switch (yachtState.SailState)
                {
                    case YachtSailState.Grot_Only:
                        yachtState.SailState = YachtSailState.No_Sail;
                        
                        if (!grotClothObject .IsUnityNull())
                            grotClothObject.SetActive(false);
                        
                        if (!grotClothPhysics .IsUnityNull())
                            grotClothPhysics.enabled = false;
                        break;
                    
                    case YachtSailState.Grot_and_Fok:
                        yachtState.SailState = YachtSailState.Grot_Only;
                        
                        if (!fokClothObject .IsUnityNull())
                            fokClothObject.SetActive(false);
                        
                        if (!fokClothPhysics .IsUnityNull())
                            fokClothPhysics.enabled = false;
                        break;
                }
            }
        }
        
        #endregion

        #region Sail Selection 1,2
        
        private void HandleSailSelectionInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) selectedSail = SelectedSail.Grot;
            if (Input.GetKeyDown(KeyCode.Alpha2)) selectedSail = SelectedSail.Fok;
        }
        
        #endregion

        #region Boom Angle Q,E
        
        private void HandleBoomAngleInput()
        {
            bool canAdjust = false;
            ConfigurableJoint targetJoint = null;
            bool angleChanged = false;

            switch (selectedSail)
            {
                case SelectedSail.Grot:
                    canAdjust = yachtState.SailState == YachtSailState.Grot_Only ||
                                yachtState.SailState == YachtSailState.Grot_and_Fok;
                    targetJoint = grotBoomJoint;
                    
                    if (!canAdjust || targetJoint .IsUnityNull()) break;
                    
                    float currentAngle = currentGrotBoomAngle;
                    
                    if (Input.GetKey(KeyCode.Q))
                    {
                        currentAngle -= sailStep * Time.deltaTime;
                        angleChanged = true;
                    }
                    if (Input.GetKey(KeyCode.E))
                    {
                        currentAngle += sailStep * Time.deltaTime;
                        angleChanged = true;
                    }
                    
                    currentAngle = Mathf.Clamp(currentAngle, boomMinAngle, boomMaxAngle);
                    
                    if (angleChanged)
                    {
                        SetBoomTargetAngle(targetJoint, currentAngle);
                        currentGrotBoomAngle = currentAngle;
                    }
                    break;
                    
                case SelectedSail.Fok:
                    canAdjust = yachtState.SailState == YachtSailState.Fok_Only ||
                                yachtState.SailState == YachtSailState.Grot_and_Fok;
                    
                    if (!canAdjust) break;
                    
                    if (Input.GetKey(KeyCode.Q))
                    {
                        t += Time.deltaTime * speed;
                        angleChanged = true;
                    }
                    if (Input.GetKey(KeyCode.E))
                    {
                        t -= Time.deltaTime * speed;
                        angleChanged = true;
                    }

                    if (!angleChanged) break;
                    
                    var points = splineComponent.Spline;
                    if (FokShot.IsUnityNull() || points.Count == 0 || FokBone.IsUnityNull()) break;
                    
                    t = Mathf.Clamp01(t);
                    
                    FokShot.position = splineTransform.TransformPoint(points.EvaluatePosition(t));
                    FokShot.rotation = Quaternion.LookRotation(splineTransform.TransformDirection(points.EvaluateTangent(t)), Vector3.up);
                    
                    FokBone.position = FokShot.position;
                    FokBone.rotation = FokShot.rotation;
                    break;
            }
        }
        
        private void SetBoomTargetAngle(ConfigurableJoint joint, float angle)
        {
            if (joint.IsUnityNull()) return;
            
            float tolerance = 3f;
            
            SoftJointLimit lowLimit = joint.lowAngularXLimit;
            lowLimit.limit = angle - tolerance;
            joint.lowAngularXLimit = lowLimit;
            
            SoftJointLimit highLimit = joint.highAngularXLimit;
            highLimit.limit = angle + tolerance;
            joint.highAngularXLimit = highLimit;
        }
        
        #endregion
    }
}