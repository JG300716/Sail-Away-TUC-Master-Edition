using UnityEngine;

namespace Game.Scripts
{
    [RequireComponent(typeof(Rigidbody))]
    public class SailPhysics : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private YachtState yachtState;
        [SerializeField] private Rigidbody yachtRigidbody;
        
        [Header("Orientation Fix")]
        [Tooltip("Jeśli forward jachtu jest obrócony, ustaw poprawkę (0, 90, 180, 270)")]
        [SerializeField] private float forwardRotationOffset = 90f; // 90° w prawo = naprawia forward w lewo
        
        [Header("Mass Scaling")]
        [SerializeField] private float massScale = 3000f;
        
        [Header("Sail Configuration")]
        [SerializeField] private SailConfig[] sails;
        
        [Header("Physics Parameters - MAKSYMALNE TESTOWE")]
        [Range(0f, 5f)]
        [SerializeField] private float sailEfficiency = 5.0f;
        
        [Range(0f, 1f)]
        [SerializeField] private float dragCoefficient = 0.1f;
        
        [Range(0f, 5f)]
        [SerializeField] private float hullDragCoefficient = 0.1f;
        
        [Range(0f, 10f)]
        [SerializeField] private float lateralDragCoefficient = 0.3f;
        
        [Header("Force Application")]
        [Range(0f, 2000f)]
        [SerializeField] private float forceMultiplier = 1000.0f;
        
        [Range(0f, 2000)]
        [SerializeField] private float maxForcePerSail = 1000f;
        
        [Header("Safety")]
        [SerializeField] private float maxVelocity = 30f;
        [SerializeField] private bool autoResetOnExplosion = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugVectors = true;
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private bool showDetailedDebug = true;
        [SerializeField] private float vectorScale = 5f;
        
        private Vector3 currentWindVelocity;
        private Vector3 totalAppliedForce;
        private bool explosionDetected = false;
        
        private WindManager Wind => WindManager.Instance;

        [System.Serializable]
        public class SailConfig
        {
            public string sailName = "Sail";
            public SailType sailType = SailType.Mainsail;
            
            [Header("Boom Control - RÓŻNE dla Grot i Fok")]
            [Tooltip("GROT: Transform z jointem który się obraca\nFOK: Transform shota (górny róg)")]
            public Transform boomOrShotTransform;
            
            [Tooltip("TYLKO FOK: Transform tack (dolny przedni róg foka)")]
            public Transform fokTackTransform;
            
            [Tooltip("Tylko wizualizacja")]
            public Cloth sailCloth;
            
            [Range(0f, 50f)]
            public float sailArea = 20f;
            
            public bool isActive = true;
            
            // Runtime
            [HideInInspector] public Vector3 sailNormal;
            [HideInInspector] public Vector3 apparentWind;
            [HideInInspector] public Vector3 totalForce;
            [HideInInspector] public float angleOfAttack;
            [HideInInspector] public float effectiveSailArea;
            [HideInInspector] public float smoothedAngleOfAttack;
            [HideInInspector] public Vector3 smoothedSailNormal;
            [HideInInspector] public Vector3 previousTotalForce; // NOWE
        }
        
        public enum SailType { Mainsail, Jib }

        // Cached corrected forward
        private Vector3 CorrectedForward
        {
            get
            {
                Quaternion correction = Quaternion.Euler(0, forwardRotationOffset, 0);
                return correction * transform.forward;
            }
        }
        
        private Vector3 CorrectedRight
        {
            get
            {
                Quaternion correction = Quaternion.Euler(0, forwardRotationOffset, 0);
                return correction * transform.right;
            }
        }

        void Start()
        {
            if (yachtRigidbody == null)
                yachtRigidbody = GetComponent<Rigidbody>();
            
            yachtRigidbody.linearDamping = 0.1f;
            yachtRigidbody.angularDamping = 1.0f;
            yachtRigidbody.maxAngularVelocity = 3f;
            
            ValidateConfiguration();
            
            Debug.Log($"[SailPhysics] Forward Correction: {forwardRotationOffset}°");
            Debug.Log($"[SailPhysics] Original Forward: {transform.forward}");
            Debug.Log($"[SailPhysics] Corrected Forward: {CorrectedForward}");
        }

