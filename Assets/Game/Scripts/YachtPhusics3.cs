using UnityEngine;

namespace Game.Scripts
{
    public class SailPhysicsKinematic : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private YachtState yachtState;
        
        [Header("Orientation")]
        [SerializeField] private float forwardRotationOffset = 90f;
        
        [Header("Sail Configuration")]
        [SerializeField] private SailConfig[] sails;
        
        [Header("Physics Parameters - KINEMATYCZNE")]
        [Range(0f, 10f)]
        [SerializeField] private float sailForceMultiplier = 2.0f; // Jak mocno żagle przyspieszają
        
        [Range(0f, 5f)]
        [SerializeField] private float dragCoefficient = 0.5f; // Opór
        
        [Range(0f, 20f)]
        [SerializeField] private float maxSpeed = 15f; // Max prędkość m/s
        
        [Header("Debug")]
        [SerializeField] private bool showDebugVectors = true;
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private float vectorScale = 5f;
        
        // State
        private Vector3 velocity = Vector3.zero;
        private Vector3 currentWindVelocity;
        
        private WindManager Wind => WindManager.Instance;

        [System.Serializable]
        public class SailConfig
        {
            public string sailName = "Sail";
            public SailType sailType = SailType.Mainsail;
            
            public Transform boomOrShotTransform;
            public Transform fokTackTransform;
            
            [Range(0f, 50f)]
            public float sailArea = 20f;
            
            public bool isActive = true;
            
            // Runtime
            [HideInInspector] public Vector3 sailNormal;
            [HideInInspector] public Vector3 apparentWind;
            [HideInInspector] public Vector3 acceleration;
            [HideInInspector] public float angleOfAttack;
        }
        
        public enum SailType { Mainsail, Jib }

        private Vector3 CorrectedForward => Quaternion.Euler(0, forwardRotationOffset, 0) * transform.forward;
        private Vector3 CorrectedRight => Quaternion.Euler(0, forwardRotationOffset, 0) * transform.right;

        void FixedUpdate()
        {
            if (Wind == null) return;
            
            float dt = Time.fixedDeltaTime;
            
            // 1. Oblicz wiatr
            currentWindVelocity = Wind.GetWindDirection3D() * (float)Wind.WindSpeed;
            
            // 2. Aktualizuj stany żagli
            UpdateSailStates();
            
            // 3. Oblicz przyspieszenie od żagli
            Vector3 sailAcceleration = Vector3.zero;
            
            foreach (var sail in sails)
            {
                if (!sail.isActive) continue;
                
                CalculateSailAcceleration(sail);
                sailAcceleration += sail.acceleration;
            }
            
            // 4. Zastosuj przyspieszenie
            velocity += sailAcceleration * dt;
            
            // 5. Opór
            float speed = velocity.magnitude;
            if (speed > 0.01f)
            {
                Vector3 dragForce = -velocity.normalized * dragCoefficient * speed;
                velocity += dragForce * dt;
            }
            
            // 6. Limituj prędkość
            if (velocity.magnitude > maxSpeed)
            {
                velocity = velocity.normalized * maxSpeed;
            }
            
            // 7. Przesuń jacht (tylko XZ!)
            Vector3 movement = new Vector3(velocity.x, 0, velocity.z) * dt;
            transform.position += movement;
            
            // 8. Aktualizuj YachtState
            UpdateYachtState();
        }

        void Update()
        {
            if (showDebugVectors) DrawDebugVectors();
        }

        private void UpdateSailStates()
        {
            if (yachtState == null) return;
            
            foreach (var sail in sails)
            {
                sail.isActive = sail.sailType == SailType.Mainsail 
                    ? (yachtState.SailState == YachtSailState.Grot_Only || yachtState.SailState == YachtSailState.Grot_and_Fok)
                    : (yachtState.SailState == YachtSailState.Fok_Only || yachtState.SailState == YachtSailState.Grot_and_Fok);
            }
        }

        private void CalculateSailAcceleration(SailConfig sail)
        {
            if (sail.boomOrShotTransform == null)
            {
                sail.acceleration = Vector3.zero;
                return;
            }
            
            // 1. Normalna żagla
            if (sail.sailType == SailType.Mainsail)
            {
                CalculateMainsailNormal(sail);
            }
            else
            {
                CalculateJibNormal(sail);
            }
            
            // 2. Wiatr pozorny
            sail.apparentWind = currentWindVelocity - new Vector3(velocity.x, 0, velocity.z);
            
            // 3. Kąt natarcia
            Vector3 windXZ = new Vector3(sail.apparentWind.x, 0, sail.apparentWind.z);
            Vector3 normalXZ = new Vector3(sail.sailNormal.x, 0, sail.sailNormal.z);
            
            if (windXZ.magnitude < 0.1f || normalXZ.magnitude < 0.01f)
            {
                sail.angleOfAttack = 0f;
                sail.acceleration = Vector3.zero;
                return;
            }
            
            sail.angleOfAttack = Vector3.SignedAngle(normalXZ, windXZ, Vector3.up);
            
            // 4. Efektywność żagla
            float absAngle = Mathf.Abs(sail.angleOfAttack);
            float effectiveness = absAngle < 15f ? 0.2f :
                                 absAngle < 45f ? Mathf.Lerp(0.2f, 1.0f, (absAngle - 15f) / 30f) :
                                 absAngle < 90f ? 1.0f :
                                 Mathf.Lerp(1.0f, 0.3f, (absAngle - 90f) / 90f);
            
            // 5. Przyspieszenie (PROSTE!)
            float windSpeed = sail.apparentWind.magnitude;
            Vector3 windDir = sail.apparentWind.normalized;
            
            // Siła prostopadła do wiatru (lift)
            Vector3 liftDir = Vector3.Cross(Vector3.up, windDir).normalized;
            
            // Przyspieszenie = kierunek * prędkość_wiatru² * efektywność * powierzchnia * multiplier
            sail.acceleration = liftDir * windSpeed * windSpeed * effectiveness * sail.sailArea * sailForceMultiplier * 0.001f;
            sail.acceleration.y = 0; // Tylko XZ
        }

