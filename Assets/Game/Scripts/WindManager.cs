using UnityEngine;

namespace Game.Scripts
{
    /// <summary>
    /// Wind Manager - Singleton
    /// Zarządza wiatrem w symulacji jachtu
    /// Dostępny globalnie przez WindManager.Instance
    /// </summary>
    public class WindManager : MonoBehaviour
    {
        #region Singleton
        
        // Statyczna instancja - dostępna z każdego miejsca w kodzie
        public static WindManager Instance { get; private set; }

        void Awake()
        {
            // Sprawdź czy już istnieje instancja
            if (Instance != null && Instance != this)
            {
                // Jeśli tak, zniszcz ten duplikat
                Debug.LogWarning("Duplicate WindManager found! Destroying...");
                Destroy(this.gameObject);
                return;
            }

            // Ustaw tę instancję jako główną
            Instance = this;

            // Opcjonalnie: zachowaj przez zmiany scen
            // Odkomentuj jeśli WindManager ma przetrwać loading sceny
            // DontDestroyOnLoad(this.gameObject);

            Debug.Log("WindManager Singleton initialized");
        }

        void OnDestroy()
        {
            // Wyczyść instancję gdy obiekt jest niszczony
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Wind Parameters

        // --- Parametry wiatru ---
        [Header("Wind Speed Settings")]
        [SerializeField] public float minWindSpeed = 0.0f; // minimalna prędkość wiatru [m/s]
        [SerializeField] public float maxWindSpeed = 15.0f; // maksymalna prędkość wiatru [m/s]
        [SerializeField] public double WindSpeed; // aktualna prędkość wiatru [m/s]
        [SerializeField] private double TargetWindSpeed; // docelowa prędkość wiatru [m/s]
        
        [Header("Wind Direction Settings")]
        [SerializeField] public float minWindDirectionDeg = 0.0f; // minimalny kierunek wiatru [°]
        [SerializeField] public float maxWindDirectionDeg = 360.0f; // maksymalny kierunek wiatru [°]
        [SerializeField] public double WindDegree; // aktualny kąt wiatru względem północy [°]
        [SerializeField] private double TargetWindDegree; // docelowy kąt wiatru względem północy [°]

        [Header("Steady Wind Settings")]
        [SerializeField] public double steadyWindChangeInterval = 10.0; // czas między zmianami wiatru [s]
        [SerializeField] public double windSpeedStep = 0.1; // krok zmiany prędkości wiatru
        [SerializeField] public double windDirectionStep = 1.0; // krok zmiany kierunku wiatru [°]

        private double timeSinceLastChange = 0.0;
        public bool GenerateSteadyWindFlag = false; // flaga generowania wiatru stałego
        private bool UpdateSteadyWind = false;

        #endregion

        #region Initialization

        void Start()
        {
            // Ustaw początkowe wartości
            if (WindSpeed == 0)
            {
                WindSpeed = 5.0; // Domyślna prędkość wiatru
            }

            if (GenerateSteadyWindFlag)
            {
                GenerateSteadyWind(0);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Ustawia warunki wiatrowe ręcznie
        /// </summary>
        public void SetWindConditions(double windSpeed, double windDirectionDeg)
        {
            WindSpeed = Mathf.Clamp((float)windSpeed, minWindSpeed, maxWindSpeed);
            WindDegree = Mathf.Repeat((float)windDirectionDeg, 360f);
        }

        /// <summary>
        /// Generuje losowy wiatr natychmiast
        /// </summary>
        public void GenerateRandomWind()
        {
            WindSpeed = Random.Range(minWindSpeed, maxWindSpeed);
            WindDegree = Random.Range(minWindDirectionDeg, maxWindDirectionDeg);
            
            Debug.Log($"Random wind generated: Speed={WindSpeed:F1}m/s, Direction={WindDegree:F0}°");
        }

        /// <summary>
        /// Rozpoczyna generowanie stopniowo zmieniającego się wiatru
        /// </summary>
        public void GenerateSteadyWind(double deltaTime)
        {
            // Losowy cel wiatru
            TargetWindSpeed = Random.Range(minWindSpeed, maxWindSpeed);
            TargetWindDegree = Random.Range(minWindDirectionDeg, maxWindDirectionDeg);

            timeSinceLastChange = 0.0;
            GenerateSteadyWindFlag = false;
            UpdateSteadyWind = true;

            Debug.Log($"Steady wind target set: Speed={TargetWindSpeed:F1}m/s, Direction={TargetWindDegree:F0}°");
        }

        /// <summary>
        /// Resetuje wiatr do losowych wartości
        /// </summary>
        public void ResetWind()
        {
            GenerateRandomWind();
            timeSinceLastChange = 0.0;
        }

        /// <summary>
        /// Pobiera wektor kierunku wiatru (2D)
        /// </summary>
        public Vector2 GetWindDirection2D()
        {
            float angleRad = (float)WindDegree * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
        }

        /// <summary>
        /// Pobiera wektor kierunku wiatru (3D, XZ plane)
        /// </summary>
        public Vector3 GetWindDirection3D()
        {
            float angleRad = (float)WindDegree * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad));
        }

        /// <summary>
        /// Pobiera wektor wiatru z prędkością (siła wiatru)
        /// </summary>
        public Vector3 GetWindForce()
        {
            return GetWindDirection3D() * (float)WindSpeed;
        }

        #endregion

        #region Update Loop

        void Update()
        {
            if (GenerateSteadyWindFlag)
            {
                GenerateSteadyWind(Time.deltaTime);
            }

            if (UpdateSteadyWind)
            {
                UpdateSteadyWindConditions(Time.deltaTime);
            }
        }

        /// <summary>
        /// Aktualizuje wiatr stopniowo w kierunku docelowych wartości
        /// </summary>
        public void UpdateSteadyWindConditions(double deltaTime)
        {
            timeSinceLastChange += deltaTime;
            
            if (timeSinceLastChange < steadyWindChangeInterval)
            {
                return;
            }
            
            // Stopniowe przybliżanie do celu z określonym krokiem
            WindSpeed = MoveTowards(WindSpeed, TargetWindSpeed, windSpeedStep);
            WindDegree = MoveTowardsAngle(WindDegree, TargetWindDegree, windDirectionStep);

            timeSinceLastChange = 0.0;
            
            // Sprawdź czy osiągnięto cel
            bool speedReached = Mathf.Approximately((float)WindSpeed, (float)TargetWindSpeed);
            bool directionReached = Mathf.Approximately((float)WindDegree, (float)TargetWindDegree);
            
            UpdateSteadyWind = !(speedReached && directionReached);
            
            // Jeśli osiągnięto cel, wygeneruj nowy
            if (!UpdateSteadyWind)
            {
                GenerateSteadyWindFlag = true;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Przesuwa wartość w kierunku celu o maksymalny krok
        /// </summary>
        private double MoveTowards(double current, double target, double maxDelta)
        {
            if (current < target)
            {
                return Mathf.Min((float)(current + maxDelta), (float)target);
            }
            else
            {
                return Mathf.Max((float)(current - maxDelta), (float)target);
            }
        }

        /// <summary>
        /// Przesuwa kąt w kierunku celu uwzględniając wrap-around 360°
        /// </summary>
        private double MoveTowardsAngle(double current, double target, double maxDelta)
        {
            float delta = Mathf.DeltaAngle((float)current, (float)target);
            
            if (Mathf.Abs(delta) <= maxDelta)
            {
                return target;
            }
            
            return Mathf.Repeat((float)(current + Mathf.Sign(delta) * maxDelta), 360f);
        }

        #endregion
    }
}