        void FixedUpdate()
        {
            if (Wind == null) return;
            
            if (DetectExplosion())
            {
                if (autoResetOnExplosion) ResetPhysics();
                return;
            }
            
            ClampVelocities();
            CalculateWindConditions();
            UpdateSailStates();
            
            foreach (var sail in sails)
            {
                if (!sail.isActive) continue;
                CalculateSailParameters(sail);
                
                // if (showDetailedDebug)
                //     DebugSailCalculations(sail);
            }
            
            ApplySailForces();
            ApplyDrag();
        }

        void Update()
        {
            //if (showDebugVectors) DrawDebugVectors();
        }

        #region VALIDATION & SAFETY

        private void ValidateConfiguration()
        {
            foreach (var sail in sails)
            {
                if (sail.boomOrShotTransform == null)
                {
                    Debug.LogError($"[SailPhysics] {sail.sailName}: BRAK boomOrShotTransform!");
                }
                else
                {
                    string config = sail.sailType == SailType.Mainsail 
                        ? $"Boom (joint)" 
                        : $"Shot + Tack ({(sail.fokTackTransform != null ? "OK" : "BRAK TACK!")})";
                    
                    Debug.Log($"[SailPhysics] {sail.sailName}: {sail.boomOrShotTransform.name} ({config})");
                }
            }
        }

        private bool DetectExplosion()
        {
            float vel = yachtRigidbody.linearVelocity.magnitude;
            if (vel > maxVelocity || float.IsNaN(vel))
            {
                if (!explosionDetected)
                {
                    Debug.LogError($"[SailPhysics] EKSPLOZJA! Vel={vel}");
                    explosionDetected = true;
                }
                return true;
            }
            explosionDetected = false;
            return false;
        }

        private void ResetPhysics()
        {
            yachtRigidbody.linearVelocity = Vector3.zero;
            yachtRigidbody.angularVelocity = Vector3.zero;
            transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }

        private void ClampVelocities()
        {
            if (yachtRigidbody.linearVelocity.magnitude > maxVelocity)
                yachtRigidbody.linearVelocity = yachtRigidbody.linearVelocity.normalized * maxVelocity;
        }

        #endregion

        #region PHYSICS CALCULATIONS