        private void CalculateMainsailNormal(SailConfig sail)
        {
            Transform boom = sail.boomOrShotTransform;
            Vector3 boomDir = new Vector3(boom.right.x, 0, boom.right.z).normalized;
            
            sail.sailNormal = new Vector3(-boomDir.z, 0, boomDir.x);
            
            if (Vector3.Dot(sail.sailNormal, CorrectedForward) < 0)
                sail.sailNormal = -sail.sailNormal;
            
            if (float.IsNaN(sail.sailNormal.x))
                sail.sailNormal = CorrectedForward;
        }

        private void CalculateJibNormal(SailConfig sail)
        {
            Transform shot = sail.boomOrShotTransform;
            Transform tack = sail.fokTackTransform;
            
            if (shot == null || tack == null)
            {
                sail.sailNormal = CorrectedForward;
                return;
            }
            
            Vector3 luffEdge = (shot.position - tack.position);
            luffEdge = new Vector3(luffEdge.x, 0, luffEdge.z).normalized;
            
            Vector3 normalOption1 = new Vector3(luffEdge.z, 0, -luffEdge.x);
            Vector3 normalOption2 = new Vector3(-luffEdge.z, 0, luffEdge.x);
            
            sail.sailNormal = Vector3.Dot(normalOption1, CorrectedForward) > Vector3.Dot(normalOption2, CorrectedForward)
                ? normalOption1 : normalOption2;
            
            if (float.IsNaN(sail.sailNormal.x) || sail.sailNormal.magnitude < 0.1f)
                sail.sailNormal = CorrectedForward;
        }

        private void UpdateYachtState()
        {
            if (yachtState == null) return;
            
            float speed = velocity.magnitude;
            yachtState.V_current = speed;
            
            // Przyspieszenie = suma przyspieszenia od żagli
            float totalAccel = 0f;
            foreach (var sail in sails)
            {
                if (sail.isActive)
                    totalAccel += sail.acceleration.magnitude;
            }
            yachtState.Acceleration = totalAccel;
        }

        private void DrawDebugVectors()
        {
            if (!Application.isPlaying) return;
            
            Vector3 basePos = transform.position;
            
            // Wiatr
            Debug.DrawRay(basePos + Vector3.up * 8f, currentWindVelocity, Color.cyan);
            
            // Forward poprawiony
            Debug.DrawRay(basePos, CorrectedForward * 6f, Color.white);
            
            // Prędkość
            Debug.DrawRay(basePos + Vector3.up * 3f, velocity * 2f, Color.magenta);
            
            foreach (var sail in sails)
            {
                if (!sail.isActive || sail.boomOrShotTransform == null) continue;
                
                Vector3 origin = basePos + Vector3.up * 2f;
                
                // Normalna żagla (żółty)
                Debug.DrawRay(origin, sail.sailNormal * vectorScale, Color.yellow);
                
                // Apparent wind (niebieski)
                Debug.DrawRay(origin, sail.apparentWind.normalized * vectorScale, new Color(0, 0.7f, 1f));
                
                // Przyspieszenie (ZIELONY - powinno być widoczne!)
                if (sail.acceleration.magnitude > 0.01f)
                {
                    Debug.DrawRay(origin, sail.acceleration * vectorScale * 100f, Color.green);
                }
            }
        }

        void OnGUI()
        {
            if (!showDebugUI || !Application.isPlaying) return;
            
            int y = 140;
            GUI.Box(new Rect(10, y, 500, 280), "Sail Physics - KINEMATIC");
            y += 25;
            
            GUI.Label(new Rect(20, y, 480, 20), $"Velocity: {velocity.magnitude:F2} m/s ({velocity.magnitude * 1.94f:F1} kn)");
            y += 20;
            
            GUI.Label(new Rect(20, y, 480, 20), $"Velocity Vector: {velocity:F2}");
            y += 20;
            
            GUI.Label(new Rect(20, y, 480, 20), $"Wind: {Wind.WindSpeed:F1} m/s @ {Wind.WindDegree:F0}°");
            y += 20;
            
            GUI.Label(new Rect(20, y, 480, 20), $"Forward: {CorrectedForward:F2}");
            y += 25;
            
            foreach (var sail in sails)
            {
                if (!sail.isActive) continue;
                
                GUI.Label(new Rect(20, y, 480, 20), $"{sail.sailName}:");
                y += 20;
                
                GUI.Label(new Rect(30, y, 470, 20), $"AoA: {sail.angleOfAttack:F0}°");
                y += 20;
                
                GUI.Label(new Rect(30, y, 470, 20), $"Normal: {sail.sailNormal:F2}");
                y += 20;
                
                float accel = sail.acceleration.magnitude;
                GUI.color = accel > 0.1f ? Color.green : (accel > 0.01f ? Color.yellow : Color.red);
                GUI.Label(new Rect(30, y, 470, 20), $"Acceleration: {accel:F3} m/s²");
                GUI.color = Color.white;
                y += 25;
            }
        }
    }
}