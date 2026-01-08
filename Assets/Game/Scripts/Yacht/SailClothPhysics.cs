using UnityEngine;

namespace Game.Scripts
{
    /// <summary>
    /// Oblicza siły aerodynamiczne na podstawie Cloth żagla
    /// Uproszczona wersja - traktuje żagiel jako pojedynczą powierzchnię
    /// </summary>
    public class SailClothPhysics : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Cloth sailCloth;

        [SerializeField] private Rigidbody yachtRigidbody;
        [SerializeField] private Transform sailAttachmentPoint;

        [Header("Sail Parameters")] [SerializeField]
        private float sailArea = 20f; // Powierzchnia żagla [m²]

        [Header("Physics Parameters")] [SerializeField]
        private float airDensity = 1.225f; // kg/m³

        [SerializeField] private float liftCoefficient = 0.5f; // Współczynnik siły nośnej
        [SerializeField] private float dragCoefficient = 0.15f; // Współczynnik oporu
        [SerializeField] private float forceMultiplier = 0.8f; // Globalne wzmocnienie siły
        [SerializeField] private float maxForce = 3000f; // Maksymalna siła [N]

        [Header("Wind Application")] [SerializeField]
        private bool applyWindToCloth = true;

        [SerializeField] private float windForceMultiplier = 1.0f;

        [Header("Sail Orientation")] [SerializeField]
        private Vector3 sailNormalDirection = Vector3.forward; // Kierunek normalnej żagla w local space

        [Header("Debug")] [SerializeField] private bool showForceVectors = true;
        [SerializeField] private float forceVectorScale = 0.1f;
        [SerializeField] private bool enableDebugLogs = false;

        private UnifiedWindManager Wind => UnifiedWindManager.Instance;
        private Vector3 currentDriveForce;
        private Vector3 currentLateralForce;
        private Vector3 totalForce;
        private bool isInitialized = false;

        void Start()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Auto-assign components
            if (sailCloth == null)
                sailCloth = GetComponent<Cloth>();

            if (sailAttachmentPoint == null)
                sailAttachmentPoint = transform;

            // Validation
            if (sailCloth == null)
            {
                Debug.LogError($"[SailClothPhysics] Brak Cloth component na {gameObject.name}!");
                enabled = false;
                return;
            }

            if (yachtRigidbody == null)
            {
                Debug.LogError($"[SailClothPhysics] Nie przypisano Yacht Rigidbody na {gameObject.name}!");
                enabled = false;
                return;
            }

            isInitialized = true;

            if (enableDebugLogs)
            {
                Debug.Log($"[SailClothPhysics] Zainicjalizowano {gameObject.name}");
                Debug.Log($"  - Sail Area: {sailArea}m²");
                Debug.Log($"  - Max Force: {maxForce}N");
                Debug.Log($"  - Force Multiplier: {forceMultiplier}");
            }
        }

        void FixedUpdate()
        {
            if (!isInitialized || Wind == null)
                return;

            Vector3 trueWindDir = Wind.GetWindDirection3D();
            float windSpeed = (float)Wind.WindSpeed;

            Vector3 apparentWind =
                trueWindDir * windSpeed - yachtRigidbody.linearVelocity;

            if (apparentWind.sqrMagnitude < 0.1f)
            {
                ResetForces();
                return;
            }

            float apparentSpeed = apparentWind.magnitude;
            Vector3 apparentDir = apparentWind.normalized;

            // === AERODYNAMIKA JAK W SAIL ===
            var result = SailAeroCalculator.Calculate(
                transform,
                yachtRigidbody,
                Wind,
                apparentDir,
                apparentSpeed,
                sailArea,
                6f,     // długość (albo parametr)
                2f,     // szerokość
                0f,
                1.5f,
                1.33f,
                0.9f,
                forceMultiplier
            );

            totalForce = result.force;

            // === CLOTH TYLKO WIZUALNIE ===
            if (applyWindToCloth)
            {
                sailCloth.externalAcceleration =
                    apparentDir * apparentSpeed * windForceMultiplier;
            }

            currentDriveForce =
                Vector3.Project(totalForce, yachtRigidbody.transform.forward);
            currentLateralForce =
                Vector3.Project(totalForce, yachtRigidbody.transform.right);
        }

        private void ResetForces()
        {
            totalForce = Vector3.zero;
            currentDriveForce = Vector3.zero;
            currentLateralForce = Vector3.zero;
        }

        /// <summary>
        /// Zwraca siłę napędową (do integracji z YachtPhysics)
        /// </summary>
        public float GetDriveForce()
        {
            if (yachtRigidbody == null)
                return 0f;

            return currentDriveForce.magnitude *
                   Mathf.Sign(Vector3.Dot(currentDriveForce, yachtRigidbody.transform.forward));
        }

        /// <summary>
        /// Zwraca siłę boczną (do obliczania przechyłu)
        /// </summary>
        public float GetLateralForce()
        {
            if (yachtRigidbody == null)
                return 0f;

            return currentLateralForce.magnitude *
                   Mathf.Sign(Vector3.Dot(currentLateralForce, yachtRigidbody.transform.right));
        }

        /// <summary>
        /// Zwraca całkowitą siłę (dla debugowania)
        /// </summary>
        public Vector3 GetTotalForce()
        {
            return totalForce;
        }

    }
}