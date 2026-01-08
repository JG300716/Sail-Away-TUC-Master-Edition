using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Zunifikowany system żagla:
/// - Pełna fizyka aerodynamiczna (z Sail.cs)
/// - Wizualne trzepotanie Cloth (z SailClothPhysics.cs)
/// </summary>
public class UnifiedSail : MonoBehaviour
{
    
    [Header("Sail Visual Settings")]
    public float sailLength = 1.5f;
    public float sailWidth = 0.1f;
    public Color sailColor = new Color(1f, 1f, 1f, 0.8f);
    
    [Header("Sail Area & Coefficients")]
    [Tooltip("Powierzchnia żagla w m² (auto-obliczana z length × width jeśli 0)")]
    public float sailArea = 0f;
    
    [Tooltip("Aspect Ratio (wysokość²/powierzchnia) - wpływa na efektywność")]
    public float aspectRatio = 0f;
    
    [Tooltip("Maksymalny Coefficient of Lift przy optymalnym angle of attack")]
    public float maxCoefficientOfLift = 1.5f;
    
    [Tooltip("Coefficient of Drag przy biegu z wiatrem (downwind)")]
    public float maxCoefficientOfDrag = 1.33f;
    
    [Tooltip("Mnożnik siły dla dostosowania do gameplay")]
    public float forceMultiplier = 10f;
    
    [Tooltip("Mnożnik momentu obrotowego od żagla")]
    public float torqueMultiplier = 0.1f;
    
    [Tooltip("Oswald efficiency number (e) - typowo 0.85-0.95 dla żagli")]
    public float oswaldEfficiency = 0.9f;
    
    [Header("Rotation Constraints")]
    [Tooltip("Minimalny kąt obrotu żagla względem przodu jachtu")]
    public float minAngle = -90f;
    
    [Tooltip("Maksymalny kąt obrotu żagla względem przodu jachtu")]
    public float maxAngle = 90f;
    
    [Header("Joint Configuration (for Boom)")]
    [Tooltip("Czy żagiel jest na joicie (np. boom)? Jeśli tak, używa lokalnych kątów")]
    public bool isOnJoint = false;
    
    [Tooltip("Auto-wykryj Joint component i użyj jego limitów")]
    public bool autoDetectJoint = true;
    
    [Tooltip("Początkowy offset kąta jointa (jeśli joint startuje pod kątem)")]
    public float jointInitialOffset = 0f;
    
    [Tooltip("Kierunek obrotu jointa: 1 = normalny, -1 = odwrócony")]
    public float jointDirection = 1f;
    
    [Header("Spline Movement (for Jib/Fok)")]
    [Tooltip("Czy żagiel używa Spline do kontroli końcówki?")]
    public bool useSplineMovement = false;
    
    [Tooltip("Spline Container definiujący ścieżkę ruchu KOŃCÓWKI")]
    public UnityEngine.Splines.SplineContainer splineContainer;
    
    [Tooltip("Pozycja na spline (0 = początek, 1 = koniec)")]
    [Range(0f, 1f)]
    public float splinePosition = 0.5f;
    
    [Tooltip("Bone/Transform końcówki foka (np. FokShot) - WYMAGANE dla spline")]
    public Transform controlBone;
    
    [Tooltip("Drugi bone do synchronizacji (opcjonalny, np. FokBone)")]
    public Transform secondaryBone;
    
    [Header("Visual Alignment")]
    [Tooltip("Offset kąta dla wizualizacji (żeby zielona linia pokrywała się z fizycznym bomem)")]
    [Range(-180f, 180f)]
    public float visualAngleOffset = 0f;
    
    [Tooltip("Auto-oblicz visual offset z różnicy między transform.forward a target direction")]
    public bool autoCalculateVisualOffset = false;
    
    [Tooltip("Target direction dla auto-kalkulacji (lokalny forward boom w rest position)")]
    public Vector3 targetRestDirection = Vector3.forward;
    
    private HingeJoint hingeJoint;
    private ConfigurableJoint configurableJoint;
    private Transform splineTransform;
    
