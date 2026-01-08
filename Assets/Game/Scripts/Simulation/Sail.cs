using UnityEngine;

public class Sail : MonoBehaviour
{
    [Header("Sail Settings")]
    public float sailLength = 1.5f;
    public float sailWidth = 0.1f;
    public Color sailColor = new Color(1f, 1f, 1f, 0.8f);
    
    [Header("Sail Area & Coefficients")]
    [Tooltip("Powierzchnia żagla w m² (auto-obliczana z length × width jeśli 0)")]
    public float sailArea = 0f; // Jeśli 0, używa sailLength × sailWidth
    
    [Tooltip("Aspect Ratio (wysokość²/powierzchnia) - wpływa na efektywność")]
    public float aspectRatio = 0f; // Jeśli 0, auto-oblicza
    
    [Tooltip("Maksymalny Coefficient of Lift przy optymalnym angle of attack")]
    public float maxCoefficientOfLift = 1.5f;
    
    [Tooltip("Coefficient of Drag przy biegu z wiatrem (downwind)")]
    public float maxCoefficientOfDrag = 1.33f;
    
    [Tooltip("Mnożnik siły dla dostosowania do gameplay (domyślnie 10)")]
    public float forceMultiplier = 10f;
    
    [Tooltip("Mnożnik momentu obrotowego od żagla (domyślnie 0.1, zmniejsz dla mniejszego obrotu)")]
    public float torqueMultiplier = 0.1f;
    
    [Tooltip("Oswald efficiency number (e) - typowo 0.85-0.95 dla żagli")]
    public float oswaldEfficiency = 0.9f;
    
    [Header("Rotation Constraints")]
    [Tooltip("Minimalny kąt obrotu żagla względem przodu jachtu (forward)")]
    public float minAngle = -90f;
    
    [Tooltip("Maksymalny kąt obrotu żagla względem przodu jachtu (forward)")]
    public float maxAngle = 90f;
    
    [Header("Anchor Point")]
    public Vector2 anchorPointLocal = Vector2.zero; // Punkt zakotwiczenia w 2D (X,Z)
    
    private Transform yachtTransform;
    private float currentAngle = 0f;
    private WindSystem windSystem; // Referencja do WindSystem
    
    void Start()
    {
        yachtTransform = transform.parent;
        
        // Znajdź WindSystem
        windSystem = FindObjectOfType<WindSystem>();
        if (windSystem == null)
        {
            Debug.LogError("Sail: WindSystem not found in scene!");
        }
        
        // Ustaw początkowy kąt na środek zakresu (lub 0 jeśli w zakresie)
        float centerAngle = (minAngle + maxAngle) / 2f;
        
        // Jeśli 0 jest w zakresie, użyj 0
        if (minAngle <= 0f && maxAngle >= 0f)
        {
            currentAngle = 0f;
        }
        else
        {
            currentAngle = centerAngle;
        }
        
        SetupSail();
    }
    
