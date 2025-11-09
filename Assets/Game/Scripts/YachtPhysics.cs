using UnityEngine;
using System;

namespace Game.Scripts
{
    public class YachtPhysics : MonoBehaviour
    {
        // --- Parametry fizyczne powietrza i wody ---
        [SerializeField] public double RhoAir = 1.225; // gęstość powietrza [kg/m³]
        [SerializeField] public double RhoWater = 1025; // gęstość wody [kg/m³]

        // --- Parametry jachtu ---
        [SerializeField] public double BoatMass = 2700.0; // masa jachtu [kg]
        [SerializeField] public double WettedArea = 10.0; // powierzchnia zmoczona kadłuba [m²]
        [SerializeField] public double DragCoeffHull = 0.009; // opór kadłuba [-]
        [SerializeField] public double MaxSpeed = 15.0; // maksymalna prędkość jachtu [m/s]

        // --- Parametry przechyłu ---
        [Header("Heel (Przechył)")]
        [SerializeField] public float heelAngle = 0f; // aktualny kąt przechyłu [°]
        [SerializeField] public float maxHeelAngle = 35f; // maksymalny kąt przechyłu [°]
        [SerializeField] public float heelSpeed = 2f; // prędkość zmiany przechyłu
        [SerializeField] public float heelDamping = 3f; // tłumienie przechyłu
        
        // --- Parametry kołysania (pitch/roll) ---
        [Header("Wave Motion")]
        [SerializeField] public float pitchAmplitude = 2f; // amplituda kołysania przód-tył [°]
        [SerializeField] public float pitchFrequency = 0.5f; // częstotliwość kołysania
        [SerializeField] public float rollWaveAmplitude = 1f; // amplituda bocznego kołysania [°]
        [SerializeField] public float rollWaveFrequency = 0.3f;

        // --- Parametry aerodynamiczne żagli ---
        [SerializeField] public double SailAreaGrot = 20.0; // powierzchnia grota [m²]
        [SerializeField] public double SailAreaFok = 15.0; // powierzchnia foka [m²]

        // --- Charakterystyki aerodynamiczne żagli ---
        [SerializeField] public double ClAlpha = 0.1; // przyrost siły nośnej na radian
        [SerializeField] public double ClMax = 1.2; // maksymalny Cl przed przeciągnięciem
        [SerializeField] public double Cd0 = 0.01; // bazowy współczynnik oporu
        [SerializeField] public double K = 0.05; // współczynnik indukowanego oporu

        // --- Kąty ustawienia żagli względem osi jachtu ---
        [SerializeField] public float SheetAngleGrot = 0.0f; // kąt grota [°]
        [SerializeField] public float SheetAngleFok = 0.0f; // kąt foka [°]
        
        [SerializeField] private Transform boatModel; // model jachtu do animacji przechyłu
        
        private bool Initialized = false;
        private float waveTime = 0f;
        private double lateralForce = 0.0; // siła boczna do obliczania przechyłu

        private WindManager Wind => WindManager.Instance;
        
        void Start()
        {
            Initialized = Wind != null;
            if (boatModel == null)
                boatModel = transform;
        }

        public double ComputeAcceleration(double boatSpeed, double boatHeadingDeg, YachtSailState sailState)
        {
            if (!Initialized) return 0.0;
            
            // Obliczanie wiatru pozornego (Apparent Wind)
            double windDeg = Wind.WindDegree;
            double V_true = Wind.WindSpeed;
            
            // Kąt między wiatrem a łódką (normalizacja do 0-360°)
            double relativeWindDeg = (windDeg - boatHeadingDeg + 360.0) % 360.0;
            
            // Konwersja na radiany
            double relativeWindRad = DegToRad(relativeWindDeg);
            
            // Komponenty wiatru pozornego
            double vx = V_true * Math.Cos(relativeWindRad) - boatSpeed; // komponent wzdłuż osi jachtu
            double vy = V_true * Math.Sin(relativeWindRad); // komponent boczny
            double Va = Math.Sqrt(vx * vx + vy * vy); // prędkość pozornego wiatru
            double betaAW = Math.Atan2(vy, -vx); // kąt pozornego wiatru względem osi jachtu

            // Siła aerodynamiczna dla grota i foka
            bool grotSet = sailState == YachtSailState.Grot_Only || sailState == YachtSailState.Grot_and_Fok;
            bool fokSet = sailState == YachtSailState.Fok_Only || sailState == YachtSailState.Grot_and_Fok;

            double FdriveGrot = 0.0, FlateralGrot = 0.0;
            double FdriveFok = 0.0, FlateralFok = 0.0;

            if (grotSet)
                ComputeSailForces(SailAreaGrot, SheetAngleGrot, Va, betaAW, out FdriveGrot, out FlateralGrot);
            
            if (fokSet)
                ComputeSailForces(SailAreaFok, SheetAngleFok, Va, betaAW, out FdriveFok, out FlateralFok);

            double Fdrive = FdriveGrot + FdriveFok;
            lateralForce = FlateralGrot + FlateralFok;

            // Opór hydrodynamiczny kadłuba (zawsze działa przeciwnie do ruchu)
            double R_hull = 0.5 * RhoWater * WettedArea * DragCoeffHull * boatSpeed * Math.Abs(boatSpeed);
            if (boatSpeed > 0) R_hull = -R_hull; // opór przeciwny do kierunku ruchu
            else if (boatSpeed < 0) R_hull = Math.Abs(R_hull);

            // Przyspieszenie netto
            double acceleration = (Fdrive + R_hull) / BoatMass;

            // Ograniczenie maksymalnej prędkości
            if (boatSpeed > MaxSpeed && acceleration > 0)
                acceleration = 0;
            if (boatSpeed < -MaxSpeed * 0.3 && acceleration < 0)
                acceleration = 0;

            return acceleration;
        }

