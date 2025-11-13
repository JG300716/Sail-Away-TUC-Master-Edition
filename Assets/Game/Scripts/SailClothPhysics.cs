using UnityEngine;

namespace Game.Scripts
{
    /// <summary>
    /// Oblicza siły aerodynamiczne na podstawie Cloth żagla
    /// Uproszczona wersja - traktuje żagiel jako pojedynczą powierzchnię
    /// </summary>
    public class SailClothPhysics : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Cloth sailCloth;
        [SerializeField] private Rigidbody yachtRigidbody;
        [SerializeField] private Transform sailAttachmentPoint;
        
        [Header("Sail Parameters")]
        [SerializeField] private float sailArea = 20f; // Powierzchnia żagla [m²]
        
        [Header("Physics Parameters")]
        [SerializeField] private float airDensity = 1.225f; // kg/m³
        [SerializeField] private float liftCoefficient = 0.5f; // Współczynnik siły nośnej
        [SerializeField] private float dragCoefficient = 0.15f; // Współczynnik oporu
        [SerializeField] private float forceMultiplier = 0.8f; // Globalne wzmocnienie siły
        [SerializeField] private float maxForce = 3000f; // Maksymalna siła [N]
        
        [Header("Wind Application")]
        [SerializeField] private bool applyWindToCloth = true;
        [SerializeField] private float windForceMultiplier = 1.0f;
        
        [Header("Sail Orientation")]
        [SerializeField] private Vector3 sailNormalDirection = Vector3.forward; // Kierunek normalnej żagla w local space
        
        [Header("Debug")]
        [SerializeField] private bool showForceVectors = true;
        [SerializeField] private float forceVectorScale = 0.1f;
        [SerializeField] private bool enableDebugLogs = false;
        
        private WindManager Wind => WindManager.Instance;
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
    
            // Pobierz wiatr prawdziwy
            Vector3 windDirection = Wind.GetWindDirection3D();
            float windSpeed = (float)Wind.WindSpeed;
    
            if (windSpeed < 0.1f)
            {
                ResetForces();
                return;
            }
    
            // POPRAWIONE: Oblicz wiatr pozorny
            // Apparent wind = True wind + Boat wind (opposing boat movement)
            Vector3 trueWind = windDirection * windSpeed;
            Vector3 boatWind = -yachtRigidbody.linearVelocity; // Przeciwny kierunek do ruchu łódki
            Vector3 apparentWind = trueWind + boatWind;
    
