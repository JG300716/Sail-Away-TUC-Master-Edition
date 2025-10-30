using UnityEngine;
using System;
using UnityEngine.Serialization;

namespace Game.Assets
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
        
        [SerializeField] public WindManager windManager;
        
        private bool Initialized = false;
        void Start()
        {
            Initialized = windManager != null; 
        }

        public double ComputeAcceleration(double boatSpeed, double boatHeadingDeg, YachtSailState yachtState)
        {
            if (!Initialized) return 0.0;
            
            // Wiatr pozorny (Apparent Wind)
            double betaTrue = DegToRad(windManager.WindDegree);
            double V_true = windManager.WindSpeed;
            
            // Kąt między wiatrem a łódką (w stopniach)
            double relativeWindDeg = Math.Clamp(betaTrue - boatHeadingDeg, 0, 360);
            
            // Konwersja na radiany
            double relativeWindRad = Math.PI * relativeWindDeg / 180.0;
            
            double vx = V_true * Math.Cos(relativeWindRad) - boatSpeed; // komponent X (kierunek jachtu)
            double vy = V_true * Math.Sin(relativeWindRad); // komponent boczny
            double Va = Math.Sqrt(vx * vx + vy * vy); // prędkość pozornego wiatru
            double betaAW = Math.Atan2(vy, -vx); // kąt pozornego wiatru względem osi jachtu

            // Siła aerodynamiczna dla grota i foka
            bool grotSet = (yachtState & YachtSailState.Grot_Only) != 0 || (yachtState & YachtSailState.Grot_and_Fok) != 0;
            bool fokSet  = (yachtState & YachtSailState.Fok_Only)  != 0 || (yachtState & YachtSailState.Grot_and_Fok) != 0;

            double FdriveGrot = grotSet
                    ? ComputeSailForce(SailAreaGrot, SheetAngleGrot, Va, betaAW)
                    : 0.0;
            double FdriveFok = fokSet
                    ? ComputeSailForce(SailAreaFok, SheetAngleFok, Va, betaAW)
                    : 0.0;
            double Fdrive = FdriveGrot + FdriveFok;

            // Opór hydrodynamiczny kadłuba
            double R_hull = 0.5 * RhoWater * WettedArea * DragCoeffHull * boatSpeed * boatSpeed;

            // Przyspieszenie netto
            double acceleration = (Fdrive - R_hull) / BoatMass;

            return acceleration;
        }

        private double ComputeSailForce(double area, double sheetAngleDeg, double Va, double betaAW)
        {
            double sheetRad = DegToRad(sheetAngleDeg);
            double alpha = sheetRad - betaAW; // kąt natarcia żagla

            // Aerodynamiczne współczynniki żagla
            double Cl = ClAlpha * alpha;
            if (Cl > ClMax) Cl = ClMax;
            if (Cl < -ClMax) Cl = -ClMax;
            double Cd = Cd0 + K * Cl * Cl;

            // Siły
            double q = 0.5 * RhoAir * Va * Va; // ciśnienie dynamiczne
            double L = q * area * Cl; // siła nośna
            double D = q * area * Cd; // siła oporu

            // Siła netto wzdłuż osi jachtu (napęd)
            double Fdrive = L * Math.Cos(betaAW) - D * Math.Sin(betaAW);
            return Fdrive;
        }

        private double DegToRad(double deg) => deg * Math.PI / 180.0;

        // Update is called once per frame
        void Update()
        {
        }
    }
}