using UnityEngine;
using System;

public class WindSystem : MonoBehaviour
{
    // Definicja kursu żeglarskiego
    [System.Serializable]
    public class PointOfSail
    {
        public string name;                  // Nazwa kursu (np. "Fordewind")
        [Range(0f, 180f)]
        public float minAngle;               // Minimalny kąt (od jachtu do wiatru)
        [Range(0f, 180f)]
        public float maxAngle;               // Maksymalny kąt
        [Range(-1f, 1f)]
        public float efficiencyMultiplier;   // Mnożnik efektywności (0 = brak siły, 1 = pełna)
        [Range(-2f, 2f)]
        public float speedMultiplier;        // Mnożnik prędkości (jak szybko jacht płynie)
        public Color debugColor;             // Kolor dla wizualizacji
    }
    
    [Header("Wind Parameters")]
    public Vector2 windDirection = Vector2.right; // 2D dla łatwości, mapowane na XZ
    public float windSpeed = 5f;
    public bool drawWindArrows = true;
    
    [Header("Wind Variation")]
    public bool enableVariation = false;
    public float variationSpeed = 0.5f;
    public float variationAmount = 30f; // w stopniach
    
    [Header("Points of Sail - Global Configuration")]
    public PointOfSail[] pointsOfSail = new PointOfSail[]
    {
        new PointOfSail 
        { 
            name = "Fordewind", 
            minAngle = 0f, 
            maxAngle = 20f, 
            efficiencyMultiplier = 0.8f,
            speedMultiplier = 1.5f,
            debugColor = new Color(0f, 1f, 0f, 0.5f) // Zielony
        },
        new PointOfSail 
        { 
            name = "Baksztag", 
            minAngle = 20f, 
            maxAngle = 80f, 
            efficiencyMultiplier = 1.0f,
            speedMultiplier = 1.8f,
            debugColor = new Color(0f, 1f, 1f, 0.5f) // Cyjan
        },
        new PointOfSail 
        { 
            name = "Półwiatr", 
            minAngle = 80f, 
            maxAngle = 110f, 
            efficiencyMultiplier = 0.9f,
            speedMultiplier = 1.4f,
            debugColor = new Color(0f, 0f, 1f, 0.5f) // Niebieski
        },
        new PointOfSail 
        { 
            name = "Bajdewind", 
            minAngle = 110f, 
            maxAngle = 150f, 
            efficiencyMultiplier = 0.7f,
            speedMultiplier = 1.0f,
            debugColor = new Color(1f, 1f, 0f, 0.5f) // Żółty
        },
        new PointOfSail 
        { 
            name = "Kąt martwy", 
            minAngle = 150f, 
            maxAngle = 180f, 
            efficiencyMultiplier = -0.5f,
            speedMultiplier = 0.0f,
            debugColor = new Color(1f, 0f, 0f, 0.5f) // Czerwony
        }
    };
    
    [Header("Dead Zone Transition")]
    [Tooltip("Szerokość strefy przejściowej przy wejściu/wyjściu z martwej strefy")]
    [Range(0f, 20f)]
    public float deadZoneFadeWidth = 10f;
    
    private float baseAngle;
    
    void Start()
    {
        windDirection.Normalize();
        // 0° = Z+ (północ), 90° = X+ (wschód)
        baseAngle = Mathf.Atan2(windDirection.x, windDirection.y) * Mathf.Rad2Deg;
    }
    
    void Update()
    {
        if (enableVariation)
        {
            float variation = Mathf.Sin(Time.time * variationSpeed) * variationAmount;
            float currentAngle = baseAngle + variation;
            // Sin/Cos zamienione miejscami - 0° wskazuje Z+
            windDirection = new Vector2(
                Mathf.Sin(currentAngle * Mathf.Deg2Rad),
                Mathf.Cos(currentAngle * Mathf.Deg2Rad)
            );
        }
        
        // Zmiana kierunku wiatru klawiszami (do testów)
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            RotateWind(-variationAmount * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            RotateWind(variationAmount * Time.deltaTime);
        }
        
        // Zmiana prędkości wiatru
        if (Input.GetKey(KeyCode.UpArrow))
        {
            windSpeed += variationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            windSpeed = Mathf.Max(0, windSpeed - variationSpeed * Time.deltaTime);
        }
    }
    
    void RotateWind(float angle)
    {
        // Atan2(x, y) zamiast Atan2(y, x) - 0° wskazuje Z+
        float currentAngle = Mathf.Atan2(windDirection.x, windDirection.y) * Mathf.Rad2Deg;
        currentAngle += angle;
        baseAngle = currentAngle;
        // Sin/Cos zamienione miejscami
        windDirection = new Vector2(
            Mathf.Sin(currentAngle * Mathf.Deg2Rad),
            Mathf.Cos(currentAngle * Mathf.Deg2Rad)
        );
    }
    
    public Vector2 GetWindDirection()
    {
        return windDirection; // Zwraca Vector2 (x, y) które mapuje się na (X, Z) w 3D
    }
    
    public float GetWindSpeed()
    {
        return windSpeed;
    }
    
