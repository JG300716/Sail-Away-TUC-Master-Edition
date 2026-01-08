using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class YachtController : MonoBehaviour
{
    [Header("Yacht Settings")]
    public float yachtLength = 2f;
    public float yachtWidth = 0.8f;
    public Color yachtColor = Color.white;
    public Color directionArrowColor = Color.red;
    
    [Header("Physics")]
    public float mass = 100f;
    public float drag = 0.5f;
    public float angularDrag = 0.8f;
    
    [Header("Manual Control")]
    public float rudderTorque = 10f;
    public float sailAdjustmentSpeed = 30f; // stopni/sekundę
    public Sail activeSail = null; // Żagiel do ręcznej kontroli
    
    private Rigidbody rb;
    private GameObject directionArrow;
    private YachtPhysicsSimulator physicsSimulator;
    
    void Start()
    {
        SetupYacht();
        SetupPhysics();
        
        // Znajdź physics simulator
        physicsSimulator = GetComponent<YachtPhysicsSimulator>();
    }
    
    void Update()
    {
        HandleManualControl();
    }
    
    void HandleManualControl()
    {
        // Sterowanie kierunkiem (A/D lub Q/E)
        float steerInput = 0f;
        
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q))
            steerInput = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.E))
            steerInput = 1f;
        
        if (steerInput != 0)
        {
            ApplyTorque(steerInput * rudderTorque);
        }

        // Wybór aktywnego żagla
        if (Input.GetKeyDown(KeyCode.Alpha1) && physicsSimulator != null)
        {
            activeSail = physicsSimulator.mainSail;
            Debug.Log("Selected Main Sail");
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2) && physicsSimulator != null)
        {
            activeSail = physicsSimulator.jibSail;
            Debug.Log("Selected Jib Sail");
        }
        
        // Manualne sterowanie żaglami (Z/X)
        if (activeSail != null)
        {
            if (Input.GetKey(KeyCode.Z))
                activeSail.RotateSail(sailAdjustmentSpeed * Time.deltaTime);
            if (Input.GetKey(KeyCode.X))
                activeSail.RotateSail(-sailAdjustmentSpeed * Time.deltaTime);
        }
    }
    
    void SetupYacht()
    {
        // Główny prostokąt jachtu - obrócony do płaszczyzny XZ
        GameObject yachtBody = new GameObject("YachtBody");
        yachtBody.transform.SetParent(transform);
        yachtBody.transform.localPosition = Vector3.zero;
        yachtBody.transform.localRotation = Quaternion.Euler(90, 0, 0); // Obrót sprite do XZ
        
        SpriteRenderer sr = yachtBody.AddComponent<SpriteRenderer>();
        sr.sprite = CreateRectangleSprite(yachtLength, yachtWidth, yachtColor);
        sr.sortingOrder = 1;
        
        // Strzałka kierunku (igła) - wskazuje forward (oś Z)
        directionArrow = new GameObject("DirectionArrow");
        directionArrow.transform.SetParent(transform);
        directionArrow.transform.localPosition = new Vector3(0, 0, yachtLength * 0.6f);
        directionArrow.transform.localRotation = Quaternion.Euler(90, 0, 0); // Obrót sprite do XZ
        
        SpriteRenderer arrowSr = directionArrow.AddComponent<SpriteRenderer>();
        arrowSr.sprite = CreateArrowSprite(0.3f, 0.5f, directionArrowColor);
        arrowSr.sortingOrder = 2;
    }
    
    void SetupPhysics()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.useGravity = false;
        
        // KLUCZOWE: Ograniczenia dla ruchu 2D w płaszczyźnie XZ
        rb.constraints = RigidbodyConstraints.FreezePositionY | 
                        RigidbodyConstraints.FreezeRotationX | 
                        RigidbodyConstraints.FreezeRotationZ;
        
        BoxCollider collider = gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(yachtWidth, 0.5f, yachtLength);
    }
    
    Sprite CreateRectangleSprite(float length, float width, Color color)
    {
        int pixelsPerUnit = 100;
        int heightPx = Mathf.RoundToInt(length * pixelsPerUnit);
        int widthPx = Mathf.RoundToInt(width * pixelsPerUnit);
        
        Texture2D texture = new Texture2D(widthPx, heightPx);
        Color[] pixels = new Color[widthPx * heightPx];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, widthPx, heightPx), 
            new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
    
    Sprite CreateArrowSprite(float width, float height, Color color)
    {
        int pixelsPerUnit = 100;
        int heightPx = Mathf.RoundToInt(height * pixelsPerUnit);
        int widthPx = Mathf.RoundToInt(width * pixelsPerUnit);
        
        Texture2D texture = new Texture2D(widthPx, heightPx);
        
        for (int y = 0; y < heightPx; y++)
        {
            for (int x = 0; x < widthPx; x++)
            {
                float normalizedY = (float)y / heightPx;
                float normalizedX = (float)x / widthPx;
                
                // Trójkąt strzałki
                if (normalizedY > 0.6f)
                {
                    float tipWidth = (1f - normalizedY) * 2.5f;
                    if (Mathf.Abs(normalizedX - 0.5f) < tipWidth * 0.5f)
                    {
                        texture.SetPixel(x, y, color);
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
                // Trzon strzałki
                else if (Mathf.Abs(normalizedX - 0.5f) < 0.15f)
                {
                    texture.SetPixel(x, y, color);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, widthPx, heightPx), 
            new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
    
    // Publiczne metody do kontroli jachtu z algorytmu
    public void ApplyForce(Vector3 force)
    {
        force.y = 0; // Tylko XZ
        rb.AddForce(force);
    }
    
    public void ApplyForce(Vector2 force)
    {
        // Vector2 (x,y) -> Vector3 (x,0,z) - mapowanie 2D na XZ
        rb.AddForce(new Vector3(force.x, 0, force.y));
    }
    
    public void ApplyTorque(float torque)
    {
        // Moment obrotowy wokół osi Y (vertical)
        rb.AddTorque(Vector3.up * torque);
    }
    
    public Vector3 GetVelocity()
    {
        return rb.linearVelocity;
    }
    
    public Vector2 GetVelocity2D()
    {
        // Vector3 (x,y,z) -> Vector2 (x,z) - mapowanie XZ na 2D
        return new Vector2(rb.linearVelocity.x, rb.linearVelocity.z);
    }
    
    public float GetAngularVelocity()
    {
        // Tylko komponent Y (obrót wokół vertical)
        return rb.angularVelocity.y;
    }
    
    public Vector3 GetAngularVelocityVector()
    {
        return rb.angularVelocity;
    }

    public void ApplyTorque(Vector3 torqueVector)
    {
        rb.AddTorque(torqueVector);
    }

    public void ApplyTorquePitch(float torque)
    {
        rb.AddTorque(Vector3.right * torque);
    }

    public void ApplyTorqueRoll(float torque)
    {
        rb.AddTorque(Vector3.forward * torque);
    }
    
    public Vector3 GetForwardDirection()
    {
        // Forward w 3D to oś Z
        return transform.forward;
    }
    
    public Vector2 GetForwardDirection2D()
    {
        // Vector3 (x,y,z) -> Vector2 (x,z) - mapowanie XZ na 2D
        Vector3 forward = transform.forward;
        return new Vector2(forward.x, forward.z);
    }
}