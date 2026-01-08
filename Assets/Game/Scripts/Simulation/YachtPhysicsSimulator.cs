using UnityEngine;

public class YachtPhysicsSimulator : MonoBehaviour
{
    [Header("References")]
    public YachtController yacht;
    public Sail mainSail;
    public Sail jibSail; // Drugi żagiel (opcjonalny)
    public WindSystem windSystem;
    
    [Header("Water Resistance")]
    public float waterDragCoefficient = 0.3f;
    public float lateralDragMultiplier = 5f; // Większy opór boczny
    
    void Start()
    {
        // Automatyczne znalezienie komponentów jeśli nie przypisane
        if (yacht == null)
            yacht = GetComponent<YachtController>();
        if (windSystem == null)
            windSystem = FindObjectOfType<WindSystem>();
        
        // Znalezienie żagli
        Sail[] sails = GetComponentsInChildren<Sail>();
        if (sails.Length > 0 && mainSail == null)
            mainSail = sails[0];
        if (sails.Length > 1 && jibSail == null)
            jibSail = sails[1];
    }
    
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
        
        // Siła z głównego żagla
        if (mainSail != null)
        {
            // Konwersja wiatru do 3D
            Vector3 windDir3D = new Vector3(windDir.x, 0, windDir.y);
            
            // Pobierz siłę z pełnymi informacjami
            Sail.SailForceResult sailResult = mainSail.CalculateWindForceWithTorque(windDir3D, windSpeed);
            
            // Aplikacja siły - konwersja Vector3 na Vector2
            Vector2 sailForce2D = new Vector2(sailResult.force.x, sailResult.force.z);
            yacht.ApplyForce(sailForce2D);
            
            // Moment obrotowy od przesunięcia punktu aplikacji siły
            // TYLKO jeśli nie jesteśmy w dead zone
            if (sailResult.torqueMultiplier > 0.01f)
            {
                Vector2 forcePosition2D = new Vector2(mainSail.transform.position.x, mainSail.transform.position.z);
                Vector2 yachtPos2D = new Vector2(yacht.transform.position.x, yacht.transform.position.z);
                Vector2 leverArm = forcePosition2D - yachtPos2D;
                
                // Cross product w 2D: torque = leverArm.x * force.y - leverArm.y * force.x
                float torque = leverArm.x * sailForce2D.y - leverArm.y * sailForce2D.x;
                
                // Aplikuj torque z mnożnikami: torqueMultiplier z żagla
                float finalTorque = torque * mainSail.torqueMultiplier * sailResult.torqueMultiplier;
                yacht.ApplyTorque(finalTorque);
            }
        }
        
        // Siła z drugiego żagla (jeśli istnieje)
        if (jibSail != null)
        {
            Vector3 windDir3D = new Vector3(windDir.x, 0, windDir.y);
            Sail.SailForceResult sailResult = jibSail.CalculateWindForceWithTorque(windDir3D, windSpeed);
            
            Vector2 sailForce2D = new Vector2(sailResult.force.x, sailResult.force.z);
            yacht.ApplyForce(sailForce2D);
            
            // Moment obrotowy też dla drugiego żagla
            if (sailResult.torqueMultiplier > 0.01f)
            {
                Vector2 forcePosition2D = new Vector2(jibSail.transform.position.x, jibSail.transform.position.z);
                Vector2 yachtPos2D = new Vector2(yacht.transform.position.x, yacht.transform.position.z);
                Vector2 leverArm = forcePosition2D - yachtPos2D;
                float torque = leverArm.x * sailForce2D.y - leverArm.y * sailForce2D.x;
                
                float finalTorque = torque * jibSail.torqueMultiplier * sailResult.torqueMultiplier;
                yacht.ApplyTorque(finalTorque);
            }
        }
    }
    
    void ApplyWaterResistance()
    {
        // Używamy wersji 2D dla prostszego kodu
        Vector2 velocity = yacht.GetVelocity2D();
        Vector2 forwardDir = yacht.GetForwardDirection2D();
        
        // Rozkład prędkości na składowe
        float forwardSpeed = Vector2.Dot(velocity, forwardDir);
        float lateralSpeed = Vector2.Dot(velocity, new Vector2(-forwardDir.y, forwardDir.x));
        
        // Opór w kierunku ruchu
        Vector2 forwardDrag = -forwardDir * forwardSpeed * waterDragCoefficient;
        
        // Zwiększony opór boczny (orza/kil jachtu)
        Vector2 lateralDir = new Vector2(-forwardDir.y, forwardDir.x);
        Vector2 lateralDrag = -lateralDir * lateralSpeed * waterDragCoefficient * lateralDragMultiplier;
        
        yacht.ApplyForce(forwardDrag + lateralDrag);
        
        // Opór obrotowy
        float angularVel = yacht.GetAngularVelocity();
        yacht.ApplyTorque(-angularVel * waterDragCoefficient);
    }
}