    // Pobierz kurs na podstawie kąta jachtu względem wiatru
    public PointOfSail GetPointOfSailForAngle(float yachtAngleToWind)
    {
        float absAngle = Mathf.Abs(yachtAngleToWind);
        
        foreach (PointOfSail pos in pointsOfSail)
        {
            if (absAngle >= pos.minAngle && absAngle < pos.maxAngle)
            {
                return pos;
            }
        }
        
        // Domyślnie ostatni (Kąt martwy)
        return pointsOfSail[pointsOfSail.Length - 1];
    }
    
    // Pobierz nazwę kursu i hals
    public string GetPointOfSailName(float yachtAngleToWind, out string tack)
    {
        // Określ hals
        if (yachtAngleToWind < -1f)
        {
            tack = "Lewy hals"; // Port tack
        }
        else if (yachtAngleToWind > 1f)
        {
            tack = "Prawy hals"; // Starboard tack
        }
        else
        {
            tack = ""; // Prawie 0°
        }
        
        return GetPointOfSailForAngle(yachtAngleToWind).name;
    }
    
    // Oblicz mnożnik efektywności dla danego kąta
    public float GetEfficiencyMultiplier(float yachtAngleToWind)
    {
        float absAngle = Mathf.Abs(yachtAngleToWind);
        PointOfSail currentPos = GetPointOfSailForAngle(yachtAngleToWind);
        
        // Użyj efektywności bezpośrednio z kursu
        float efficiency = currentPos.efficiencyMultiplier;
        
        // Sprawdź czy jesteśmy w strefie fade (przejście między kursami)
        // Fade tylko na granicy dead zone
        PointOfSail deadZone = pointsOfSail[pointsOfSail.Length - 1];
        float deadZoneStart = deadZone.minAngle;
        
        // Znajdź poprzedni kurs (przed dead zone)
        PointOfSail prevPos = pointsOfSail.Length > 1 ? pointsOfSail[pointsOfSail.Length - 2] : currentPos;
        
        if (absAngle >= deadZoneStart && absAngle < deadZoneStart + deadZoneFadeWidth)
        {
            // W strefie fade między ostatnim kursem a dead zone
            float fadeProgress = (absAngle - deadZoneStart) / deadZoneFadeWidth;
            efficiency = Mathf.Lerp(prevPos.efficiencyMultiplier, deadZone.efficiencyMultiplier, fadeProgress);
        }
        
        return efficiency;
    }
    
    // Pobierz kąt martwego (dla kompatybilności wstecznej)
    public float GetDeadZoneAngle()
    {
        return pointsOfSail[pointsOfSail.Length - 1].minAngle;
    }
    
    public float GetDeadZoneFadeWidth()
    {
        return deadZoneFadeWidth;
    }
    
    // Rysowanie strzałek wiatru w scenie - PŁASZCZYZNA XZ (poziomo)
    void OnDrawGizmos()
    {
        if (!drawWindArrows) return;
        
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.7f);
        
        // Siatka strzałek wiatru w płaszczyźnie XZ (Y=0)
        int gridSize = 10;
        float spacing = 2f;
        
        // Konwersja Vector2 (x, y) na Vector3 (x, 0, z) dla płaszczyzny XZ
        Vector3 windDir3D = new Vector3(windDirection.x, 0, windDirection.y);
        
        for (int x = -gridSize; x <= gridSize; x++)
        {
            for (int z = -gridSize; z <= gridSize; z++)
            {
                // Pozycja w płaszczyźnie XZ (Y=0 dla widoku z góry)
                Vector3 pos = new Vector3(x * spacing, 0, z * spacing);
                Vector3 arrowEnd = pos + windDir3D * windSpeed * 0.2f;
                
                // Strzałka
                Gizmos.DrawLine(pos, arrowEnd);
                
                // Główka strzałki - perpendicular w płaszczyźnie XZ
                Vector3 perpendicular = new Vector3(-windDir3D.z, 0, windDir3D.x) * 0.1f;
                Gizmos.DrawLine(arrowEnd, arrowEnd - windDir3D * 0.15f + perpendicular);
                Gizmos.DrawLine(arrowEnd, arrowEnd - windDir3D * 0.15f - perpendicular);
            }
        }
    }
    
    // Wizualizacja GUI
    // void OnGUI()
    // {
    //     GUIStyle style = new GUIStyle(GUI.skin.box);
    //     style.fontSize = 14;
    //     style.alignment = TextAnchor.UpperLeft;
    //     
    //     // Atan2(x, y) - 0° wskazuje Z+ (północ)
    //     float angle = Mathf.Atan2(windDirection.x, windDirection.y) * Mathf.Rad2Deg;
    //     
    //     string info = $"Wind System\n" +
    //                   $"Direction: {angle:F1}°\n" +
    //                   $"Speed: {windSpeed:F1} m/s\n" +
    //                   $"Controls:\n" +
    //                   $"← → : Rotate wind\n" +
    //                   $"↑ ↓ : Change speed";
    //     
    //     GUI.Box(new Rect(10, 10, 200, 130), info, style);
    // }
}