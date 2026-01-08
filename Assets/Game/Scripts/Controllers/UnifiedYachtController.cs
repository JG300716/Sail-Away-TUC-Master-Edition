using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.Splines;
using Game.Scripts.Interface;

namespace Game.Scripts.Controllers
{
    public enum SelectedSail
    {
        Grot,
        Fok
    }
    
    public class UnifiedYachtController : ControllerInterface
    {
        [Header("References")]
        [SerializeField] private YachtState yachtState;
        [SerializeField] private Rigidbody yachtRigidbody;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private GameObject steeringWheelObject;

        [Header("Steering - PHYSICS BASED")]
        [SerializeField] private float rudderTorque = 5f;
        [SerializeField] private float rudderStep = 30f;
        [SerializeField] private float rudderMin = -30f;
        [SerializeField] private float rudderMax = 30f;
        [SerializeField] private float minSpeedForSteering = 0.5f;
        private float rudderAngle = 0f;

        [Header("Sail Control")]
        [SerializeField] private float sailStep = 30f; // stopni/sekundę
        private SelectedSail selectedSail = SelectedSail.Grot;
        
        // Zapisane stany żagli (żeby nie resetowały)
        private float grotLastAngle = 0f;
        private float fokLastSplinePosition = 0.5f;
        
        [Header("Unified Sails")]
        [SerializeField] private GameObject grotClothObject;
        [SerializeField] private GameObject fokClothObject;
        [SerializeField] private UnifiedSail grotSail; // Grot na joicie
        [SerializeField] private UnifiedSail fokSail;  // Fok na spline

        private UnifiedWindManager Wind => UnifiedWindManager.Instance;
        
        public override void Initialize()
        {
            // Pobierz Rigidbody
            if (yachtRigidbody == null)
                yachtRigidbody = GetComponent<Rigidbody>();
            
            // Auto-znajdź UnifiedSail jeśli nie przypisane
            if (grotSail == null && grotClothObject != null)
                grotSail = grotClothObject.GetComponentInChildren<UnifiedSail>();
            
            if (fokSail == null && fokClothObject != null)
                fokSail = fokClothObject.GetComponentInChildren<UnifiedSail>();
            
            // Inicjalizacja - ukryj żagle
            if (grotClothObject != null)
                grotClothObject.SetActive(false);
            
            if (fokClothObject != null)
                fokClothObject.SetActive(false);
        }

        public override void UpdateController()
        {
            HandleSteeringInput();
            HandleSailStateInput();
            HandleSailSelectionInput();
            HandleSailAngleInput(); // ZMIENIONE z HandleBoomAngleInput
            HandleCameraSwap();
            LeaveYacht();
        }

        public override void FixedUpdateController(){}

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
        
        void FixedUpdate()
        {
            ApplyPhysicsSteering();
        }
        
        void LeaveYacht()
        {
            if (yachtState.IsUnityNull()) return;
            if (!yachtState.isDriving) return;
            if (!Input.GetKeyDown(KeyCode.F)) return;
            GameManager.UnsteerYacht();
        }
        
        #region Camera
        
        void HandleCameraSwap()
        {
            if (!Input.GetKeyDown(KeyCode.Tab)) return;
            if (cameraController.IsUnityNull()) return;
            cameraController.ChangeCamera();
        }
        
        #endregion
        
        #region Steering AD - PHYSICS BASED
        
        private void HandleSteeringInput()
        {
            float rudderBefore = rudderAngle;
            if (Input.GetKey(KeyCode.A)) rudderAngle -= rudderStep * Time.deltaTime;
            else if (Input.GetKey(KeyCode.D)) rudderAngle += rudderStep * Time.deltaTime;
            
            rudderAngle = Mathf.Clamp(rudderAngle, rudderMin, rudderMax);
            steeringWheelObject?.transform.Rotate((rudderAngle - rudderBefore) * 10f, 0f, 0f);
        }