            // DODANE: Ogranicz maksymalny apparent wind (safety)
            float maxApparentWind = 30f; // 30 m/s = ~108 km/h (realistyczne)
            if (apparentWind.magnitude > maxApparentWind)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[SailClothPhysics] Apparent wind clamped from {apparentWind.magnitude:F1} to {maxApparentWind}");
                apparentWind = apparentWind.normalized * maxApparentWind;
            }
    
            // Aplikuj wiatr na Cloth
            if (applyWindToCloth && sailCloth != null)
            {
                ApplyWindToCloth(apparentWind);
            }
            
            // Oblicz siły aerodynamiczne
            CalculateAerodynamicForces(apparentWind);
    
            // Aplikuj siły na jacht
            ApplyForcesToYacht();
        }

        /// <summary>
        /// Aplikuje siłę wiatru bezpośrednio na Cloth (dla trzepotania)
        /// </summary>
        private void ApplyWindToCloth(Vector3 apparentWind)
        {
            Vector3 windAcceleration = apparentWind * windForceMultiplier;
            sailCloth.externalAcceleration = windAcceleration;
            
            // Losowe podmuchy dla realizmu
            float gustStrength = Mathf.PerlinNoise(Time.time * 0.5f, 0) * 2f;
            sailCloth.randomAcceleration = Vector3.one * gustStrength;
        }

        /// <summary>
        /// Oblicza siły aerodynamiczne - uproszczona wersja
        /// Traktuje żagiel jako pojedynczą płaszczyznę
        /// </summary>
       private void CalculateAerodynamicForces(Vector3 apparentWind)
        {
            if (apparentWind.sqrMagnitude < 0.01f)
            {
                ResetForces();
                return;
            }
            
            // Normalna żagla w world space (bazowa)
            Vector3 baseSailNormal = transform.TransformDirection(sailNormalDirection).normalized;
            
            // KLUCZOWA ZMIANA: Sprawdź z której strony wieje wiatr
            float dotProduct = Vector3.Dot(apparentWind.normalized, baseSailNormal);
            
            // Automatycznie odwróć normalną jeśli wiatr przychodzi "z tyłu"
            Vector3 effectiveSailNormal;
            if (dotProduct < 0)
            {
                // Wiatr od drugiej strony - odwróć normalną
                effectiveSailNormal = -baseSailNormal;
                dotProduct = -dotProduct; // Teraz zawsze dodatni
            }
            else
            {
                // Wiatr od "normalnej" strony
                effectiveSailNormal = baseSailNormal;
            }
            
            // Debug: Pokaż którą stronę używamy
            if (enableDebugLogs && Time.frameCount % 120 == 0)
            {
                string side = dotProduct > 0 && effectiveSailNormal == baseSailNormal ? "RIGHT" : "LEFT";
                Debug.Log($"[SailClothPhysics] Sail side: {side}, Dot: {dotProduct:F3}");
            }
            
            // Żagiel generuje siłę gdy wiatr uderza w niego
            if (dotProduct > 0.01f)
            {
                // Dynamiczne ciśnienie: q = 0.5 * ρ * V²
                float dynamicPressure = 0.5f * airDensity * apparentWind.sqrMagnitude;
                
                // === SIŁA NOŚNA (LIFT) ===
                Vector3 crossProduct = Vector3.Cross(effectiveSailNormal, apparentWind);
                
                if (crossProduct.sqrMagnitude > 0.0001f)
                {
                    Vector3 liftDirection = Vector3.Cross(apparentWind, crossProduct).normalized;
                    
                    float liftMagnitude = dynamicPressure * sailArea * liftCoefficient * dotProduct * 0.5f;
                    Vector3 lift = liftDirection * liftMagnitude;
                    
                    // === SIŁA OPORU (DRAG) ===
                    float dragMagnitude = dynamicPressure * sailArea * dragCoefficient * dotProduct;
                    Vector3 drag = apparentWind.normalized * dragMagnitude;
                    
                    // Suma
                    totalForce = (lift + drag) * forceMultiplier;
                }
                else
                {
                    // Tylko drag
                    float dragMagnitude = dynamicPressure * sailArea * dragCoefficient * dotProduct;
                    totalForce = apparentWind.normalized * dragMagnitude * forceMultiplier;
                }
                
                // Clamp siły
                if (totalForce.magnitude > maxForce)
                {
                    if (enableDebugLogs && Time.frameCount % 60 == 0)
                    {
                        Debug.LogWarning($"[SailClothPhysics] Siła ograniczona! Było: {totalForce.magnitude:F0}N, Jest: {maxForce}N");
                    }
                    totalForce = totalForce.normalized * maxForce;
                }
            }
            else
            {
                totalForce = Vector3.zero;
            }
            
            // Rozdziel na komponenty
            Vector3 boatForward = yachtRigidbody.transform.forward;
            Vector3 boatRight = yachtRigidbody.transform.right;
            
            currentDriveForce = Vector3.Project(totalForce, boatForward);
            currentLateralForce = Vector3.Project(totalForce, boatRight);
            
            // Debug
            if (enableDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[SailClothPhysics] {gameObject.name}:");
                Debug.Log($"  Apparent Wind: {apparentWind.magnitude:F2} m/s");
                Debug.Log($"  Base Normal: {baseSailNormal}");
                Debug.Log($"  Effective Normal: {effectiveSailNormal}");
                Debug.Log($"  Dot Product: {dotProduct:F3}");
                Debug.Log($"  Total Force: {totalForce.magnitude:F2}N");
                Debug.Log($"  Drive: {currentDriveForce.magnitude:F2}N, Lateral: {currentLateralForce.magnitude:F2}N");
            }
        }

        /// <summary>
        /// Aplikuje obliczone siły na Rigidbody jachtu
        /// </summary>
        private void ApplyForcesToYacht()
        {
            if (totalForce.sqrMagnitude < 0.01f)
                return;
            
            Vector3 applicationPoint = sailAttachmentPoint.position;
           // yachtRigidbody.AddForceAtPosition(totalForce, applicationPoint, ForceMode.Force);
        }

        /// <summary>
        /// Resetuje wszystkie siły do zera
        /// </summary>
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
            
            return currentDriveForce.magnitude * Mathf.Sign(Vector3.Dot(currentDriveForce, yachtRigidbody.transform.forward));
        }

        /// <summary>
        /// Zwraca siłę boczną (do obliczania przechyłu)
        /// </summary>
        public float GetLateralForce()
        {
            if (yachtRigidbody == null) 
                return 0f;
            
            return currentLateralForce.magnitude * Mathf.Sign(Vector3.Dot(currentLateralForce, yachtRigidbody.transform.right));
        }

        /// <summary>
        /// Zwraca całkowitą siłę (dla debugowania)
        /// </summary>
        public Vector3 GetTotalForce()
        {
            return totalForce;
        }

        void OnDrawGizmos()
        {
            if (!showForceVectors || !Application.isPlaying || !isInitialized)
                return;
    
            Vector3 center = sailAttachmentPoint != null ? sailAttachmentPoint.position : transform.position;
            Vector3 baseSailNormal = transform.TransformDirection(sailNormalDirection).normalized;
    
            // Normalna bazowa (żółta)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(center, center + baseSailNormal * 2f);
            Gizmos.DrawSphere(center + baseSailNormal * 2f, 0.1f);
    
            // Odwrotna normalna (pomarańczowa) - druga strona żagla
            Gizmos.color = new Color(1f, 0.5f, 0f); // Pomarańczowy
            Gizmos.DrawLine(center, center - baseSailNormal * 2f);
            Gizmos.DrawSphere(center - baseSailNormal * 2f, 0.1f);
    
            // Siła napędowa (zielona)
            if (currentDriveForce.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(center, center + currentDriveForce * forceVectorScale);
                Gizmos.DrawSphere(center + currentDriveForce * forceVectorScale, 0.15f);
            }
    
            // Siła boczna (czerwona)
            if (currentLateralForce.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(center, center + currentLateralForce * forceVectorScale);
                Gizmos.DrawSphere(center + currentLateralForce * forceVectorScale, 0.15f);
            }
    
            // Całkowita siła (cyjan)
            if (totalForce.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(center, center + totalForce * forceVectorScale);
                Gizmos.DrawSphere(center + totalForce * forceVectorScale, 0.2f);
            }
        }

        // === DEBUG METHODS ===
        
        [ContextMenu("Test Initialization")]
        private void TestInitialization()
        {
            enableDebugLogs = true;
            InitializeComponents();
        }

        [ContextMenu("Log Current State")]
        private void LogCurrentState()
        {
            Debug.Log("=== SAIL CLOTH PHYSICS STATE ===");
            Debug.Log($"Initialized: {isInitialized}");
            Debug.Log($"Sail Area: {sailArea}m²");
            Debug.Log($"Max Force: {maxForce}N");
            Debug.Log($"Total Force: {totalForce.magnitude:F2}N");
            Debug.Log($"Drive Force: {GetDriveForce():F2}N");
            Debug.Log($"Lateral Force: {GetLateralForce():F2}N");
            
            if (Wind != null)
            {
                Debug.Log($"Wind Speed: {Wind.WindSpeed:F2} m/s");
                Debug.Log($"Wind Direction: {Wind.WindDegree:F0}°");
            }
            
            if (yachtRigidbody != null)
            {
                Debug.Log($"Yacht Velocity: {yachtRigidbody.linearVelocity.magnitude:F2} m/s");
            }
        }

        [ContextMenu("Adjust Sail Normal +90°")]
        private void RotateSailNormal()
        {
            sailNormalDirection = Quaternion.Euler(0, 90, 0) * sailNormalDirection;
            Debug.Log($"Sail Normal Direction: {sailNormalDirection}");
        }
    }
}