    [Header("Anchor Point")]
    public Vector2 anchorPointLocal = Vector2.zero;
    
    // ===================================================================
    // SEKCJA 2: CLOTH VISUAL EFFECTS (z SailClothPhysics)
    // ===================================================================
    
    [Header("Cloth Visual Effects")]
    [Tooltip("Włącz Cloth component dla realistycznego trzepotania")]
    public bool useClothVisuals = true;
    
    [Tooltip("Referencja do Cloth component (opcjonalne - auto-znajdzie)")]
    public Cloth sailCloth;
    
    [Tooltip("Siła wiatru aplikowana na Cloth (tylko wizualnie)")]
    [Range(0f, 5f)]
    public float clothWindForceMultiplier = 1.0f;
    
    [Tooltip("Siła losowych podmuchów dla realizmu")]
    [Range(0f, 5f)]
    public float clothGustStrength = 2.0f;
    
    [Tooltip("Prędkość animacji podmuchów")]
    [Range(0f, 2f)]
    public float clothGustSpeed = 0.5f;
    
    [Header("Apparent Wind (dla Cloth)")]
    [Tooltip("Maksymalny apparent wind dla bezpieczeństwa [m/s]")]
    public float maxApparentWind = 30f;
    
    // ===================================================================
    // SEKCJA 3: PRYWATNE ZMIENNE
    // ===================================================================
    
    private Transform yachtTransform;
    private Rigidbody yachtRigidbody;
    private float currentAngle = 0f;
    private UnifiedWindManager windManager;
    
    // ===================================================================
    // SEKCJA 4: INICJALIZACJA
    // ===================================================================
    
    void Start()
    {
        InitializeComponents();
        SetupSail();
    }
    
