using Game.Scripts.Controllers;
using UnityEngine;

public class FinalPhysics : MonoBehaviour
{
    [Header("References")]
    public YAchtUnifiedController2 yacht;
    public UnifiedSail mainSail;
    public UnifiedWindManager windSystem;

    [Header("Water Resistance")]
    public float waterDragCoefficient = 0.3f;
    public float lateralDragMultiplier = 5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showForceGizmos = true;
    
    private Vector3 lastSailForce;
    private float lastTorque;

    void FixedUpdate()
    {
        if (yacht == null || windSystem == null) return;
        
        // 1. Symulacja siły wiatru na żaglach
        ApplyWindForces();
        
        // 2. Opór wody
        ApplyWaterResistance();
    }

    void ApplyWindForces()
    {
        Vector2 windDir = windSystem.GetWindDirection();
        float windSpeed = windSystem.GetWindSpeed();

        // Safety check: wiatr
        if (windSpeed < 0.01f)
        {
            if (enableDebugLogs)
                Debug.Log("[FinalPhysics] Wind speed too low, skipping");
            return;
        }

        // KRYTYCZNE: Sprawdź czy żagiel jest aktywny i włączony!
        if (mainSail == null || !mainSail.gameObject.activeInHierarchy || !mainSail.enabled)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
                Debug.Log("[FinalPhysics] MainSail not active or disabled");
            return;
        }
        