    void SetupSail()
    {
        // Ustawienie pozycji zakotwiczenia - mapowanie Vector2(x,y) na Vector3(x,0,z)
        transform.localPosition = new Vector3(anchorPointLocal.x, 0, anchorPointLocal.y);
        
        // Ustaw początkową rotację żagla
        transform.localRotation = Quaternion.Euler(0, currentAngle, 0);
        
        // Stworzenie wizualizacji żagla - obrócony do płaszczyzny XZ
        GameObject sailVisual = new GameObject("SailVisual");
        sailVisual.transform.SetParent(transform);
        sailVisual.transform.localPosition = Vector3.zero;
        sailVisual.transform.localRotation = Quaternion.Euler(90, 0, 0); // Obrót sprite do XZ
        
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
        
        // Prostokąt z obramowaniem
        for (int y = 0; y < heightPx; y++)
        {
            for (int x = 0; x < widthPx; x++)
            {
                // Obramowanie
                if (x == 0 || x == widthPx - 1 || y == 0 || y == heightPx - 1)
                {
                    pixels[y * widthPx + x] = Color.black;
                }
                else
                {
                    pixels[y * widthPx + x] = color;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // Pivot u dołu (0.5, 0) - punkt zakotwiczenia na maszcie
        return Sprite.Create(texture, new Rect(0, 0, widthPx, heightPx), 
            new Vector2(0.5f, 0f), pixelsPerUnit);
    }
    
    // Ustawienie kąta żagla z ograniczeniami (obrót wokół osi Y)
    public void SetSailAngle(float angle)
    {
        // Bezpośrednio clamp i ustaw
        currentAngle = Mathf.Clamp(angle, minAngle, maxAngle);
        
        // Bezpośrednie ustawienie - Unity Quaternion radzi sobie z dowolnymi kątami
        transform.localRotation = Quaternion.Euler(0, currentAngle, 0);
        
        // Debug log dla testu
        if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.X))
        {
            Debug.Log($"Sail {name}: currentAngle={currentAngle:F1}°, localRotation.eulerAngles.y={transform.localRotation.eulerAngles.y:F1}°");
        }
    }
    
    // Płynna zmiana kąta żagla
    public void RotateSail(float deltaAngle)
    {
        SetSailAngle(currentAngle + deltaAngle);
    }
    
    // Obliczenie siły wiatru na żaglu - wersja Vector2
    public Vector2 CalculateWindForce(Vector2 windDirection, float windSpeed)
    {
        // Konwersja na Vector3 (XZ), obliczenie, konwersja z powrotem
        Vector3 windDir3D = new Vector3(windDirection.x, 0, windDirection.y);
        Vector3 force3D = CalculateWindForce3D(windDir3D, windSpeed);
        return new Vector2(force3D.x, force3D.z);
    }
    
    // Struktura zwracająca zarówno siłę jak i informacje o torque
    public struct SailForceResult
    {
        public Vector3 force;
        public float torqueMultiplier; // Mnożnik dla torque (0-1)
        public bool inDeadZone;
        public float angleOfAttack;
    }
    
    // Obliczenie siły wiatru na żaglu - pełna wersja z torque info
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
        
        // Kierunek żagla w przestrzeni świata (forward = chord line żagla w XZ)
        Vector3 sailChord = transform.forward;
        sailChord.y = 0;
        sailChord.Normalize();
        
        // Angle of Attack (α) - kąt między wiatrem a chord line żagla
        float angleOfAttack = Vector3.SignedAngle(windDirection, sailChord, Vector3.up);
        result.angleOfAttack = angleOfAttack;
        
        // === CHECK DEAD ZONE ===
        // Używamy WindSystem do określenia efektywności
        if (windSystem == null)
        {
            Debug.LogWarning("Sail: WindSystem not found, using default values");
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
        
        // Pobierz efektywność z WindSystem
        float deadZoneFactor = windSystem.GetEfficiencyMultiplier(yachtAngleToWind);
        WindSystem.PointOfSail currentPos = windSystem.GetPointOfSailForAngle(yachtAngleToWind);
        
        // inDeadZone = czy jesteśmy w kursie "Kąt martwy" (ostatni w liście)
        WindSystem.PointOfSail deadZoneCourse = windSystem.pointsOfSail[windSystem.pointsOfSail.Length - 1];
        result.inDeadZone = (currentPos.name == deadZoneCourse.name);
        
        // === CALCULATE SAIL AREA AND ASPECT RATIO ===
        float area = sailArea > 0 ? sailArea : sailLength * sailWidth;
        float AR = aspectRatio > 0 ? aspectRatio : (sailLength * sailLength) / area;
        
        // === CALCULATE DYNAMIC PRESSURE (raz dla wszystkich obliczeń) ===
        float airDensity = 1.225f; // kg/m³
        float dynamicPressure = 0.5f * airDensity * windSpeed * windSpeed;
        
        // Jeśli efektywność jest ujemna lub zero, specjalna obsługa
        if (deadZoneFactor <= 0f)
        {
            Vector3 yachtVelocity = Vector3.zero;
            if (transform.parent != null)
            {
                Rigidbody parentRb = transform.parent.GetComponent<Rigidbody>();
                if (parentRb != null)
                {
                    yachtVelocity = parentRb.linearVelocity;
                    yachtVelocity.y = 0; // Tylko XZ
                }
            }
            
            // Jeśli deadZoneFactor = 0, tylko hamowanie
            if (Mathf.Abs(deadZoneFactor) < 0.01f)
            {
                // Hamowanie - drag przeciwko ruchowi
                if (yachtVelocity.magnitude > 0.01f)
                {
                    Vector3 deadZoneDragDir = -yachtVelocity.normalized;
                    float deadZoneDragMag = 0.5f * dynamicPressure * area;
                    result.force = deadZoneDragDir * deadZoneDragMag * forceMultiplier;
                }
                else
                {
                    result.force = Vector3.zero;
                }
            }
            else
            {
                // Ujemna efektywność = siła cofająca!
                // Żagiel działa jak hamulec PLUS generuje siłę wsteczną
                
                float reverseFactor = Mathf.Abs(deadZoneFactor); // np. 0.5 dla -0.5
                
                // 1. Drag hamujący (przeciwko ruchowi)
                Vector3 dragForce = Vector3.zero;
                if (yachtVelocity.magnitude > 0.01f)
                {
                    Vector3 dragDir = -yachtVelocity.normalized;
                    float dragMag = reverseFactor * dynamicPressure * area;
                    dragForce = dragDir * dragMag * forceMultiplier;
                }
                
                // 2. Siła cofająca (przeciw wiatrowi)
                // Wiatr pcha żagiel do tyłu
                Vector3 reverseDir = -windDirection; // Przeciwny kierunek wiatru
                float reverseMag = reverseFactor * dynamicPressure * area * 0.5f; // 50% siły do tyłu
                Vector3 reverseForce = reverseDir * reverseMag * forceMultiplier;
                
                // Łączna siła = drag + cofanie
                result.force = dragForce + reverseForce;
            }
            
            result.torqueMultiplier = 0f; // Brak momentu obrotowego
            return result;
        }
        
        // === COEFFICIENTS OF LIFT AND DRAG ===
        // Używamy angle of attack żagla (nie kąta jachtu do wiatru)
        float absAngle = Mathf.Abs(angleOfAttack);
        float CL = 0f;
        
        if (absAngle < 15f)
        {
            CL = (maxCoefficientOfLift / 15f) * absAngle;
        }
        else if (absAngle < 30f)
        {
            float t = (absAngle - 15f) / 15f;
            CL = Mathf.Lerp(maxCoefficientOfLift, maxCoefficientOfLift * 0.53f, t);
        }
        else if (absAngle < 90f)
        {
            float t = (absAngle - 30f) / 60f;
            CL = Mathf.Lerp(maxCoefficientOfLift * 0.53f, maxCoefficientOfLift * 0.067f, t);
        }
        else
        {
            CL = maxCoefficientOfLift * 0.067f;
        }
        
        // Induced Drag
        float inducedDrag = (CL * CL) / (Mathf.PI * oswaldEfficiency * AR);
        
        // Parasitic Drag
        float parasiticDrag = 0f;
        
        if (absAngle < 15f)
        {
            parasiticDrag = 0.05f + 0.02f * (absAngle / 15f);
        }
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
        {
            parasiticDrag = maxCoefficientOfDrag;
        }
        
        float CD = inducedDrag + parasiticDrag;
        
        // === CALCULATE FORCES ===
        float liftMagnitude = CL * dynamicPressure * area;
        float aeroDragMagnitude = CD * dynamicPressure * area;
        
        // === DECOMPOSE FORCES TO WORLD SPACE ===
        Vector3 liftDirection = Vector3.Cross(windDirection, Vector3.up);
        
        if (angleOfAttack < 0)
        {
            liftDirection = -liftDirection;
        }
        
        Vector3 aeroDragDirection = windDirection;
        
        // Total Aerodynamic Force z dead zone factorem
        Vector3 totalForce = (liftDirection * liftMagnitude + aeroDragDirection * aeroDragMagnitude) * deadZoneFactor;
        
        result.force = totalForce * forceMultiplier;
        result.torqueMultiplier = deadZoneFactor; // Torque też skalowany przez dead zone
        
        return result;
    }
    
    // Obliczenie siły wiatru na żaglu - wersja Vector3 (dla płaszczyzny XZ)
    // Kompatybilność wsteczna - używa nowej metody
    public Vector3 CalculateWindForce3D(Vector3 windDirection, float windSpeed)
    {
        return CalculateWindForceWithTorque(windDirection, windSpeed).force;
    }
    
    // Pobierz obliczoną powierzchnię żagla
    public float GetSailArea()
    {
        return sailArea > 0 ? sailArea : sailLength * sailWidth;
    }
    
    // Pobierz obliczony aspect ratio
    public float GetAspectRatio()
    {
        float area = GetSailArea();
        return aspectRatio > 0 ? aspectRatio : (sailLength * sailLength) / area;
    }
    
    // Debug info - wyświetl aktualne współczynniki dla danego kąta
    public string GetDebugInfo(Vector3 windDirection, float windSpeed)
    {
        windDirection.y = 0;
        windDirection.Normalize();
        
        Vector3 sailChord = transform.forward;
        sailChord.y = 0;
        sailChord.Normalize();
        
        float angleOfAttack = Vector3.SignedAngle(windDirection, sailChord, Vector3.up);
        
        return $"Sail Debug:\n" +
               $"Area: {GetSailArea():F2} m²\n" +
               $"AR: {GetAspectRatio():F2}\n" +
               $"AoA: {angleOfAttack:F1}°\n" +
               $"Wind: {windSpeed:F1} m/s";
    }
    
    public float GetCurrentAngle()
    {
        return currentAngle;
    }
    
    // Pobierz nazwę kursu (point of sail) - używa WindSystem
    public static string GetPointOfSail(float yachtAngleToWind, out string tack, WindSystem windSystem)
    {
        if (windSystem != null)
        {
            return windSystem.GetPointOfSailName(yachtAngleToWind, out tack);
        }
        
        // Fallback jeśli brak WindSystem
        if (yachtAngleToWind < -1f)
        {
            tack = "Lewy hals";
        }
        else if (yachtAngleToWind > 1f)
        {
            tack = "Prawy hals";
        }
        else
        {
            tack = "";
        }
        
        return "Unknown";
    }
    
    public Vector2 GetSailDirection()
    {
        // Zwraca kierunek jako Vector2 (X,Z)
        Vector3 dir = transform.forward;
        return new Vector2(dir.x, dir.z);
    }
    
    public Vector3 GetSailDirection3D()
    {
        return transform.forward;
    }
    
    // Wizualizacja kierunku żagla (do debugowania)
}