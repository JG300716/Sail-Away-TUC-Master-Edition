using UnityEngine;

namespace Game.Assets
{
    public class WindManager : MonoBehaviour
    {
        // --- Parametry wiatru ---
        [SerializeField] public float minWindSpeed = 0.0f; // minimalna prędkość wiatru [m/s]
        [SerializeField] public float maxWindSpeed = 15.0f; // maksymalna prędkość wiatru [m/s]
        [SerializeField] public double WindSpeed; // aktualna prędkość wiatru [m/s]
        [SerializeField] private double TargetWindSpeed; // docelowa prędkość wiatru [m/s]
        
        [SerializeField] public float minWindDirectionDeg = 0.0f; // minimalny kierunek wiatru [°]
        [SerializeField] public float maxWindDirectionDeg = 360.0f; // maksymalny kierunek wiatru [°]
        [SerializeField] public double WindDegree; // aktualny kąt wiatru względem północy [°]
        [SerializeField] private double TargetWindDegree; // docelowy kąt wiatru względem północy [°]

        [Header("Steady wind settings")]
        [SerializeField] public double steadyWindChangeInterval = 10.0; // czas między zmianami wiatru [s]
        [SerializeField] public double windSpeedStep = 0.1; // krok zmiany prędkości wiatru
        [SerializeField] public double windDirectionStep = 1.0; // krok zmiany kierunku wiatru [°]

        private double timeSinceLastChange = 0.0;
        public bool GenerateSteadyWindFlag = false; // flaga generowania wiatru stałego
        private bool UpdateSteadyWind = false;

        // --- Konstruktor ---
        public WindManager(double windSpeed = 5.0, double windDirectionDeg = 0.0)
        {
            WindSpeed = windSpeed;
            WindDegree = windDirectionDeg;
            GenerateSteadyWindFlag = false;
            UpdateSteadyWind = false;
        }

        // --- Ustawienia ręczne ---
        public void SetWindConditions(double windSpeed, double windDirectionDeg)
        {
            WindSpeed = Mathf.Clamp((float)windSpeed, minWindSpeed, maxWindSpeed);
            WindDegree = Mathf.Repeat((float)windDirectionDeg, 360f);
        }

        // --- Generowanie losowego wiatru ---
        public void GenerateRandomWind()
        {
            WindSpeed = UnityEngine.Random.Range(minWindSpeed, maxWindSpeed);
            WindDegree = UnityEngine.Random.Range(minWindDirectionDeg, maxWindDirectionDeg);
        }

        // --- Generowanie wiatru ze stałymi krokami ---
        public void GenerateSteadyWind(double deltaTime)
        {
            // Losowy cel wiatru
            TargetWindSpeed = UnityEngine.Random.Range(minWindSpeed, maxWindSpeed);
            TargetWindDegree = UnityEngine.Random.Range(minWindDirectionDeg, maxWindDirectionDeg);

            timeSinceLastChange = 0.0;
            GenerateSteadyWindFlag = false;
            UpdateSteadyWind = true;
        }

        public void UpdateSteadyWindConditions(double deltaTime)
        {
            timeSinceLastChange += deltaTime;
            if (timeSinceLastChange < steadyWindChangeInterval) return;
            
            // Stopniowe przybliżanie do celu z określonym krokiem
            WindSpeed = MoveTowards(WindSpeed, TargetWindSpeed, windSpeedStep);
            WindDegree = MoveTowardsAngle(WindDegree, TargetWindDegree, windDirectionStep);

            timeSinceLastChange = 0.0;
            
            UpdateSteadyWind = !(Mathf.Approximately((float)WindSpeed, (float)TargetWindSpeed) &&
                                     Mathf.Approximately((float)WindDegree, (float)TargetWindDegree));
        }
        
        // --- Reset wiatru ---
        public void ResetWind()
        {
            GenerateRandomWind();
            timeSinceLastChange = 0.0;
        }

        void Update()
        {
            if (GenerateSteadyWindFlag) GenerateSteadyWind(Time.deltaTime);
            if (UpdateSteadyWind) UpdateSteadyWindConditions(Time.deltaTime);
        }

        // --- Pomocnicze funkcje ---
        private double MoveTowards(double current, double target, double maxDelta)
        {
            if (current < target)
                return Mathf.Min((float)(current + maxDelta), (float)target);
            else
                return Mathf.Max((float)(current - maxDelta), (float)target);
        }

        private double MoveTowardsAngle(double current, double target, double maxDelta)
        {
            float delta = Mathf.DeltaAngle((float)current, (float)target);
            if (Mathf.Abs(delta) <= maxDelta)
                return target;
            return Mathf.Repeat((float)(current + Mathf.Sign(delta) * maxDelta), 360f);
        }
    }
}
