using Game.Scripts;
using Game.Scripts.Controllers;
using UnityEngine;

public class UnifiedYachtPhysics : MonoBehaviour
{
    // =========================================================
    // ====================== REFERENCES =======================
    // =========================================================
    [Header("References")]
    public YAchtUnifiedController2 yacht;
    public UnifiedSail mainSail;
    public SailClothPhysics jibSail;
    public UnifiedWindManager windSystem;

    // =========================================================
    // =================== PHYSICAL PARAMS =====================
    // =========================================================
    [Header("Mass Properties")]
    public float mass = 1200f;                 // kg
    public float momentOfInertia = 2500f;      // yaw inertia

    [Header("Water Resistance")]
    public float waterDragCoefficient = 0.3f;
    public float lateralDragMultiplier = 5f;

    // =========================================================
    // ====================== STATE ============================
    // =========================================================
    private Vector2 velocity2D;
    private float angularVelocity;

    // =========================================================
    // =================== ACCUMULATORS ========================
    // =========================================================
    private Vector2 accumulatedForce2D;
    private float accumulatedTorque;

    // =========================================================
    // ===================== OUTPUT ============================
    // =========================================================
    private Vector2 currentAcceleration2D;
    private float currentAngularAcceleration;

    // =========================================================
    // ======================= UNITY ===========================
    // =========================================================
    void Start()
    {
        if (yacht == null)
            yacht = GetComponent<YAchtUnifiedController2>();

        if (windSystem == null)
            windSystem = FindObjectOfType<UnifiedWindManager>();
    }

    void FixedUpdate()
    {
        if (yacht == null || windSystem == null)
            return;

        // 1. Reset akumulatorów
        accumulatedForce2D = Vector2.zero;
        accumulatedTorque = 0f;

        // 2. Zbieranie sił
        ApplyWindForces();
        ApplyWaterResistance();

        // 3. PRZYŚPIESZENIA
        currentAcceleration2D =
            accumulatedForce2D / Mathf.Max(mass, 0.001f);

        currentAngularAcceleration =
            accumulatedTorque / Mathf.Max(momentOfInertia, 0.001f);

        // 4. Integracja ruchu
        velocity2D += currentAcceleration2D * Time.fixedDeltaTime;
        angularVelocity += currentAngularAcceleration * Time.fixedDeltaTime;
    }

    // =========================================================
    // ==================== WIND FORCES ========================
    // =========================================================
    void ApplyWindForces()
    {
        Vector2 windDir2D = windSystem.GetWindDirection();
        float windSpeed = windSystem.GetWindSpeed();
        Vector3 windDir3D = new Vector3(windDir2D.x, 0, windDir2D.y).normalized;

        // ----------- MAIN SAIL -----------
        if (mainSail != null)
        {
            UnifiedSail.SailForceResult result =
                mainSail.CalculateWindForceWithTorque(windDir3D, windSpeed);

            AccumulateSailForce(
                result.force,
                mainSail.transform.position,
                mainSail.torqueMultiplier,
                result.torqueMultiplier
            );
        }

        // // ----------- JIB -----------
        // if (jibSail != null)
        // {
        //     Vector3 force3D = jibSail.GetTotalForce();
        //     Vector2 force2D = new Vector2(force3D.x, force3D.z);
        //
        //     accumulatedForce2D += force2D;
        //
        //     Vector2 lever =
        //         new Vector2(jibSail.transform.position.x, jibSail.transform.position.z) -
        //         new Vector2(transform.position.x, transform.position.z);
        //
        //     accumulatedTorque +=
        //         lever.x * force2D.y -
        //         lever.y * force2D.x;
        // }
    }

    void AccumulateSailForce(
        Vector3 force3D,
        Vector3 worldPos,
        float sailTorqueMultiplier,
        float aeroTorqueMultiplier)
    {
        Vector2 force2D = new Vector2(force3D.x, force3D.z);
        accumulatedForce2D += force2D;

        if (aeroTorqueMultiplier <= 0.01f)
            return;

        Vector2 lever =
            new Vector2(worldPos.x, worldPos.z) -
            new Vector2(transform.position.x, transform.position.z);

        float torque =
            lever.x * force2D.y -
            lever.y * force2D.x;

        accumulatedTorque +=
            torque * sailTorqueMultiplier * aeroTorqueMultiplier;
    }

    // =========================================================
    // ================== WATER RESISTANCE =====================
    // =========================================================
    void ApplyWaterResistance()
    {
        Vector2 forward = yacht.GetForwardDirection2D();
        Vector2 lateral = new Vector2(-forward.y, forward.x);

        float forwardSpeed = Vector2.Dot(velocity2D, forward);
        float lateralSpeed = Vector2.Dot(velocity2D, lateral);

        Vector2 forwardDrag =
            -forward * forwardSpeed * waterDragCoefficient;

        Vector2 lateralDrag =
            -lateral * lateralSpeed *
            waterDragCoefficient * lateralDragMultiplier;

        accumulatedForce2D += forwardDrag + lateralDrag;

        accumulatedTorque +=
            -angularVelocity * waterDragCoefficient;
    }

    // =========================================================
    // ===================== PUBLIC API ========================
    // =========================================================

    /// <summary>
    /// Zwraca aktualne przyśpieszenie liniowe jachtu (XZ → Vector2)
    /// </summary>
    public Vector2 GetCurrentAcceleration2D()
    {
        return currentAcceleration2D;
    }

    /// <summary>
    /// Zwraca aktualne przyśpieszenie kątowe (yaw)
    /// </summary>
    public float GetCurrentAngularAcceleration()
    {
        return currentAngularAcceleration;
    }

    /// <summary>
    /// Zwraca aktualną prędkość (do HUD/AI)
    /// </summary>
    public Vector2 GetVelocity2D()
    {
        return velocity2D;
    }
}