    void InitializeComponents()
    {
        yachtTransform = transform.parent;
        
        // Auto-wykryj Spline
        if (useSplineMovement && splineContainer != null)
        {
            splineTransform = splineContainer.transform;
            Debug.Log($"[UnifiedSail] Using Spline movement for {gameObject.name}");
        }
        
        // Auto-wykryj Joint (tylko jeśli NIE używamy Spline)
        if (!useSplineMovement && autoDetectJoint)
        {
            hingeJoint = GetComponent<HingeJoint>();
            configurableJoint = GetComponent<ConfigurableJoint>();
            
            if (hingeJoint != null)
            {
                isOnJoint = true;
                Debug.Log($"[UnifiedSail] Auto-detected HingeJoint on {gameObject.name}");
                
                // Odczytaj limity z jointa
                if (hingeJoint.useLimits)
                {
                    minAngle = hingeJoint.limits.min;
                    maxAngle = hingeJoint.limits.max;
                    Debug.Log($"[UnifiedSail] Using HingeJoint limits: min={minAngle:F1}°, max={maxAngle:F1}°");
                }
            }
            else if (configurableJoint != null)
            {
                isOnJoint = true;
                Debug.Log($"[UnifiedSail] Auto-detected ConfigurableJoint on {gameObject.name}");
                
                // ConfigurableJoint może używać różnych osi - sprawdź wszystkie
                bool limitsFound = false;
                
                // Sprawdź Angular X Limit
                // WAŻNE: ConfigurableJoint Angular X działa inaczej!
                // lowAngularXLimit = -negative direction
                // highAngularXLimit = +positive direction
                if (configurableJoint.angularXMotion == ConfigurableJointMotion.Limited)
                {
                    float lowLimit = configurableJoint.lowAngularXLimit.limit;
                    float highLimit = configurableJoint.highAngularXLimit.limit;
                    
                    // ConfigurableJoint używa konwencji: low=-negative, high=+positive
                    minAngle = -lowLimit;  // Ujemny kierunek (w lewo)
                    maxAngle = highLimit;  // Dodatni kierunek (w prawo)
                    
                    limitsFound = true;
                    Debug.Log($"[UnifiedSail] ConfigurableJoint Angular X raw limits: low={lowLimit:F1}°, high={highLimit:F1}°");
                    Debug.Log($"[UnifiedSail] Converted to sail angles: min={minAngle:F1}°, max={maxAngle:F1}°");
                }
                // Sprawdź Angular Y Limit
                else if (configurableJoint.angularYMotion == ConfigurableJointMotion.Limited)
                {
                    minAngle = -configurableJoint.angularYLimit.limit;
                    maxAngle = configurableJoint.angularYLimit.limit;
                    limitsFound = true;
                    Debug.Log($"[UnifiedSail] Using ConfigurableJoint Angular Y limits: min={minAngle:F1}°, max={maxAngle:F1}°");
                }
                // Sprawdź Angular Z Limit
                else if (configurableJoint.angularZMotion == ConfigurableJointMotion.Limited)
                {
                    minAngle = -configurableJoint.angularZLimit.limit;
                    maxAngle = configurableJoint.angularZLimit.limit;
                    limitsFound = true;
                    Debug.Log($"[UnifiedSail] Using ConfigurableJoint Angular Z limits: min={minAngle:F1}°, max={maxAngle:F1}°");
                }
                
                if (!limitsFound)
                {
                    Debug.LogWarning($"[UnifiedSail] ConfigurableJoint found but no limits detected! Using manual min={minAngle:F1}°, max={maxAngle:F1}°");
                }
            }
        }
        
        // Znajdź WindManager
        windManager = FindObjectOfType<UnifiedWindManager>();
        if (windManager == null)
        {
            Debug.LogError($"[UnifiedSail] UnifiedWindManager not found in scene!");
        }
        
        // Znajdź Rigidbody jachtu
        if (yachtTransform != null)
        {
            yachtRigidbody = yachtTransform.GetComponent<Rigidbody>();
            if (yachtRigidbody == null)
            {
                Debug.LogWarning($"[UnifiedSail] No Rigidbody found on yacht parent!");
            }
        }
        
        // Auto-znajdź Cloth jeśli włączone
        if (useClothVisuals && sailCloth == null)
        {
            sailCloth = GetComponentInChildren<Cloth>();
            if (sailCloth != null)
            {
                Debug.Log($"[UnifiedSail] Auto-found Cloth component on {sailCloth.gameObject.name}");
            }
        }
        
        // Ustaw początkowy kąt
        float centerAngle = (minAngle + maxAngle) / 2f;
        currentAngle = (minAngle <= 0f && maxAngle >= 0f) ? 0f : centerAngle;
    }
    
    void SetupSail()
    {
        // Dla Spline: NIE zmieniamy pozycji (zostaje 0,0,0)
        // Dla normalnych żagli: ustawiamy anchor point
        if (!useSplineMovement && !isOnJoint)
        {
            transform.localPosition = new Vector3(anchorPointLocal.x, 0, anchorPointLocal.y);
        }
        
        // Ustaw początkową rotację (dla jointów)
        if (isOnJoint)
        {
            float centerAngle = (minAngle + maxAngle) / 2f;
            currentAngle = jointInitialOffset != 0f ? jointInitialOffset : centerAngle;
            SetSailAngle(currentAngle);
        }
        else if (useSplineMovement)
        {
            // Dla Spline: ustaw początkową pozycję końcówki
            currentAngle = 0f;
            UpdateSplinePosition();
        }
        else
        {
            // Dla normalnego żagla
            float centerAngle = (minAngle + maxAngle) / 2f;
            currentAngle = (minAngle <= 0f && maxAngle >= 0f) ? 0f : centerAngle;
            SetSailAngle(currentAngle);
        }
        
        // Auto-kalkulacja visual offset (tylko dla jointów)
        if (autoCalculateVisualOffset && isOnJoint)
        {
            Vector3 currentLocalForward = transform.localRotation * Vector3.forward;
            float angleDiff = Vector3.SignedAngle(targetRestDirection, currentLocalForward, Vector3.up);
            visualAngleOffset = -angleDiff;
            
            Debug.Log($"[UnifiedSail] Auto-calculated visualAngleOffset={visualAngleOffset:F1}° " +
                      $"(currentForward={currentLocalForward}, target={targetRestDirection})");
        }
        
        // Jeśli nie ma Cloth, stwórz prostą wizualizację sprite
        if (!useClothVisuals || sailCloth == null)
        {
            CreateSpriteVisualization();
        }
        
        string setupInfo = $"[UnifiedSail] {gameObject.name} initialized: ";
        if (useSplineMovement)
        {
            setupInfo += $"useSpline=True, splinePos={splinePosition:F2}, controlBone={controlBone?.name}";
        }
        else if (isOnJoint)
        {
            setupInfo += $"isOnJoint=True, currentAngle={currentAngle:F1}°, visualOffset={visualAngleOffset:F1}°";
        }
        else
        {
            setupInfo += $"standard sail, currentAngle={currentAngle:F1}°";
        }
        Debug.Log(setupInfo);
    }
    
