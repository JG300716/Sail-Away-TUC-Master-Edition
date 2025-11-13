using UnityEngine;
using System;

namespace Game.Scripts
{
    public class YachtPhysics : MonoBehaviour
    {
        [Header("Cloth Integration")]
        [SerializeField] private bool useClothPhysics = true;
        [SerializeField] private SailClothPhysics grotCloth;
        [SerializeField] private SailClothPhysics fokCloth;
        
        [Header("Physical Parameters")]
        [SerializeField] private double rhoWater = 1025.0; // Gęstość wody [kg/m³]
        [SerializeField] private double boatMass = 2700.0; // Masa jachtu [kg]
        [SerializeField] private double wettedArea = 10.0; // Powierzchnia zmoczona [m²]
        [SerializeField] private double dragCoeffHull = 0.009; // Współczynnik oporu kadłuba
        [SerializeField] private double maxSpeed = 15.0; // Maksymalna prędkość [m/s]
        
        [Header("Boat Model")]
        [SerializeField] private Transform boatModel;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        private bool initialized = false;
        private Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            
            if (rb == null)
            {
                Debug.LogError("[YachtPhysics] Brak Rigidbody!");
                enabled = false;
                return;
            }
            
            if (boatModel == null)
                boatModel = transform;
            
            initialized = true;
        }

        /// <summary>
        /// Główna funkcja obliczająca przyspieszenie jachtu
        /// TYLKO Z CLOTH SIMULATION
        /// </summary>
        public double ComputeAcceleration(double boatSpeed, double boatHeadingDeg, YachtSailState sailState)
        {
            if (!initialized)
                return 0.0;
            
            double totalDriveForce = 0.0;
            double totalLateralForce = 0.0;
            
            if (useClothPhysics)
            {
                // === NOWY SPOSÓB: Pobierz siły z Cloth żagli ===
                
                bool grotActive = sailState == YachtSailState.Grot_Only || 
                                 sailState == YachtSailState.Grot_and_Fok;
                                 
                bool fokActive = sailState == YachtSailState.Fok_Only || 
                                sailState == YachtSailState.Grot_and_Fok;
                
                if (grotActive && grotCloth != null && grotCloth.enabled)
                {
                    totalDriveForce += grotCloth.GetDriveForce();
                    totalLateralForce += grotCloth.GetLateralForce();
                }
                
                if (fokActive && fokCloth != null && fokCloth.enabled)
                {
                    totalDriveForce += fokCloth.GetDriveForce();
                    totalLateralForce += fokCloth.GetLateralForce();
                }
                
                if (enableDebugLogs && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[YachtPhysics] Cloth Forces:");
                    Debug.Log($"  Drive: {totalDriveForce:F2}N");
                    Debug.Log($"  Lateral: {totalLateralForce:F2}N");
                }
            }
            else
            {
                // Fallback - jeśli Cloth wyłączony, brak siły
                Debug.LogWarning("[YachtPhysics] Use Cloth Physics is disabled! No sail forces!");
                totalDriveForce = 0.0;
            }
            
            // === OPÓR KADŁUBA ===
            // R = 0.5 * ρ * A * Cd * v²
            double resistanceHull = 0.5 * rhoWater * wettedArea * dragCoeffHull * 
                                   boatSpeed * Math.Abs(boatSpeed);
            
            // Opór zawsze przeciwny do kierunku ruchu
            if (boatSpeed > 0)
                resistanceHull = -resistanceHull;
            else if (boatSpeed < 0)
                resistanceHull = Math.Abs(resistanceHull);
            
            // === PRZYSPIESZENIE ===
            // F = ma → a = F/m
            double netForce = totalDriveForce + resistanceHull;
            double acceleration = netForce / boatMass;
            
            // Ograniczenie maksymalnej prędkości
            if (boatSpeed > maxSpeed && acceleration > 0)
                acceleration = 0;
            
            if (boatSpeed < -maxSpeed * 0.3 && acceleration < 0)
                acceleration = 0;
            
            if (enableDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[YachtPhysics] Acceleration:");
                Debug.Log($"  Net Force: {netForce:F2}N");
                Debug.Log($"  Acceleration: {acceleration:F3} m/s²");
                Debug.Log($"  Speed: {boatSpeed:F2} m/s");
            }
            
            return acceleration;
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !initialized)
                return;
            
            // Rysuj wektor siły napędowej
            if (grotCloth != null && grotCloth.enabled)
            {
                Vector3 grotForce = grotCloth.GetTotalForce();
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, transform.position + grotForce * 0.01f);
            }
            
            if (fokCloth != null && fokCloth.enabled)
            {
                Vector3 fokForce = fokCloth.GetTotalForce();
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + fokForce * 0.01f);
            }
        }

        [ContextMenu("Log Physics State")]
        private void LogPhysicsState()
        {
            Debug.Log("=== YACHT PHYSICS STATE ===");
            Debug.Log($"Use Cloth Physics: {useClothPhysics}");
            Debug.Log($"Boat Mass: {boatMass} kg");
            Debug.Log($"Max Speed: {maxSpeed} m/s");
            
            if (rb != null)
            {
                Debug.Log($"Current Speed: {rb.linearVelocity.magnitude:F2} m/s");
            }
            
            if (grotCloth != null)
            {
                Debug.Log($"Grot Drive Force: {grotCloth.GetDriveForce():F2}N");
                Debug.Log($"Grot Lateral Force: {grotCloth.GetLateralForce():F2}N");
            }
            
            if (fokCloth != null)
            {
                Debug.Log($"Fok Drive Force: {fokCloth.GetDriveForce():F2}N");
                Debug.Log($"Fok Lateral Force: {fokCloth.GetLateralForce():F2}N");
            }
        }
    }
}