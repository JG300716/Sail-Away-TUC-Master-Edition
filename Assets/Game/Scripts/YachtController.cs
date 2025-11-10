using Unity.VisualScripting;
using UnityEngine;

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
        [SerializeField] private YachtPhysics yachtPhysics;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera secondCamera;
        
        [Header("Steering")]
        [SerializeField] private float rudderStep = 2f;
        [SerializeField] private float rudderMin = -30f;
        [SerializeField] private float rudderMax = 30f;
        private float rudderAngle = 0f;

        [Header("Sail Control")]
        [SerializeField] private float sailStep = 30f; // Stopień zmiany kąta żagla [°/s]
        private SelectedSail selectedSail = SelectedSail.Grot;
        
        [Header("Cloth Sails")]
        [SerializeField] private GameObject grotClothObject;
        [SerializeField] private GameObject fokClothObject;
        [SerializeField] private SailClothPhysics grotClothPhysics;
        [SerializeField] private SailClothPhysics fokClothPhysics;
        
        [Header("Boom Control")]
        [SerializeField] private ConfigurableJoint grotBoomJoint; // Joint bomu grota
        [SerializeField] private ConfigurableJoint fokBoomJoint;   // Joint bomu foka (opcjonalnie)
        [SerializeField] private float boomMinAngle = -90f; // Min kąt bomu
        [SerializeField] private float boomMaxAngle = 90f;  // Max kąt bomu
        
        // Aktualne pozycje boomów
        private float currentGrotBoomAngle = 0f;
        private float currentFokBoomAngle = 0f;
        
        private WindManager Wind => WindManager.Instance;
        
        void Start()
        {
            // Inicjalizacja - ukryj żagle i wyłącz fizykę
            if (grotClothObject != null)
                grotClothObject.SetActive(false);
            
            if (fokClothObject != null)
                fokClothObject.SetActive(false);
            
            if (grotClothPhysics != null)
                grotClothPhysics.enabled = false;
            
            if (fokClothPhysics != null)
                fokClothPhysics.enabled = false;
            
            // Inicjalizuj limity jointów
            InitializeBoomJoints();
        }
        
        void Update()
        {
            HandleSteeringInput();
            HandleSailStateInput();
            HandleSailSelectionInput();
            HandleBoomAngleInput(); // ZMIENIONE: Sterowanie boomem zamiast sheet angle
            HandleCameraSwap();
            ApplySteering();
            
            // Debug: Wiatr
            if (Wind != null)
            {
                var windDir = Quaternion.Euler(0, (float)Wind.WindDegree, 0) * Vector3.forward;
                Debug.DrawLine(transform.position, transform.position + windDir * 5f, Color.cyan);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3);
        }
        
        #region Boom Joint Setup
        
        /// <summary>
        /// Inicjalizuje limity jointów boomów
        /// </summary>
        private void InitializeBoomJoints()
        {
            if (grotBoomJoint != null)
            {
                SetBoomJointLimits(grotBoomJoint, boomMinAngle, boomMaxAngle);
                currentGrotBoomAngle = 0f;
            }
            
            if (fokBoomJoint != null)
            {
                SetBoomJointLimits(fokBoomJoint, boomMinAngle, boomMaxAngle);
                currentFokBoomAngle = 0f;
            }
        }
        
        /// <summary>
        /// Ustawia limity kątowe dla ConfigurableJoint bomu
        /// </summary>
        private void SetBoomJointLimits(ConfigurableJoint joint, float minAngle, float maxAngle)
        {
            // Dla ConfigurableJoint limity są w Angular X/Y/Z Limit
            // Zakładamy że boom obraca się wokół Y (Angular Y Motion = Limited)
            
            SoftJointLimit lowLimit = joint.lowAngularXLimit;
            lowLimit.limit = minAngle;
            joint.lowAngularXLimit = lowLimit;
            
            SoftJointLimit highLimit = joint.highAngularXLimit;
            highLimit.limit = maxAngle;
            joint.highAngularXLimit = highLimit;
            
            // Alternatywnie dla Angular Y:
            SoftJointLimit yLimit = joint.angularYLimit;
            yLimit.limit = maxAngle; // Angular Y używa tylko jednej wartości
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

        #region Sail State WS
        
        private void HandleSailStateInput()
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                switch (yachtState.SailState)
                {
                    case YachtSailState.No_Sail:
                        yachtState.SailState = YachtSailState.Grot_Only;
                        
                        // Włącz Cloth grot
                        if (grotClothObject != null)
                            grotClothObject.SetActive(true);
                        
                        if (grotClothPhysics != null)
                            grotClothPhysics.enabled = true;
                        
                        // Reset kąta bomu
                        currentGrotBoomAngle = 0f;
                        SetBoomTargetAngle(grotBoomJoint, currentGrotBoomAngle);
                        break;
                    
                    case YachtSailState.Grot_Only:
                        yachtState.SailState = YachtSailState.Grot_and_Fok;
                        
                        // Włącz Cloth fok
                        if (fokClothObject != null)
                            fokClothObject.SetActive(true);
                        
                        if (fokClothPhysics != null)
                            fokClothPhysics.enabled = true;
                        
                        // Reset kąta bomu
                        currentFokBoomAngle = 0f;
                        SetBoomTargetAngle(fokBoomJoint, currentFokBoomAngle);
                        break;
                }
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                switch (yachtState.SailState)
                {
                    case YachtSailState.Grot_Only:
                        yachtState.SailState = YachtSailState.No_Sail;
                        
                        // Wyłącz Cloth grot
                        if (grotClothObject != null)
                            grotClothObject.SetActive(false);
                        
                        if (grotClothPhysics != null)
                            grotClothPhysics.enabled = false;
                        break;
                    
                    case YachtSailState.Grot_and_Fok:
                        yachtState.SailState = YachtSailState.Grot_Only;
                        
                        // Wyłącz Cloth fok
                        if (fokClothObject != null)
                            fokClothObject.SetActive(false);
                        
                        if (fokClothPhysics != null)
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
        
        /// <summary>
        /// Obsługuje wejście Q/E do sterowania kątem bomu
        /// </summary>
        private void HandleBoomAngleInput()
        {
            // Sprawdź czy wybrany żagiel jest postawiony
            bool canAdjust = false;
            ConfigurableJoint targetJoint = null;
            
            switch (selectedSail)
            {
                case SelectedSail.Grot:
                    canAdjust = yachtState.SailState == YachtSailState.Grot_Only ||
                                yachtState.SailState == YachtSailState.Grot_and_Fok;
                    targetJoint = grotBoomJoint;
                    break;
                    
                case SelectedSail.Fok:
                    canAdjust = yachtState.SailState == YachtSailState.Fok_Only ||
                                yachtState.SailState == YachtSailState.Grot_and_Fok;
                    targetJoint = fokBoomJoint;
                    break;
            }

            if (!canAdjust || targetJoint == null)
                return;

            // Pobierz aktualny kąt
            float currentAngle = selectedSail == SelectedSail.Grot ? currentGrotBoomAngle : currentFokBoomAngle;

            // Regulacja kąta bomu
            bool angleChanged = false;
            
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

            // Ogranicz do limitów jointa
            currentAngle = Mathf.Clamp(currentAngle, boomMinAngle, boomMaxAngle);

            // Zapisz nowy kąt
            if (selectedSail == SelectedSail.Grot)
                currentGrotBoomAngle = currentAngle;
            else
                currentFokBoomAngle = currentAngle;

            // Aplikuj do jointa
            if (angleChanged)
            {
                SetBoomTargetAngle(targetJoint, currentAngle);
            }
        }
        
        /// <summary>
        /// Ustawia docelowy kąt bomu przez Target Rotation jointa
        /// </summary>
        private void SetBoomTargetAngle(ConfigurableJoint joint, float angle)
        {
            if (joint == null)
                return;
            
            // OPCJA 1: Użyj Target Rotation (wymaga XDrive/YZDrive)
            // Quaternion targetRotation = Quaternion.Euler(0, angle, 0);
            // joint.targetRotation = targetRotation;
            
            // OPCJA 2: Bezpośrednia rotacja (jeśli używasz FixedUpdate)
            // Transform boomTransform = joint.transform;
            // boomTransform.localRotation = Quaternion.Euler(0, angle, 0);
            
            // OPCJA 3: Dynamiczne limity (najprostsze dla ConfigurableJoint)
            // Ograniczamy joint do konkretnego kąta przez zawężenie limitów
            float tolerance = 1f; // Małe okno tolerancji
            
            SoftJointLimit lowLimit = joint.lowAngularXLimit;
            lowLimit.limit = angle - tolerance;
            joint.lowAngularXLimit = lowLimit;
            
            SoftJointLimit highLimit = joint.highAngularXLimit;
            highLimit.limit = angle + tolerance;
            joint.highAngularXLimit = highLimit;
            
            // Debug
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log($"[YachtController] Boom {selectedSail} angle set to: {angle:F1}°");
            }
        }
        
        /// <summary>
        /// Alternatywna metoda: Bezpośrednia aplikacja siły obrotowej na boom
        /// Użyj jeśli powyższa metoda nie działa dobrze
        /// </summary>
        private void ApplyBoomTorque(ConfigurableJoint joint, float targetAngle)
        {
            if (joint == null)
                return;
            
            Rigidbody boomRb = joint.GetComponent<Rigidbody>();
            if (boomRb == null)
                return;
            
            // Oblicz różnicę między aktualnym a docelowym kątem
            float currentAngle = joint.transform.localEulerAngles.y;
            if (currentAngle > 180f) currentAngle -= 360f; // Normalizuj do -180..180
            
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            
            // Aplikuj moment obrotowy proporcjonalny do różnicy
            float torque = angleDifference * 10f; // Dostosuj współczynnik
            boomRb.AddRelativeTorque(0, torque, 0, ForceMode.Force);
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Log Boom State")]
        private void LogBoomState()
        {
            Debug.Log("=== BOOM STATE ===");
            Debug.Log($"Selected Sail: {selectedSail}");
            Debug.Log($"Grot Boom Angle: {currentGrotBoomAngle:F1}°");
            Debug.Log($"Fok Boom Angle: {currentFokBoomAngle:F1}°");
            Debug.Log($"Boom Limits: [{boomMinAngle:F0}°, {boomMaxAngle:F0}°]");
            
            if (grotBoomJoint != null)
            {
                Debug.Log($"Grot Joint - Low: {grotBoomJoint.lowAngularXLimit.limit:F1}°, High: {grotBoomJoint.highAngularXLimit.limit:F1}°");
            }
        }
        
        #endregion
    }
}