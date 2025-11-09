using UnityEngine;

namespace Game.Scripts
{
    /// <summary>
    /// Kontroler obciągacza bomu (boom vang) - naciąga/luzuje liny
    /// Wpływa na napięcie żagla i kąt bomu
    /// </summary>
    public class BoomVangController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform boomTransform; // Główny transform bomu (pivot)
        [SerializeField] private Transform vangTransform; // Obciągacz (mesh)
        [SerializeField] private LineRenderer vangLine; // Linka wizualna (opcjonalna)
        [SerializeField] private SailSkeletonController sailController; // Kontroler żagla
        [SerializeField] private YachtPhysics yachtPhysics; // Fizyka jachtu

        [Header("Vang Settings")]
        [SerializeField] private float vangTension = 0.5f; // Napięcie 0-1 (0=luźne, 1=napięte)
        [SerializeField] private float minVangLength = 1.5f; // Minimalna długość linki [m]
        [SerializeField] private float maxVangLength = 3.5f; // Maksymalna długość linki [m]
        [SerializeField] private float vangAdjustSpeed = 0.2f; // Prędkość zmiany napięcia

        [Header("Boom Angle Limits (przez obciągacz)")]
        [SerializeField] private bool limitBoomAngle = true; // Czy obciągacz limituje kąt
        [SerializeField] private float maxBoomAngleWhenTight = 45f; // Max kąt gdy napięty
        [SerializeField] private float maxBoomAngleWhenLoose = 85f; // Max kąt gdy luźny

        [Header("Visual Settings")]
        [SerializeField] private Transform vangAttachPoint; // Punkt na kadłubie gdzie linka się łączy
        [SerializeField] private Color lineColorTight = Color.red; // Kolor gdy napięte
        [SerializeField] private Color lineColorLoose = Color.green; // Kolor gdy luźne
        [SerializeField] private float lineWidth = 0.05f; // Grubość linii

        [Header("Auto-adjust")]
        [SerializeField] private bool autoAdjustByWind = false; // Automatyczne dostosowanie do wiatru
        [SerializeField] private float autoAdjustSensitivity = 0.5f;

        private float currentVangLength;
        private bool initialized = false;
        private WindManager Wind => WindManager.Instance;

        void Start()
        {
            initialized = boomTransform != null;

            if (!initialized)
            {
                Debug.LogError("BoomVangController: Missing boom transform!");
                return;
            }

            // Ustaw początkową długość linki
            currentVangLength = Mathf.Lerp(maxVangLength, minVangLength, vangTension);

            // Setup wizualizacji linki
            if (vangLine != null)
            {
                SetupVangLine();
            }
            else if (vangTransform != null)
            {
                // Jeśli nie ma LineRenderer, stwórz go
                vangLine = gameObject.AddComponent<LineRenderer>();
                SetupVangLine();
            }
        }

        void Update()
        {
            if (!initialized) return;

            // Input od gracza (można też kontrolować przez UI)
            HandleVangInput();

            // Auto-dostosowanie do wiatru
            if (autoAdjustByWind && yachtPhysics != null)
            {
                AutoAdjustVangByWind();
            }

            // Aktualizuj efekty napięcia
            ApplyVangEffects();

            // Aktualizuj wizualizację
            UpdateVangVisualization();
        }

        private void HandleVangInput()
        {
            // Klawisze do kontroli obciągacza
            // R = zaciągnij (tighten), F = poluzuj (loosen)
            
            if (Input.GetKey(KeyCode.R))
            {
                // Zaciągaj obciągacz
                vangTension += vangAdjustSpeed * Time.deltaTime;
                vangTension = Mathf.Clamp01(vangTension);
            }

            if (Input.GetKey(KeyCode.F))
            {
                // Luzuj obciągacz
                vangTension -= vangAdjustSpeed * Time.deltaTime;
                vangTension = Mathf.Clamp01(vangTension);
            }

            // Aktualizuj długość linki
            float targetLength = Mathf.Lerp(maxVangLength, minVangLength, vangTension);
            currentVangLength = Mathf.Lerp(currentVangLength, targetLength, Time.deltaTime * 2f);
        }