    void CreateSpriteVisualization()
    {
        GameObject sailVisual = new GameObject("SailVisual");
        sailVisual.transform.SetParent(transform);
        sailVisual.transform.localPosition = Vector3.zero;
        sailVisual.transform.localRotation = Quaternion.Euler(90, 0, 0);
        
        SpriteRenderer sr = sailVisual.AddComponent<SpriteRenderer>();
        sr.sprite = CreateRectangleSprite(sailLength, sailWidth, sailColor);
        sr.sortingOrder = 3;
    }
    
    Sprite CreateRectangleSprite(float length, float width, Color color)
    {
        int pixelsPerUnit = 100;
        int heightPx = Mathf.RoundToInt(length * pixelsPerUnit);
        int widthPx = Mathf.RoundToInt(width * pixelsPerUnit);
        
        Texture2D texture = new Texture2D(widthPx, heightPx);
        Color[] pixels = new Color[widthPx * heightPx];
        
        for (int y = 0; y < heightPx; y++)
        {
            for (int x = 0; x < widthPx; x++)
            {
                if (x == 0 || x == widthPx - 1 || y == 0 || y == heightPx - 1)
                    pixels[y * widthPx + x] = Color.black;
                else
                    pixels[y * widthPx + x] = color;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, widthPx, heightPx), 
            new Vector2(0.5f, 0f), pixelsPerUnit);
    }
    
    // ===================================================================
    // SEKCJA 5: UPDATE - CLOTH VISUAL EFFECTS
    // ===================================================================
    
    void FixedUpdate()
    {
        if (useClothVisuals && sailCloth != null && windManager != null)
        {
            ApplyClothWind();
        }
    }
    
    /// <summary>
    /// Aplikuje wiatr pozorny na Cloth dla realistycznego trzepotania
    /// (tylko wizualny efekt, nie wpływa na fizykę jachtu)
    /// </summary>
    void ApplyClothWind()
    {
        // Pobierz prawdziwy wiatr
        Vector3 trueWind = windManager.GetWindDirection3D() * windManager.GetWindSpeed();
        
        // Oblicz wiatr pozorny (apparent wind)
        Vector3 boatWind = yachtRigidbody != null ? -yachtRigidbody.linearVelocity : Vector3.zero;
        Vector3 apparentWind = trueWind + boatWind;
        
        // Ogranicz maksymalny apparent wind
        if (apparentWind.magnitude > maxApparentWind)
        {
            apparentWind = apparentWind.normalized * maxApparentWind;
        }
        
        // Aplikuj na Cloth
        Vector3 windAcceleration = apparentWind * clothWindForceMultiplier;
        sailCloth.externalAcceleration = windAcceleration;
        
        // Losowe podmuchy dla realizmu
        float gustStrength = Mathf.PerlinNoise(Time.time * clothGustSpeed, 0) * clothGustStrength;
        sailCloth.randomAcceleration = Vector3.one * gustStrength;
    }
    
    // ===================================================================
    // SEKCJA 6: STEROWANIE KĄTEM ŻAGLA
    // ===================================================================
    
