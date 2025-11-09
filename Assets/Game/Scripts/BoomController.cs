using UnityEngine;

namespace Game.Scripts
{
    public class BoomController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private YachtState yachtState;
        [SerializeField] private YachtPhysics yachtPhysics;

        [Header("Boom Transforms")]
        [SerializeField] private Transform boomGrot; // Transform bomu grota
        [SerializeField] private Transform boomFok; // Transform bomu foka (opcjonalny)

        [Header("Boom Settings")]
        [SerializeField] private float boomRotationSpeed = 3f; // prędkość obrotu bomu
        [SerializeField] private float maxBoomAngle = 85f; // maksymalny kąt bomu [°]
        [SerializeField] private float minBoomAngle = 5f; // minimalny kąt bomu (dead zone) [°]
        
        [Header("Boom Vang Integration")]
        [SerializeField] private BoomVangController vangController; // Kontroler obciągacza
        [SerializeField] private bool useVangLimits = true; // Czy obciągacz limituje kąt
        
        [Header("Automatic Trim (Opcjonalne)")]
        [SerializeField] private bool autoTrimGrot = false; // automatyczne ustawianie grota
        [SerializeField] private bool autoTrimFok = false; // automatyczne ustawianie foka
        [SerializeField] private float autoTrimMargin = 10f; // margines bezpieczeństwa od kąta wiatru [°]

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        private float currentBoomAngleGrot = 0f;
        private float currentBoomAngleFok = 0f;
        private bool initialized = false;
        private WindManager Wind => WindManager.Instance;

        void Start()
        {
            initialized = yachtState != null && yachtPhysics != null && Wind != null;
            
            if (!initialized)
            {
                Debug.LogError("BoomController: Missing references!");
                return;
            }

            // Ustaw początkowe pozycje
            if (boomGrot != null)
            {
                currentBoomAngleGrot = yachtPhysics.SheetAngleGrot;
                ApplyBoomRotation(boomGrot, currentBoomAngleGrot);
            }

            if (boomFok != null)
            {
                currentBoomAngleFok = yachtPhysics.SheetAngleFok;
                ApplyBoomRotation(boomFok, currentBoomAngleFok);
            }
        }

        void Update()
        {
            if (!initialized) return;

            // Aktualizuj boom grota
            if (boomGrot != null && IsSailSet(YachtSailState.Grot_Only))
            {
                float targetAngle = autoTrimGrot 
                    ? CalculateOptimalBoomAngle(true) 
                    : yachtPhysics.SheetAngleGrot;

                UpdateBoom(ref currentBoomAngleGrot, targetAngle, boomGrot);
            }

            // Aktualizuj boom foka
            if (boomFok != null && IsSailSet(YachtSailState.Fok_Only))
            {
                float targetAngle = autoTrimFok 
                    ? CalculateOptimalBoomAngle(false) 
                    : yachtPhysics.SheetAngleFok;

                UpdateBoom(ref currentBoomAngleFok, targetAngle, boomFok);
            }
        }

        private void UpdateBoom(ref float currentAngle, float targetAngle, Transform boom)
        {
            // Płynne przejście do docelowego kąta
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, boomRotationSpeed * Time.deltaTime);
            
            // Ograniczenie kąta
            currentAngle = Mathf.Clamp(currentAngle, -maxBoomAngle, maxBoomAngle);
            