        private void ApplyPhysicsSteering()
        {
            if (yachtRigidbody.IsUnityNull()) return;
    
            Vector3 velocity = yachtRigidbody.linearVelocity;
            Vector3 velocityXZ = new Vector3(velocity.x, 0, velocity.z);
            float forwardSpeed = velocityXZ.magnitude;

            float speedFactor = Mathf.Max(forwardSpeed / 3f, 0.5f);
            float torqueMagnitude = rudderAngle * rudderTorque * speedFactor; 
    
            Vector3 torque = Vector3.up * torqueMagnitude;
            
            yachtRigidbody.AddTorque(torque, ForceMode.Force);
    
            if (!yachtState.IsUnityNull())
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
                        
                        if (!grotClothObject.IsUnityNull())
                            grotClothObject.SetActive(true);
                        
                        // Przywróć zapisany stan
                        if (grotSail != null && grotSail.CanControl())
                        {
                            grotSail.SetSailAngle(grotLastAngle);
                        }
                        break;
                    
                    case YachtSailState.Grot_Only:
                        yachtState.SailState = YachtSailState.Grot_and_Fok;
                        
                        if (!fokClothObject.IsUnityNull())
                            fokClothObject.SetActive(true);
                        
                        // Przywróć zapisany stan
                        if (fokSail != null && fokSail.CanControl())
                        {
                            fokSail.SetSplinePosition(fokLastSplinePosition);
                        }
                        break;
                }
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                switch (yachtState.SailState)
                {
                    case YachtSailState.Grot_Only:
                        // Zapisz stan przed wyłączeniem
                        if (grotSail != null && grotSail.CanControl())
                        {
                            grotLastAngle = grotSail.GetCurrentAngle();
                        }
                        
                        yachtState.SailState = YachtSailState.No_Sail;
                        
                        if (!grotClothObject.IsUnityNull())
                            grotClothObject.SetActive(false);
                        break;
                    
                    case YachtSailState.Grot_and_Fok:
                        // Zapisz stan przed wyłączeniem
                        if (fokSail != null && fokSail.CanControl())
                        {
                            fokLastSplinePosition = fokSail.GetSplinePosition();
                        }
                        
                        yachtState.SailState = YachtSailState.Grot_Only;
                        
                        if (!fokClothObject.IsUnityNull())
                            fokClothObject.SetActive(false);
                        break;
                }
            }
        }
        
        #endregion

        #region Sail Selection 1,2
        
        private void HandleSailSelectionInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                selectedSail = SelectedSail.Grot;
                //ObjectHighlightManager.HighlightObject(grotClothObject);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                selectedSail = SelectedSail.Fok;
                //ObjectHighlightManager.HighlightObject(fokClothObject);
            }
        }
        
        #endregion

        #region Sail Angle Q,E - ZMIENIONE
        
        private void HandleSailAngleInput()
        {
            bool canAdjust = false;
            UnifiedSail targetSail = null;

            switch (selectedSail)
            {
                case SelectedSail.Grot:
                    canAdjust = yachtState.SailState == YachtSailState.Grot_Only ||
                                yachtState.SailState == YachtSailState.Grot_and_Fok;
                    targetSail = grotSail;
                    
                    if (!canAdjust || targetSail.IsUnityNull() || !targetSail.CanControl()) 
                        break;
                    
                    // Sterowanie grotem
                    if (Input.GetKey(KeyCode.Q))
                    {
                        targetSail.RotateSail(-sailStep * Time.deltaTime);
                    }
                    if (Input.GetKey(KeyCode.E))
                    {
                        targetSail.RotateSail(sailStep * Time.deltaTime);
                    }
                    
                    // Zapisz aktualny stan
                    grotLastAngle = targetSail.GetCurrentAngle();
                    break;
                    
                case SelectedSail.Fok:
                    canAdjust = yachtState.SailState == YachtSailState.Fok_Only ||
                                yachtState.SailState == YachtSailState.Grot_and_Fok;
                    targetSail = fokSail;
                    
                    if (!canAdjust || targetSail.IsUnityNull() || !targetSail.CanControl()) 
                        break;
                    
                    // Fok używa spline movement
                    if (Input.GetKey(KeyCode.Q))
                    {
                        targetSail.MoveAlongSpline(sailStep * 0.001f * Time.deltaTime);
                    }
                    if (Input.GetKey(KeyCode.E))
                    {
                        targetSail.MoveAlongSpline(-sailStep * 0.001f * Time.deltaTime);
                    }
                    
                    // Zapisz aktualny stan
                    fokLastSplinePosition = targetSail.GetSplinePosition();
                    break;
            }
        }
        
        #endregion
    }
}