    public void SetSailAngle(float angle)
    {
        Debug.Log($"[UnifiedSail] {gameObject.name} SetSailAngle: currentAngle={angle:F1}°");
        if (useSplineMovement)
        {
            // Dla Spline: interpretuj angle jako pozycję na spline (0-360 → 0-1)
            // Lub bezpośrednio jako splinePosition jeśli w zakresie 0-1
            if (angle >= -1f && angle <= 1f)
            {
                splinePosition = Mathf.Clamp01(angle);
            }
            else
            {
                // Konwertuj kąt na pozycję spline
                // -90 to 90 → 0 to 1
                float normalizedAngle = (angle - minAngle) / (maxAngle - minAngle);
                splinePosition = Mathf.Clamp01(normalizedAngle);
            }
            
            currentAngle = angle; // Zapamiętaj dla kompatybilności
            UpdateSplinePosition();
            return;
        }
        
        // Normalny kod dla jointów/rotacji
        // Clamp do zakresu
        currentAngle = Mathf.Clamp(angle, minAngle, maxAngle);
        
        if (isOnJoint)
        {
            // Dla jointa: ustaw lokalny kąt z kierunkiem
            float localAngle = currentAngle * jointDirection;
            transform.localRotation = Quaternion.Euler(0, currentAngle, 0);
            
            Debug.Log($"[UnifiedSail Joint] {gameObject.name}:" + $"  currentAngle={currentAngle:F1}° (clamped)" + $"  localAngle={localAngle:F1}° (with direction)" + $"  localRotation.y={transform.localRotation.eulerAngles.y:F1}°" + $"  worldRotation.y={transform.rotation.eulerAngles.y:F1}°");
        }
        else
        {
            // Normalny żagiel: bezpośrednie ustawienie
            //transform.localRotation = Quaternion.Euler(0, currentAngle, 0);
        }
        
    }
    
    void UpdateSplinePosition()
    {
        if (splineContainer == null || splineContainer.Spline == null)
        {
            Debug.LogWarning($"[UnifiedSail] SplineContainer or Spline is null on {gameObject.name}");
            return;
        }
        
        if (controlBone == null)
        {
            Debug.LogWarning($"[UnifiedSail] Control Bone is required for Spline movement on {gameObject.name}");
            return;
        }
        
        var spline = splineContainer.Spline;
        
        if (spline.Count == 0)
        {
            Debug.LogWarning($"[UnifiedSail] Spline has no points on {gameObject.name}");
            return;
        }
        
        // Oblicz pozycję i rotację na spline
        Vector3 localPosition = spline.EvaluatePosition(splinePosition);
        Vector3 worldPosition = splineTransform.TransformPoint(localPosition);
        
        Vector3 localTangent = spline.EvaluateTangent(splinePosition);
        Vector3 worldTangent = splineTransform.TransformDirection(localTangent);
        
        // Ustaw TYLKO controlBone (końcówka foka), NIE sam UnifiedSail transform!
        controlBone.position = worldPosition;
        controlBone.rotation = Quaternion.LookRotation(worldTangent, Vector3.up);
        
        // Jeśli jest secondary bone, również zaktualizuj
        if (secondaryBone != null)
        {
            secondaryBone.position = worldPosition;
            secondaryBone.rotation = controlBone.rotation;
        }
    }
    
    public void RotateSail(float deltaAngle)
    {
        SetSailAngle(currentAngle + deltaAngle);
    }
    
    public float GetCurrentAngle()
    {
        return currentAngle;
    }
    
    /// <summary>
    /// Zwraca min/max kąty dla kontrolera
    /// </summary>
    public void GetAngleLimits(out float min, out float max)
    {
        min = minAngle;
        max = maxAngle;
    }
    
    /// <summary>
    /// Sprawdza czy można sterować żaglem (dla YachtController)
    /// </summary>
    public bool CanControl()
    {
        return gameObject.activeInHierarchy && enabled;
    }
    