            // Aplikacja rotacji
            ApplyBoomRotation(boom, currentAngle);
        }

        private void ApplyBoomRotation(Transform boom, float angle)
        {
            // Boom obraca się wokół osi Y (vertical)
            // Dodatni kąt = w prawo (z perspektywy od tyłu jachtu)
            // Ujemny kąt = w lewo
            boom.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        private float CalculateOptimalBoomAngle(bool isGrot)
        {
            // Oblicz kąt wiatru pozornego względem jachtu
            double relativeWindDeg = (Wind.WindDegree - yachtState.Deg_from_north + 360.0) % 360.0;
            
            // Normalizuj do -180° do 180° (ujemne = wiatr z lewej, dodatnie = z prawej)
            if (relativeWindDeg > 180)
                relativeWindDeg -= 360;

            float windAngle = (float)relativeWindDeg;
            
            // Oblicz optymalny kąt bomu
            // Boom powinien być ustawiony pod kątem ~połowy kąta wiatru, z marginesem
            float optimalAngle = windAngle * 0.5f;
            
            // Dodaj margines bezpieczeństwa (żeby żagiel nie trzepotał)
            if (optimalAngle > 0)
                optimalAngle = Mathf.Max(optimalAngle, minBoomAngle + autoTrimMargin);
            else
                optimalAngle = Mathf.Min(optimalAngle, -(minBoomAngle + autoTrimMargin));

            // Ograniczenie do maksymalnego kąta
            optimalAngle = Mathf.Clamp(optimalAngle, -maxBoomAngle, maxBoomAngle);

            // Dla foka, zastosuj mniejszy kąt (fok jest bliżej wiatru)
            if (!isGrot)
                optimalAngle *= 0.7f;

            return optimalAngle;
        }

        private bool IsSailSet(YachtSailState checkSail)
        {
            YachtSailState currentState = yachtState.SailState;
            
            if (checkSail == YachtSailState.Grot_Only)
            {
                return currentState == YachtSailState.Grot_Only || 
                       currentState == YachtSailState.Grot_and_Fok;
            }
            else if (checkSail == YachtSailState.Fok_Only)
            {
                return currentState == YachtSailState.Fok_Only || 
                       currentState == YachtSailState.Grot_and_Fok;
            }
            
            return false;
        }

        // Funkcja publiczna do ręcznego ustawienia kąta bomu
        public void SetBoomAngle(bool isGrot, float angle)
        {
            angle = Mathf.Clamp(angle, -maxBoomAngle, maxBoomAngle);
            
            if (isGrot && boomGrot != null)
            {
                currentBoomAngleGrot = angle;
                yachtPhysics.SheetAngleGrot = angle;
            }
            else if (!isGrot && boomFok != null)
            {
                currentBoomAngleFok = angle;
                yachtPhysics.SheetAngleFok = angle;
            }
        }

        // Funkcja do animacji zawijania żagla
        public void AnimateSailFurl(bool isGrot, System.Action onComplete = null)
        {
            StartCoroutine(FurlSailCoroutine(isGrot, onComplete));
        }

        private System.Collections.IEnumerator FurlSailCoroutine(bool isGrot, System.Action onComplete)
        {
            float startAngle = isGrot ? currentBoomAngleGrot : currentBoomAngleFok;
            float elapsed = 0f;
            float duration = 2f; // czas zawijania w sekundach

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float angle = Mathf.Lerp(startAngle, 0f, t);

                if (isGrot)
                {
                    currentBoomAngleGrot = angle;
                    if (boomGrot != null)
                        ApplyBoomRotation(boomGrot, angle);
                }
                else
                {
                    currentBoomAngleFok = angle;
                    if (boomFok != null)
                        ApplyBoomRotation(boomFok, angle);
                }

                yield return null;
            }

            onComplete?.Invoke();
        }

        void OnDrawGizmos()
        {
            if (!showDebugGizmos || !initialized || !Application.isPlaying) return;

            // Rysuj linie pokazujące pozycję bomu
            if (boomGrot != null && IsSailSet(YachtSailState.Grot_Only))
            {
                Gizmos.color = Color.green;
                Vector3 boomDirection = boomGrot.right; // kierunek bomu
                Gizmos.DrawLine(boomGrot.position, boomGrot.position + boomDirection * 5f);
                Gizmos.DrawSphere(boomGrot.position + boomDirection * 5f, 0.2f);
            }

            if (boomFok != null && IsSailSet(YachtSailState.Fok_Only))
            {
                Gizmos.color = Color.cyan;
                Vector3 boomDirection = boomFok.right;
                Gizmos.DrawLine(boomFok.position, boomFok.position + boomDirection * 3f);
                Gizmos.DrawSphere(boomFok.position + boomDirection * 3f, 0.15f);
            }

            // Rysuj kierunek optymalnego ustawienia
            if (autoTrimGrot || autoTrimFok)
            {
                double relativeWindDeg = (Wind.WindDegree - yachtState.Deg_from_north + 360.0) % 360.0;
                if (relativeWindDeg > 180) relativeWindDeg -= 360;
                
                float optimalAngle = (float)relativeWindDeg * 0.5f;
                Quaternion optimalRotation = Quaternion.Euler(0f, optimalAngle, 0f);
                Vector3 optimalDirection = optimalRotation * Vector3.right;

                Gizmos.color = Color.yellow;
                Vector3 startPos = transform.position + Vector3.up * 2f;
                Gizmos.DrawLine(startPos, startPos + optimalDirection * 4f);
            }
        }

        // Funkcja pomocnicza do zwracania aktualnego kąta bomu
        public float GetBoomAngle(bool isGrot)
        {
            return isGrot ? currentBoomAngleGrot : currentBoomAngleFok;
        }
    }
}