        // Dodatkowy check przez CanControl()
        if (!mainSail.CanControl())
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
                Debug.Log("[FinalPhysics] MainSail CanControl() returned false");
            return;
        }

        // Konwersja wiatru do 3D
        Vector3 windDir3D = new Vector3(windDir.x, 0, windDir.y);
        
        // Safety check: normalizacja
        if (windDir3D.sqrMagnitude < 0.001f)
        {
            Debug.LogWarning("[FinalPhysics] Wind direction is zero!");
            return;
        }

        // Pobierz siłę z pełnymi informacjami
        UnifiedSail.SailForceResult sailResult = mainSail.CalculateWindForceWithTorque(windDir3D, windSpeed);

        // Safety check: NaN/Infinity w sile
        if (!IsValidVector(sailResult.force))
        {
            Debug.LogError($"[FinalPhysics] Invalid sail force detected! " +
                          $"force={sailResult.force}, " +
                          $"windDir={windDir3D}, windSpeed={windSpeed}, " +
                          $"sailArea={mainSail.GetSailArea()}, " +
                          $"forceMultiplier={mainSail.forceMultiplier}");
            return;
        }

        // Aplikacja siły
        Vector2 sailForce2D = new Vector2(sailResult.force.x, sailResult.force.z);
        
        // Safety check przed aplikacją
        if (!IsValidVector2(sailForce2D))
        {
            Debug.LogError($"[FinalPhysics] Invalid sailForce2D: {sailForce2D}");
            return;
        }
        
        yacht.ApplyForce(sailForce2D);
        lastSailForce = sailResult.force;

        if (enableDebugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[FinalPhysics] Sail Force: {sailForce2D}, " +
                      $"magnitude: {sailForce2D.magnitude:F1}N, " +
                      $"angleOfAttack: {sailResult.angleOfAttack:F1}°, " +
                      $"deadZone: {sailResult.inDeadZone}");
        }

        // Moment obrotowy
        if (sailResult.torqueMultiplier > 0.01f)
        {
            Vector2 forcePosition2D = new Vector2(mainSail.transform.position.x, mainSail.transform.position.z);
            Vector2 yachtPos2D = new Vector2(yacht.transform.position.x, yacht.transform.position.z);
            Vector2 leverArm = forcePosition2D - yachtPos2D;

            // Safety check: lever arm
            if (leverArm.sqrMagnitude < 0.001f)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[FinalPhysics] Lever arm too small, skipping torque");
                return;
            }

            // Cross product w 2D
            float torque = leverArm.x * sailForce2D.y - leverArm.y * sailForce2D.x;

            // Aplikuj torque z mnożnikami
            float finalTorque = torque * mainSail.torqueMultiplier * sailResult.torqueMultiplier;

            // Safety check: torque
            if (!IsValidFloat(finalTorque))
            {
                Debug.LogError($"[FinalPhysics] Invalid torque! " +
                              $"torque={torque}, " +
                              $"leverArm={leverArm}, " +
                              $"sailForce2D={sailForce2D}, " +
                              $"torqueMultiplier={mainSail.torqueMultiplier}, " +
                              $"deadZoneFactor={sailResult.torqueMultiplier}");
                return;
            }

            yacht.ApplyTorque(finalTorque);
            lastTorque = finalTorque;

            if (enableDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[FinalPhysics] Torque: {finalTorque:F1}Nm, " +
                          $"leverArm: {leverArm.magnitude:F2}m");
            }
        }
    }
    
    void ApplyWaterResistance()
    {
        Vector2 velocity = yacht.GetVelocity2D();
        Vector2 forwardDir = yacht.GetForwardDirection2D();
        
        // Safety check: forward direction
        if (forwardDir.sqrMagnitude < 0.001f)
        {
            Debug.LogWarning("[FinalPhysics] Forward direction is zero!");
            return;
        }
        forwardDir.Normalize();
        
        // Rozkład prędkości na składowe
        float forwardSpeed = Vector2.Dot(velocity, forwardDir);
        Vector2 lateralDir = new Vector2(-forwardDir.y, forwardDir.x);
        float lateralSpeed = Vector2.Dot(velocity, lateralDir);
        
        // Opór w kierunku ruchu
        Vector2 forwardDrag = -forwardDir * forwardSpeed * waterDragCoefficient;
        
        // Zwiększony opór boczny
        Vector2 lateralDrag = -lateralDir * lateralSpeed * waterDragCoefficient * lateralDragMultiplier;
        
        Vector2 totalDrag = forwardDrag + lateralDrag;
        
        // Safety check
        if (!IsValidVector2(totalDrag))
        {
            Debug.LogError($"[FinalPhysics] Invalid water drag! " +
                          $"forwardDrag={forwardDrag}, lateralDrag={lateralDrag}");
            return;
        }
        
        yacht.ApplyForce(totalDrag);
        
        // Opór obrotowy
        float angularVel = yacht.GetAngularVelocity();
        float angularDrag = -angularVel * waterDragCoefficient;
        
        if (!IsValidFloat(angularDrag))
        {
            Debug.LogError($"[FinalPhysics] Invalid angular drag! angularVel={angularVel}");
            return;
        }
        
        yacht.ApplyTorque(angularDrag);
    }

    // Safety check helpers
    bool IsValidFloat(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    bool IsValidVector2(Vector2 v)
    {
        return IsValidFloat(v.x) && IsValidFloat(v.y);
    }

    bool IsValidVector(Vector3 v)
    {
        return IsValidFloat(v.x) && IsValidFloat(v.y) && IsValidFloat(v.z);
    }

    // Gizmos dla debugowania
    void OnDrawGizmos()
    {
        if (!showForceGizmos || !Application.isPlaying || yacht == null)
            return;

        Vector3 yachtPos = yacht.transform.position;

        // Siła żagla (zielona)
        if (lastSailForce.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(yachtPos, yachtPos + lastSailForce * 0.01f); // Skalowanie dla widoczności
            Gizmos.DrawSphere(yachtPos + lastSailForce * 0.01f, 0.1f);
        }

        // Prędkość jachtu (niebieska)
        Vector3 velocity = yacht.GetVelocity();
        if (velocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(yachtPos, yachtPos + velocity);
        }

        // Kierunek wiatru (czerwony)
        if (windSystem != null)
        {
            Vector2 windDir = windSystem.GetWindDirection();
            float windSpeed = windSystem.GetWindSpeed();
            Vector3 windDir3D = new Vector3(windDir.x, 0, windDir.y) * windSpeed * 0.5f;
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(yachtPos + Vector3.up * 2f, yachtPos + Vector3.up * 2f + windDir3D);
        }
    }
}