    /// <summary>
    /// Ustawia pozycję na spline (dla foka)
    /// </summary>
    /// <param name="position">Pozycja 0-1 na spline</param>
    public void SetSplinePosition(float position)
    {
        if (!useSplineMovement)
        {
            Debug.LogWarning($"[UnifiedSail] {gameObject.name} is not using spline movement!");
            return;
        }
        
        splinePosition = Mathf.Clamp01(position);
        UpdateSplinePosition();
    }
    
    /// <summary>
    /// Przesuwa pozycję na spline o delta (dla foka)
    /// </summary>
    /// <param name="delta">Zmiana pozycji (-1 do 1)</param>
    public void MoveAlongSpline(float delta)
    {
        if (!useSplineMovement)
        {
            Debug.LogWarning($"[UnifiedSail] {gameObject.name} is not using spline movement!");
            return;
        }
        
        SetSplinePosition(splinePosition + delta);
    }
    
    /// <summary>
    /// Zwraca obecną pozycję na spline (0-1)
    /// </summary>
    public float GetSplinePosition()
    {
        return splinePosition;
    }
    
    // ===================================================================
    // SEKCJA 7: OBLICZENIA FIZYKI (z Sail.cs)
    // ===================================================================
    
    public struct SailForceResult
    {
        public Vector3 force;
        public float torqueMultiplier;
        public bool inDeadZone;
        public float angleOfAttack;
    }
    
    /// <summary>
    /// Główna metoda obliczająca siły aerodynamiczne na żaglu
    /// </summary>
    public SailForceResult CalculateWindForceWithTorque(Vector3 windDirection, float windSpeed)
    {
        SailForceResult result = new SailForceResult();
        
        // Normalizuj wiatr (tylko XZ)
        windDirection.y = 0;
        if (windDirection.magnitude < 0.001f)
        {
            result.force = Vector3.zero;
            result.torqueMultiplier = 0f;
            result.inDeadZone = true;
            result.angleOfAttack = 0f;
            return result;
        }
        windDirection.Normalize();
        
        // Kierunek żagla (chord line)
        Vector3 sailChord = transform.forward;
        sailChord.y = 0;
        sailChord.Normalize();
        
        // Angle of Attack
        float angleOfAttack = Vector3.SignedAngle(windDirection, sailChord, Vector3.up);
        result.angleOfAttack = angleOfAttack;
        
        // === CHECK DEAD ZONE ===
        if (windManager == null)
        {
            Debug.LogWarning("[UnifiedSail] WindManager not found!");
            result.force = Vector3.zero;
            result.torqueMultiplier = 0f;
            result.inDeadZone = true;
            return result;
        }
        
        // Oblicz kąt jachtu względem wiatru
        Vector3 yachtForward = Vector3.forward;
        if (transform.parent != null)
        {
            yachtForward = transform.parent.forward;
            yachtForward.y = 0;
            yachtForward.Normalize();
        }
        
        float yachtAngleToWind = Vector3.SignedAngle(yachtForward, windDirection, Vector3.up);
        
        // Pobierz efektywność z WindManager
        float deadZoneFactor = windManager.GetEfficiencyMultiplier(yachtAngleToWind);
        UnifiedWindManager.PointOfSail currentPos = windManager.GetPointOfSailForAngle(yachtAngleToWind);
        
        // inDeadZone
        UnifiedWindManager.PointOfSail deadZoneCourse = windManager.pointsOfSail[windManager.pointsOfSail.Length - 1];
        result.inDeadZone = (currentPos.name == deadZoneCourse.name);
        
        // === CALCULATE PARAMETERS ===
        float area = sailArea > 0 ? sailArea : sailLength * sailWidth;
        float AR = aspectRatio > 0 ? aspectRatio : (sailLength * sailLength) / area;
        float airDensity = 1.225f;
        float dynamicPressure = 0.5f * airDensity * windSpeed * windSpeed;
        
        // === DEAD ZONE HANDLING ===
        if (deadZoneFactor <= 0f)
        {
            Vector3 yachtVelocity = Vector3.zero;
            if (transform.parent != null && yachtRigidbody != null)
            {
                yachtVelocity = yachtRigidbody.linearVelocity;
                yachtVelocity.y = 0;
            }
            
            // Tylko hamowanie (deadZoneFactor = 0)
            if (Mathf.Abs(deadZoneFactor) < 0.01f)
            {
                if (yachtVelocity.magnitude > 0.01f)
                {
                    Vector3 dragDir = -yachtVelocity.normalized;
                    float dragMag = 0.5f * dynamicPressure * area;
                    result.force = dragDir * dragMag * forceMultiplier;
                }
                else
                {
                    result.force = Vector3.zero;
                }
            }
            else // Ujemna efektywność = hamowanie + cofanie
            {
                float reverseFactor = Mathf.Abs(deadZoneFactor);
                
                // 1. Drag hamujący
                Vector3 dragForce = Vector3.zero;
                if (yachtVelocity.magnitude > 0.01f)
                {
                    Vector3 dragDir = -yachtVelocity.normalized;
                    float dragMag = reverseFactor * dynamicPressure * area;
                    dragForce = dragDir * dragMag * forceMultiplier;
                }
                
                // 2. Siła cofająca
                Vector3 reverseDir = -windDirection;
                float reverseMag = reverseFactor * dynamicPressure * area * 0.5f;
                Vector3 reverseForce = reverseDir * reverseMag * forceMultiplier;
                
                result.force = dragForce + reverseForce;
            }
            
            result.torqueMultiplier = 0f;
            return result;
        }
        
        // === NORMAL AERODYNAMIC FORCES ===
        float absAngle = Mathf.Abs(angleOfAttack);
        float CL = CalculateLiftCoefficient(absAngle);
        float CD = CalculateDragCoefficient(absAngle, CL, AR);
        
        float liftMagnitude = CL * dynamicPressure * area;
        float aeroDragMagnitude = CD * dynamicPressure * area;
        
        // Decompose to world space
        Vector3 liftDirection = Vector3.Cross(windDirection, Vector3.up);
        if (angleOfAttack < 0)
            liftDirection = -liftDirection;
        
        Vector3 aeroDragDirection = windDirection;
        
        // Total force
        Vector3 totalForce = (liftDirection * liftMagnitude + aeroDragDirection * aeroDragMagnitude) * deadZoneFactor;
        
        result.force = totalForce * forceMultiplier;
        result.torqueMultiplier = deadZoneFactor;
        
        return result;
    }
    