        private void AutoAdjustVangByWind()
        {
            // Automatyczne dostosowanie napięcia do siły wiatru
            // Silniejszy wiatr = większe napięcie
            
            if (Wind == null) return;

            float windSpeed = (float)Wind.WindSpeed;
            float targetTension = Mathf.Clamp01((windSpeed / 15f) * autoAdjustSensitivity);
            
            vangTension = Mathf.Lerp(vangTension, targetTension, Time.deltaTime * 0.5f);
        }

        private void ApplyVangEffects()
        {
            // 1. Wpływ na napięcie żagla
            if (sailController != null)
            {
                // Im bardziej napięty obciągacz, tym bardziej napięty żagiel
                sailController.SetSailTension(vangTension);
                
                // Wpływ na trzepot - napięty żagiel mniej trzepocze
                float flappingMultiplier = Mathf.Lerp(1.5f, 0.3f, vangTension);
                sailController.flappingIntensity = sailController.flappingIntensity * flappingMultiplier;
            }

            // 2. Limitowanie kąta bomu (fizyczna blokada przez linkę)
            if (limitBoomAngle && boomTransform != null)
            {
                float currentBoomAngle = boomTransform.localEulerAngles.y;
                
                // Normalizuj kąt do -180/180
                if (currentBoomAngle > 180) currentBoomAngle -= 360;

                // Oblicz maksymalny dozwolony kąt na podstawie napięcia
                float maxAllowedAngle = Mathf.Lerp(
                    maxBoomAngleWhenLoose, 
                    maxBoomAngleWhenTight, 
                    vangTension
                );

                // Ogranicz kąt jeśli przekroczono
                if (Mathf.Abs(currentBoomAngle) > maxAllowedAngle)
                {
                    float clampedAngle = Mathf.Clamp(
                        currentBoomAngle, 
                        -maxAllowedAngle, 
                        maxAllowedAngle
                    );
                    boomTransform.localEulerAngles = new Vector3(0, clampedAngle, 0);
                }
            }

            // 3. Pozycja obciągacza (mesh) - opcjonalna animacja
            if (vangTransform != null)
            {
                // Obciągacz może lekko "podciągać" się gdy napięty
                // (wizualny efekt - opcjonalny)
                Vector3 basePos = vangTransform.localPosition;
                basePos.y = Mathf.Lerp(-0.2f, 0.1f, vangTension);
                vangTransform.localPosition = basePos;
            }
        }

        private void SetupVangLine()
        {
            if (vangLine == null) return;

            vangLine.positionCount = 2;
            vangLine.startWidth = lineWidth;
            vangLine.endWidth = lineWidth;
            vangLine.material = new Material(Shader.Find("Sprites/Default"));
            vangLine.startColor = lineColorLoose;
            vangLine.endColor = lineColorLoose;
            vangLine.useWorldSpace = true;
        }

        private void UpdateVangVisualization()
        {
            if (vangLine == null) return;

            // Punkt początkowy - boom (środek bomu lub gdzie obciągacz)
            Vector3 startPoint = vangTransform != null 
                ? vangTransform.position 
                : boomTransform.position;

            // Punkt końcowy - kadłub (gdzie linka się przyczepia)
            Vector3 endPoint = vangAttachPoint != null 
                ? vangAttachPoint.position 
                : boomTransform.position + Vector3.down * currentVangLength;

            // Ustaw pozycje linii
            vangLine.SetPosition(0, startPoint);
            vangLine.SetPosition(1, endPoint);

            // Zmień kolor na podstawie napięcia
            Color lineColor = Color.Lerp(lineColorLoose, lineColorTight, vangTension);
            vangLine.startColor = lineColor;
            vangLine.endColor = lineColor;

            // Opcjonalnie: zmień grubość linii
            float lineThickness = Mathf.Lerp(lineWidth * 0.7f, lineWidth * 1.3f, vangTension);
            vangLine.startWidth = lineThickness;
            vangLine.endWidth = lineThickness;
        }