        private void ComputeSailForces(double area, double sheetAngleDeg, double Va, double betaAW, 
                                       out double Fdrive, out double Flateral)
        {
            double sheetRad = DegToRad(sheetAngleDeg);
            double alpha = betaAW - sheetRad; // kąt natarcia żagla

            // Aerodynamiczne współczynniki żagla
            double Cl = ClAlpha * alpha;
            Cl = Math.Clamp(Cl, -ClMax, ClMax);
            double Cd = Cd0 + K * Cl * Cl;

            // Siły
            double q = 0.5 * RhoAir * Va * Va; // ciśnienie dynamiczne
            double L = q * area * Cl; // siła nośna
            double D = q * area * Cd; // siła oporu

            // Rozkład sił na kierunek ruchu (wzdłuż) i boczny
            Fdrive = L * Math.Sin(betaAW) - D * Math.Cos(betaAW);
            Flateral = L * Math.Cos(betaAW) + D * Math.Sin(betaAW);
        }

        public void UpdateHeelAndMotion(float deltaTime, double boatSpeed)
        {
            if (boatModel == null) return;

            // Obliczanie docelowego przechyłu na podstawie siły bocznej i prędkości
            float targetHeel = 0f;
            if (boatSpeed > 0.5) // tylko gdy jacht się porusza
            {
                // Siła boczna normalizowana przez prędkość łódki i powierzchnię żagli
                double totalSailArea = SailAreaGrot + SailAreaFok;
                double heelFactor = (lateralForce / (boatSpeed * totalSailArea)) * 10.0;
                targetHeel = (float)Math.Clamp(heelFactor * maxHeelAngle, -maxHeelAngle, maxHeelAngle);
            }

            // Płynne przejście do docelowego przechyłu
            heelAngle = Mathf.Lerp(heelAngle, targetHeel, heelSpeed * deltaTime);
            
            // Tłumienie - powrót do poziomu gdy brak siły
            if (Mathf.Abs(targetHeel) < 0.1f)
                heelAngle = Mathf.Lerp(heelAngle, 0f, heelDamping * deltaTime);

            // Efekt falowania (pitch i roll)
            waveTime += deltaTime;
            float pitch = Mathf.Sin(waveTime * pitchFrequency * 2f * Mathf.PI) * pitchAmplitude;
            float rollWave = Mathf.Sin(waveTime * rollWaveFrequency * 2f * Mathf.PI) * rollWaveAmplitude;

            // Zależność amplitudy od prędkości
            float speedFactor = Mathf.Clamp01((float)boatSpeed / 5f);
            pitch *= speedFactor;
            rollWave *= speedFactor;

            // Aplikacja rotacji
            boatModel.localRotation = Quaternion.Euler(pitch, 0f, heelAngle + rollWave);
        }

        private double DegToRad(double deg) => deg * Math.PI / 180.0;

        void Update()
        {
            // Aktualizacja w YachtState
        }

        // Funkcja pomocnicza do debugowania
        public double GetApparentWindAngle(double boatSpeed, double boatHeadingDeg)
        {
            if (!Initialized) return 0.0;
            
            double windDeg = Wind.WindDegree;
            double V_true = Wind.WindSpeed;
            double relativeWindDeg = (windDeg - boatHeadingDeg + 360.0) % 360.0;
            double relativeWindRad = DegToRad(relativeWindDeg);
            
            double vx = V_true * Math.Cos(relativeWindRad) - boatSpeed;
            double vy = V_true * Math.Sin(relativeWindRad);
            double betaAW = Math.Atan2(vy, -vx);
            
            return (betaAW * 180.0 / Math.PI + 360.0) % 360.0;
        }

        public double GetApparentWindSpeed(double boatSpeed, double boatHeadingDeg)
        {
            if (!Initialized) return 0.0;
            
            double windDeg = Wind.WindDegree;
            double V_true = Wind.WindSpeed;
            double relativeWindDeg = (windDeg - boatHeadingDeg + 360.0) % 360.0;
            double relativeWindRad = DegToRad(relativeWindDeg);
            
            double vx = V_true * Math.Cos(relativeWindRad) - boatSpeed;
            double vy = V_true * Math.Sin(relativeWindRad);
            
            return Math.Sqrt(vx * vx + vy * vy);
        }
    }
}