    float CalculateLiftCoefficient(float absAngle)
    {
        if (absAngle < 15f)
            return (maxCoefficientOfLift / 15f) * absAngle;
        else if (absAngle < 30f)
        {
            float t = (absAngle - 15f) / 15f;
            return Mathf.Lerp(maxCoefficientOfLift, maxCoefficientOfLift * 0.53f, t);
        }
        else if (absAngle < 90f)
        {
            float t = (absAngle - 30f) / 60f;
            return Mathf.Lerp(maxCoefficientOfLift * 0.53f, maxCoefficientOfLift * 0.067f, t);
        }
        else
            return maxCoefficientOfLift * 0.067f;
    }
    
    float CalculateDragCoefficient(float absAngle, float CL, float AR)
    {
        // Induced drag
        float inducedDrag = (CL * CL) / (Mathf.PI * oswaldEfficiency * AR);
        
        // Parasitic drag
        float parasiticDrag;
        if (absAngle < 15f)
            parasiticDrag = 0.05f + 0.02f * (absAngle / 15f);
        else if (absAngle < 30f)
        {
            float t = (absAngle - 15f) / 15f;
            parasiticDrag = Mathf.Lerp(0.07f, 0.3f, t);
        }
        else if (absAngle < 90f)
        {
            float t = (absAngle - 30f) / 60f;
            parasiticDrag = Mathf.Lerp(0.3f, maxCoefficientOfDrag, t);
        }
        else
            parasiticDrag = maxCoefficientOfDrag;
        
        return inducedDrag + parasiticDrag;
    }
    