        // Funkcje publiczne API

        /// <summary>
        /// Ustaw napięcie obciągacza (0-1)
        /// </summary>
        public void SetVangTension(float tension)
        {
            vangTension = Mathf.Clamp01(tension);
        }

        /// <summary>
        /// Pobierz aktualne napięcie obciągacza (0-1)
        /// </summary>
        public float GetVangTension()
        {
            return vangTension;
        }

        /// <summary>
        /// Maksymalny kąt bomu dozwolony przez obecne napięcie
        /// </summary>
        public float GetMaxAllowedBoomAngle()
        {
            return Mathf.Lerp(maxBoomAngleWhenLoose, maxBoomAngleWhenTight, vangTension);
        }

        /// <summary>
        /// Czy obciągacz blokuje dalszy ruch bomu?
        /// </summary>
        public bool IsVangLimiting()
        {
            if (!limitBoomAngle || boomTransform == null) return false;

            float currentAngle = boomTransform.localEulerAngles.y;
            if (currentAngle > 180) currentAngle -= 360;

            float maxAngle = GetMaxAllowedBoomAngle();
            return Mathf.Abs(currentAngle) >= maxAngle - 1f; // 1° tolerancja
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !initialized) return;

            // Rysuj linkę jako Gizmo jeśli nie ma LineRenderer
            if (vangLine == null && vangTransform != null && vangAttachPoint != null)
            {
                Color gizmoColor = Color.Lerp(lineColorLoose, lineColorTight, vangTension);
                Gizmos.color = gizmoColor;
                Gizmos.DrawLine(vangTransform.position, vangAttachPoint.position);
                
                // Punkty końcowe
                Gizmos.DrawSphere(vangTransform.position, 0.1f);
                Gizmos.DrawSphere(vangAttachPoint.position, 0.1f);
            }

            // Pokaż maksymalny dozwolony kąt bomu
            if (limitBoomAngle && boomTransform != null)
            {
                float maxAngle = GetMaxAllowedBoomAngle();
                
                Gizmos.color = Color.yellow;
                Vector3 pos = boomTransform.position;
                
                // Linia pokazująca limit kąta (prawo)
                Quaternion rightLimit = Quaternion.Euler(0, maxAngle, 0);
                Vector3 rightDir = rightLimit * Vector3.forward;
                Gizmos.DrawLine(pos, pos + rightDir * 5f);
                
                // Linia pokazująca limit kąta (lewo)
                Quaternion leftLimit = Quaternion.Euler(0, -maxAngle, 0);
                Vector3 leftDir = leftLimit * Vector3.forward;
                Gizmos.DrawLine(pos, pos + leftDir * 5f);
            }
        }

        // Debug info
        void OnGUI()
        {
            if (!Application.isPlaying || !initialized) return;

            // Opcjonalne UI debug
            if (Input.GetKey(KeyCode.LeftShift))
            {
                GUI.Box(new Rect(10, Screen.height - 150, 250, 140), "Boom Vang Debug");
                GUI.Label(new Rect(20, Screen.height - 125, 230, 20), 
                    $"Napięcie: {vangTension:F2} ({(vangTension > 0.7f ? "Napięte" : vangTension > 0.3f ? "Średnie" : "Luźne")})");
                GUI.Label(new Rect(20, Screen.height - 105, 230, 20), 
                    $"Długość linki: {currentVangLength:F2}m");
                GUI.Label(new Rect(20, Screen.height - 85, 230, 20), 
                    $"Max kąt bomu: {GetMaxAllowedBoomAngle():F0}°");
                GUI.Label(new Rect(20, Screen.height - 65, 230, 20), 
                    $"Limituje: {(IsVangLimiting() ? "TAK" : "NIE")}");
                GUI.Label(new Rect(20, Screen.height - 45, 230, 20), 
                    "R = zaciągnij, F = poluzuj");
                GUI.Label(new Rect(20, Screen.height - 25, 230, 20), 
                    "Shift = pokaż debug");
            }
        }
    }
}