        private void CalculateWindConditions()
        {
            currentWindVelocity = Wind.GetWindDirection3D() * (float)Wind.WindSpeed;
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

       private void CalculateSailParameters(SailConfig sail)
        {
            if (sail.boomOrShotTransform == null)
            {
                sail.totalForce = Vector3.zero;
                return;
            }
            
            // 1. Oblicz normalną żagla
            Vector3 rawNormal;
            if (sail.sailType == SailType.Mainsail)
            {
                rawNormal = CalculateMainsailNormalRaw(sail);
            }
            else
            {
                rawNormal = CalculateJibNormalRaw(sail);
            }
            
            // Wygładzanie normalnej
            if (sail.smoothedSailNormal == Vector3.zero)
            {
                sail.smoothedSailNormal = rawNormal;
            }
            else
            {
                sail.smoothedSailNormal = Vector3.Lerp(sail.smoothedSailNormal, rawNormal, 0.1f);
                sail.smoothedSailNormal.Normalize();
            }
            
            sail.sailNormal = sail.smoothedSailNormal;
            
            // 2. APPARENT WIND (jak na diagramie)
            // Apparent Wind = True Wind - Boat Velocity
            Vector3 boatVel = new Vector3(yachtRigidbody.linearVelocity.x, 0, yachtRigidbody.linearVelocity.z);
            sail.apparentWind = currentWindVelocity - boatVel;
            
            // 3. ANGLE OF ATTACK (kąt między sail normal a apparent wind)
            Vector3 windXZ = new Vector3(sail.apparentWind.x, 0, sail.apparentWind.z);
            Vector3 normalXZ = new Vector3(sail.sailNormal.x, 0, sail.sailNormal.z);
            
            if (windXZ.magnitude < 0.1f || normalXZ.magnitude < 0.01f)
            {
                sail.angleOfAttack = 0f;
                sail.totalForce = Vector3.zero;
                return;
            }
            
            // Kąt między normalną a wiatrem (0-180°)
            float angleToWind = Vector3.Angle(normalXZ, windXZ);
            
            // ZGODNIE Z DIAGRAMEM:
            // - 0° = wiatr "w twarz" żagla (flaga)
            // - 90° = wiatr prostopadły (max siła)
            // - 180° = wiatr od tyłu (pusty żagiel)
            
            // AoA powinno być kątem od optymalnego (90°)
            float rawAngle = angleToWind - 90f; // -90 do +90
            
            // Określ znak (która strona żagla)
            Vector3 cross = Vector3.Cross(normalXZ, windXZ);
            if (cross.y < 0)
            {
                rawAngle = -rawAngle;
            }
            
            if (sail.smoothedAngleOfAttack == 0f)
            {
                sail.smoothedAngleOfAttack = rawAngle;
            }
            else
            {
                float angleDiff = Mathf.DeltaAngle(sail.smoothedAngleOfAttack, rawAngle);
                sail.smoothedAngleOfAttack += angleDiff * 0.2f;
            }
            
            sail.angleOfAttack = sail.smoothedAngleOfAttack;
            
            float absAngle = Mathf.Abs(angleToWind); // Użyj angleToWind, nie AoA!
            
            // Efficiency curve:
            // 0°–15° -> rośnie od 0 do 1
            // 15°–30° -> max 1
            // 30°–60° -> spada do 0.5
            // >60° -> 0.3 (resztkowa siła)
            float effectiveness;
            if (absAngle < 30f) // Flaga
            {
                effectiveness = 0.1f;
            }
            else if (absAngle < 60f) // Wzrost
            {
                effectiveness = Mathf.Lerp(0.1f, 0.8f, (absAngle - 30f) / 30f);
            }
            else if (absAngle < 120f) // Optimum
            {
                effectiveness = 1.0f;
            }
            else if (absAngle < 150f) // Spadek
            {
                effectiveness = Mathf.Lerp(1.0f, 0.3f, (absAngle - 120f) / 30f);
            }
            else // Fordewind
            {
                effectiveness = 0.3f;
            }
            
            sail.effectiveSailArea = sail.sailArea * effectiveness;
            
            // 5. Forces
            CalculateSailForces(sail);
        }

        private Vector3 CalculateMainsailNormalRaw(SailConfig sail)
        {
            Transform boom = sail.boomOrShotTransform;
            Vector3 boomDir = new Vector3(boom.right.x, 0, boom.right.z).normalized;
    
            Vector3 normal = new Vector3(-boomDir.x, 0, -boomDir.z);
            
            if (float.IsNaN(normal.x))
                normal = CorrectedForward;
    
            return normal;
        }

        private Vector3 CalculateJibNormalRaw(SailConfig sail)
        {
            Transform shot = sail.boomOrShotTransform;
            Transform tack = sail.fokTackTransform;
    
            if (shot == null || tack == null)
                return CorrectedForward;
    
            Vector3 luffEdge = (shot.position - tack.position);
            luffEdge = new Vector3(luffEdge.x, 0, luffEdge.z).normalized;
    
            // ODWRÓCONE znaki!
            Vector3 normal = new Vector3(luffEdge.x, 0, luffEdge.z);
    
            if (float.IsNaN(normal.x) || normal.magnitude < 0.1f)
                normal = CorrectedForward;
    
            return normal;
        }

        private void CalculateSailForces(SailConfig sail)
        {
            float windSpeed = sail.apparentWind.magnitude;
            if (windSpeed < 0.1f)
            {
                sail.totalForce = Vector3.zero;
                return;
            }
            
            Vector3 windDir = sail.apparentWind.normalized;
            
            Vector3 forceDir = Vector3.Cross(windDir, Vector3.up).normalized;
            Vector3 forceDirOption1 = forceDir;
            Vector3 forceDirOption2 = -forceDir;
            
            float dot1 = Vector3.Dot(forceDirOption1, CorrectedForward);
            float dot2 = Vector3.Dot(forceDirOption2, CorrectedForward);
            
            // Wybierz tę która bardziej do przodu
            if (dot2 > dot1)
            {
                forceDir = forceDirOption2;
            }
            
            // 3. Magnitude siły
            float absAoA = Mathf.Abs(sail.angleOfAttack);
            float efficiency = absAoA < 15f ? Mathf.Lerp(0f, 1.0f, absAoA / 15f) :
                              absAoA < 30f ? 1.0f :
                              absAoA < 60f ? Mathf.Lerp(1.0f, 0.5f, (absAoA - 30f) / 30f) : 0.3f;
            
            float dynamicPressure = 0.5f * 1.225f * windSpeed * windSpeed / massScale;
            float forceMagnitude = dynamicPressure * sail.effectiveSailArea * efficiency * sailEfficiency;
            
            // 4. Total force
            sail.totalForce = forceDir * forceMagnitude;
            
            // Debug
            Debug.Log($"  Wind Dir: {windDir:F2}");
            Debug.Log($"  Force Dir Option 1: {forceDirOption1:F2} (dot: {dot1:F2})");
            Debug.Log($"  Force Dir Option 2: {forceDirOption2:F2} (dot: {dot2:F2})");
            Debug.Log($"  CHOSEN Force Dir: {forceDir:F2}");
            Debug.Log($"  Force Magnitude: {forceMagnitude:F4}");
            Debug.Log($"  Total: {sail.totalForce:F4}");
            
            if (sail.totalForce.magnitude > maxForcePerSail)
                sail.totalForce = sail.totalForce.normalized * maxForcePerSail;
        }

        private void ApplySailForces()
        {
            totalAppliedForce = Vector3.zero;
            
            foreach (var sail in sails)
            {
                if (!sail.isActive || sail.boomOrShotTransform == null) continue;
                
                Vector3 force = sail.totalForce * forceMultiplier;
                yachtRigidbody.AddForce(force, ForceMode.Force);
                totalAppliedForce += force;
            }
            
            // Update state
            if (yachtState != null)
            {
                yachtState.V_current = yachtRigidbody.linearVelocity.magnitude;
                yachtState.Acceleration = totalAppliedForce.magnitude / yachtRigidbody.mass;
            }
        }

        private void ApplyDrag()
        {
            Vector3 vel = yachtRigidbody.linearVelocity;
            float speed = vel.magnitude;
            
            if (speed < 0.01f) return;
            
            // Hull drag
            yachtRigidbody.AddForce(-vel.normalized * hullDragCoefficient * speed * speed, ForceMode.Force);
            
            // Lateral drag (kil) - używamy POPRAWIONEGO right
            Vector3 lateral = Vector3.Project(vel, CorrectedRight);
            yachtRigidbody.AddForce(-lateral.normalized * lateralDragCoefficient * lateral.magnitude * lateral.magnitude, ForceMode.Force);
        }

        #endregion

        // #region DEBUG
        //
        // private void DebugSailCalculations(SailConfig sail)
        // {
        //     Debug.Log($"===== {sail.sailName} ({sail.sailType}) =====");
        //     Debug.Log($"  Yacht Forward (original): {transform.forward:F2}");
        //     Debug.Log($"  Yacht Forward (corrected): {CorrectedForward:F2}");
        //     
        //     if (sail.sailType == SailType.Mainsail)
        //     {
        //         Debug.Log($"  Boom Euler Y: {sail.boomOrShotTransform.eulerAngles.y:F1}°");
        //         Debug.Log($"  Boom Right: {sail.boomOrShotTransform.right:F2}");
        //     }
        //     else // Jib
        //     {
        //         if (sail.fokTackTransform != null)
        //         {
        //             Vector3 luffDir = (sail.boomOrShotTransform.position - sail.fokTackTransform.position).normalized;
        //             Debug.Log($"  Shot Pos: {sail.boomOrShotTransform.position:F1}");
        //             Debug.Log($"  Tack Pos: {sail.fokTackTransform.position:F1}");
        //             Debug.Log($"  Luff Direction: {luffDir:F2}");
        //         }
        //     }
        //     
        //     Debug.Log($"  Sail Normal: {sail.sailNormal:F2}");
        //     Debug.Log($"  Wind: {currentWindVelocity:F2} (mag: {currentWindVelocity.magnitude:F1})");
        //     Debug.Log($"  Apparent Wind: {sail.apparentWind:F2} (mag: {sail.apparentWind.magnitude:F1})");
        //     Debug.Log($"  AoA: {sail.angleOfAttack:F1}°");
        //     Debug.Log($"  Effective Area: {sail.effectiveSailArea:F1} m²");
        //     Debug.Log($"  Total Force: {sail.totalForce:F4} (mag: {sail.totalForce.magnitude:F4})");
        //     Debug.Log($"  After x{forceMultiplier}: {(sail.totalForce * forceMultiplier).magnitude:F2}");
        //     Debug.Log($"========================");
        // }
        //
        // private void DrawDebugVectors()
        // {
        //     if (!Application.isPlaying) return;
        //     
        //     Vector3 basePos = transform.position;
        //     
        //     // Wiatr (cyan, wysoko)
        //     Debug.DrawRay(basePos + Vector3.up * 8f, currentWindVelocity, Color.cyan);
        //     
        //     // Dzioб jachtu - ORYGINAŁ (czerwony)
        //     Debug.DrawRay(basePos, transform.forward * 5f, Color.red);
        //     
        //     // Dzioб jachtu - POPRAWIONY (biały, grubszy)
        //     Debug.DrawRay(basePos, CorrectedForward * 6f, Color.white);
        //     Debug.DrawRay(basePos + Vector3.up * 0.1f, CorrectedForward * 6f, Color.white);
        //     
        //     // Right - POPRAWIONY (szary)
        //     Debug.DrawRay(basePos, CorrectedRight * 4f, Color.gray);
        //     
        //     // Prędkość (magenta)
        //     Debug.DrawRay(basePos + Vector3.up * 3f, yachtRigidbody.linearVelocity * 2f, Color.magenta);
        //     
        //     foreach (var sail in sails)
        //     {
        //         if (!sail.isActive || sail.boomOrShotTransform == null) continue;
        //         
        //         Vector3 origin = basePos + Vector3.up * 2f;
        //         
        //         if (sail.sailType == SailType.Mainsail)
        //         {
        //             // Boom osie
        //             Transform boom = sail.boomOrShotTransform;
        //             Debug.DrawRay(boom.position, boom.right * 3f, new Color(1f, 0.5f, 0.5f)); // Jasnoczerowny
        //             Debug.DrawRay(boom.position, boom.up * 2f, new Color(0.5f, 1f, 0.5f)); // Jasnozielony
        //         }
        //         else // Jib
        //         {
        //             // Luff edge
        //             if (sail.fokTackTransform != null)
        //             {
        //                 Debug.DrawLine(sail.fokTackTransform.position, sail.boomOrShotTransform.position, new Color(0.5f, 1f, 0.5f));
        //             }
        //         }
        //         
        //         // Sail normal (ŻÓŁTY - najważniejszy!)
        //         Debug.DrawRay(origin, sail.sailNormal * vectorScale, Color.yellow);
        //         
        //         // Apparent wind (jasnoniebieski)
        //         Debug.DrawRay(origin, sail.apparentWind.normalized * vectorScale, new Color(0, 0.7f, 1f));
        //         
        //         // Total force (ZIELONY - GRUBA strzałka!)
        //         if (sail.totalForce.magnitude > 0.001f)
        //         {
        //             Vector3 forceVec = sail.totalForce * vectorScale * 50f;
        //             Debug.DrawRay(origin, forceVec, Color.green);
        //             Debug.DrawRay(origin + Vector3.right * 0.1f, forceVec, Color.green);
        //             Debug.DrawRay(origin + Vector3.forward * 0.1f, forceVec, Color.green);
        //         }
        //     }
        // }
        //
        // void OnGUI()
        // {
        //     if (!showDebugUI || !Application.isPlaying) return;
        //     
        //     int y = 140;
        //     GUI.Box(new Rect(10, y, 550, 360), "Sail Physics - Forward Corrected");
        //     y += 25;
        //     
        //     if (explosionDetected)
        //     {
        //         GUI.color = Color.red;
        //         GUI.Label(new Rect(20, y, 530, 20), "!!! EKSPLOZJA !!!");
        //         GUI.color = Color.white;
        //         y += 25;
        //     }
        //     
        //     GUI.Label(new Rect(20, y, 530, 20), $"Forward Offset: {forwardRotationOffset}°");
        //     y += 20;
        //     
        //     GUI.Label(new Rect(20, y, 530, 20), $"Original Forward: {transform.forward:F2}");
        //     y += 20;
        //     
        //     GUI.color = Color.green;
        //     GUI.Label(new Rect(20, y, 530, 20), $"Corrected Forward: {CorrectedForward:F2}");
        //     GUI.color = Color.white;
        //     y += 25;
        //     
        //     GUI.Label(new Rect(20, y, 530, 20), $"Vel: {yachtRigidbody.linearVelocity.magnitude:F2} m/s ({yachtRigidbody.linearVelocity.magnitude * 1.94f:F1} kn)");
        //     y += 20;
        //     
        //     float realForce = totalAppliedForce.magnitude * massScale;
        //     GUI.Label(new Rect(20, y, 530, 20), $"Force: {totalAppliedForce.magnitude:F2} U = {realForce:F0} N");
        //     y += 20;
        //     
        //     GUI.Label(new Rect(20, y, 530, 20), $"Wind: {Wind.WindSpeed:F1} m/s @ {Wind.WindDegree:F0}°");
        //     y += 25;
        //     
        //     foreach (var sail in sails)
        //     {
        //         if (!sail.isActive) continue;
        //         
        //         GUI.Label(new Rect(20, y, 530, 20), $"{sail.sailName} ({sail.sailType}):");
        //         y += 20;
        //         
        //         if (sail.boomOrShotTransform != null)
        //         {
        //             if (sail.sailType == SailType.Mainsail)
        //             {
        //                 GUI.Label(new Rect(30, y, 520, 20), $"Boom Y: {sail.boomOrShotTransform.eulerAngles.y:F1}°");
        //             }
        //             else
        //             {
        //                 GUI.Label(new Rect(30, y, 520, 20), $"Shot: {sail.boomOrShotTransform.position:F1}");
        //             }
        //             y += 20;
        //         }
        //         
        //         GUI.Label(new Rect(30, y, 520, 20), $"Normal: {sail.sailNormal:F2}");
        //         y += 20;
        //         
        //         GUI.Label(new Rect(30, y, 520, 20), $"AoA: {sail.angleOfAttack:F0}° | Area: {sail.effectiveSailArea:F1}m²");
        //         y += 20;
        //         
        //         float forceWithMult = sail.totalForce.magnitude * forceMultiplier;
        //         GUI.color = forceWithMult > 1f ? Color.green : (forceWithMult > 0.1f ? Color.yellow : Color.red);
        //         GUI.Label(new Rect(30, y, 520, 20), $"Force: {sail.totalForce.magnitude:F3} → x{forceMultiplier} = {forceWithMult:F2}");
        //         GUI.color = Color.white;
        //         y += 25;
        //     }
        // }
        //
        // #endregion
    }
}