    // ===================================================================
    // SEKCJA 8: POMOCNICZE METODY
    // ===================================================================
    
    public Vector3 CalculateWindForce3D(Vector3 windDirection, float windSpeed)
    {
        return CalculateWindForceWithTorque(windDirection, windSpeed).force;
    }
    
    public Vector2 CalculateWindForce(Vector2 windDirection, float windSpeed)
    {
        Vector3 windDir3D = new Vector3(windDirection.x, 0, windDirection.y);
        Vector3 force3D = CalculateWindForce3D(windDir3D, windSpeed);
        return new Vector2(force3D.x, force3D.z);
    }
    
    public float GetSailArea()
    {
        return sailArea > 0 ? sailArea : sailLength * sailWidth;
    }
    
    public float GetAspectRatio()
    {
        float area = GetSailArea();
        return aspectRatio > 0 ? aspectRatio : (sailLength * sailLength) / area;
    }
    
    public Vector2 GetSailDirection()
    {
        Vector3 dir = transform.forward;
        return new Vector2(dir.x, dir.z);
    }
    
    public Vector3 GetSailDirection3D()
    {
        return transform.forward;
    }
    
    // Kompatybilność z starym API
    public static string GetPointOfSail(float yachtAngleToWind, out string tack, UnifiedWindManager windManager)
    {
        if (windManager != null)
        {
            return windManager.GetPointOfSailName(yachtAngleToWind, out tack);
        }
        
        tack = yachtAngleToWind < -1f ? "Lewy hals" : yachtAngleToWind > 1f ? "Prawy hals" : "";
        return "Unknown";
    }
    
    // ===================================================================
    // SEKCJA 9: WIZUALIZACJA
    // ===================================================================
    
    [Header("Debug Visualization")]
    [Tooltip("Rysuj gizmos (zielona linia kierunku żagla)")]
    public bool drawGizmos = true;
    
    [Tooltip("Rysuj zakresy kątów gdy zaznaczony")]
    public bool drawAngleRanges = true;
    
    void OnDrawGizmos()
    {
        if (!drawGizmos || !Application.isPlaying)
            return;
        
        // Dla Spline: nie rysuj (psuje wizualizację)
        if (useSplineMovement)
            return;
            // Normalna żagla (zielona) - z visual offset
            Gizmos.color = Color.green;
            Vector3 worldPos = transform.position;
            
            // Oblicz kierunek z uwzględnieniem visual offset
            float visualAngle = currentAngle + visualAngleOffset;
            Quaternion visualRotation = transform.parent != null ? 
                transform.parent.rotation * Quaternion.Euler(0, visualAngle, 0) :
                Quaternion.Euler(0, visualAngle, 0);
            
            Vector3 sailNormal = visualRotation * Vector3.forward * sailLength * 0.5f;
            sailNormal.y = 0;
            Gizmos.DrawLine(worldPos, worldPos + sailNormal);
            
            // Oś obrotu (żółta)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(worldPos - Vector3.up * 0.2f, worldPos + Vector3.up * sailLength);
    }

    void DrawArc(Vector3 center, float startAngle, float endAngle, float radius, Transform referenceTransform)
    {
        int segments = 20;
        float angleDiff = endAngle - startAngle;
        
        if (angleDiff > 180f) angleDiff -= 360f;
        else if (angleDiff < -180f) angleDiff += 360f;
        
        float angleStep = angleDiff / segments;
        Quaternion baseRotation = referenceTransform.rotation;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = startAngle + angleStep * i;
            float angle2 = startAngle + angleStep * (i + 1);
            
            Quaternion rot1 = baseRotation * Quaternion.Euler(0, angle1, 0);
            Quaternion rot2 = baseRotation * Quaternion.Euler(0, angle2, 0);
            
            Vector3 point1 = center + rot1 * Vector3.forward * radius;
            Vector3 point2 = center + rot2 * Vector3.forward * radius;
            
            Gizmos.DrawLine(point1, point2);
        }
    }
}