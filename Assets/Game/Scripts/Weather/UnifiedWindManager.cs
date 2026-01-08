using UnityEngine;
using System;
using Game.Scripts.Interface;

public class UnifiedWindManager : SingletonInterface<UnifiedWindManager>
{
    [System.Serializable]
    public class PointOfSail
    {
        public string name;                  // Nazwa kursu (np. "Fordewind")
        [Range(0f, 180f)]
        public float minAngle;               // Minimalny kąt (od jachtu do wiatru)
        [Range(0f, 180f)]
        public float maxAngle;               // Maksymalny kąt
        [Range(-2f, 2f)]
        public float efficiencyMultiplier;   // Mnożnik efektywności (ujemne = cofanie!)
        [Range(-2f, 2f)]
        public float speedMultiplier;        // Mnożnik prędkości (jak szybko jacht płynie)
        public Color debugColor;             // Kolor dla wizualizacji
    }
    
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
            efficiencyMultiplier = -0.5f, // Ujemne = hamowanie + cofanie!
            speedMultiplier = 0.0f,
            debugColor = new Color(1f, 0f, 0f, 0.5f) // Czerwony
        }
    };
    
    [Header("Dead Zone Transition")]
    [Tooltip("Szerokość strefy przejściowej przy wejściu/wyjściu z martwej strefy")]
    [Range(0f, 20f)]
    public float deadZoneFadeWidth = 10f;
    
    [Header("Wind Speed Settings")]
    [SerializeField] public float minWindSpeed = 0.0f;
    [SerializeField] public float maxWindSpeed = 15.0f;
    [SerializeField] public double WindSpeed = 5.0; // Aktualna prędkość [m/s]
    [SerializeField] private double TargetWindSpeed;
    
    [Header("Wind Direction Settings")]
    [SerializeField] public float minWindDirectionDeg = 0.0f;
    [SerializeField] public float maxWindDirectionDeg = 360.0f;
    [SerializeField] public double WindDegree; // Aktualny kąt względem północy [°]
    [SerializeField] private double TargetWindDegree;
    
    [Header("Steady Wind Settings")]
    [SerializeField] public double steadyWindChangeInterval = 1.0;
    [SerializeField] public double windSpeedStep = 0.3;
    [SerializeField] public double windDirectionStep = 1.0;
    
    private double timeSinceLastChange = 0.0;
    public bool GenerateSteadyWindFlag = true;
    private bool UpdateSteadyWind = false;
    
    [Header("Manual Controls (for testing)")]
    public bool enableManualControls = true;
    public float manualRotationSpeed = 30f; // stopni/sekundę
    public float manualSpeedChange = 2f; // m/s na sekundę
    
    void Start()
    {
        // Domyślna prędkość jeśli nie ustawiona
        if (WindSpeed == 0)
        {
            WindSpeed = 5.0;
        }
        
        // Uruchom generowanie wiatru jeśli flaga włączona
        if (GenerateSteadyWindFlag)
        {
            GenerateSteadyWind(0);
        }
    }
    
    void Update()
    {
        // Steady wind generation
        if (GenerateSteadyWindFlag)
        {
            GenerateSteadyWind(Time.deltaTime);
        }
        
        if (UpdateSteadyWind)
        {
            UpdateSteadyWindConditions(Time.deltaTime);
        }
        
        // Manual controls (for testing)
        if (enableManualControls)
        {
            HandleManualControls();
        }
    }
    
    void HandleManualControls()
    {
        // Zmiana kierunku wiatru klawiszami
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            WindDegree -= manualRotationSpeed * Time.deltaTime;
            WindDegree = Mathf.Repeat((float)WindDegree, 360f);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            WindDegree += manualRotationSpeed * Time.deltaTime;
            WindDegree = Mathf.Repeat((float)WindDegree, 360f);
        }
        
        // Zmiana prędkości wiatru
        if (Input.GetKey(KeyCode.UpArrow))
        {
            WindSpeed = Mathf.Clamp((float)(WindSpeed + manualSpeedChange * Time.deltaTime), minWindSpeed, maxWindSpeed);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            WindSpeed = Mathf.Clamp((float)(WindSpeed - manualSpeedChange * Time.deltaTime), minWindSpeed, maxWindSpeed);
        }
    }
    
    public Vector2 GetWindDirection()
    {
        float angleRad = (float)WindDegree * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));
    }
    
    public Vector3 GetWindDirection3D()
    {
        float angleRad = (float)WindDegree * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad));
    }
    
    public float GetWindSpeed()
    {
        return (float)WindSpeed;
    }
    
    public Vector3 GetWindForce()
    {
        return GetWindDirection3D() * (float)WindSpeed;
    }
    
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
    
    public float GetEfficiencyMultiplier(float yachtAngleToWind)
    {
        float absAngle = Mathf.Abs(yachtAngleToWind);
        PointOfSail currentPos = GetPointOfSailForAngle(yachtAngleToWind);
        
        // Użyj efektywności bezpośrednio z kursu
        float efficiency = currentPos.efficiencyMultiplier;
        
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
    
    public float GetDeadZoneAngle()
    {
        return pointsOfSail[pointsOfSail.Length - 1].minAngle;
    }
    
    public float GetDeadZoneFadeWidth()
    {
        return deadZoneFadeWidth;
    }
    
    public void SetWindConditions(double windSpeed, double windDirectionDeg)
    {
        WindSpeed = Mathf.Clamp((float)windSpeed, minWindSpeed, maxWindSpeed);
        WindDegree = Mathf.Repeat((float)windDirectionDeg, 360f);
    }
    
    public void GenerateRandomWind()
    {
        WindSpeed = UnityEngine.Random.Range(minWindSpeed, maxWindSpeed);
        WindDegree = UnityEngine.Random.Range(minWindDirectionDeg, maxWindDirectionDeg);
    }
    public void GenerateSteadyWind(double deltaTime)
    {
        // Losowy cel wiatru
        TargetWindSpeed = UnityEngine.Random.Range(minWindSpeed, maxWindSpeed);
        TargetWindDegree = UnityEngine.Random.Range(minWindDirectionDeg, maxWindDirectionDeg);
        
        timeSinceLastChange = 0.0;
        GenerateSteadyWindFlag = false;
        UpdateSteadyWind = true;
    }
    
    public void ResetWind()
    {
        GenerateRandomWind();
        timeSinceLastChange = 0.0;
    }
    
    public void UpdateSteadyWindConditions(double deltaTime)
    {
        timeSinceLastChange += deltaTime;
        
        if (timeSinceLastChange < steadyWindChangeInterval)
        {
            return;
        }
        
        // Stopniowe przybliżanie do celu
        WindSpeed = MoveTowards(WindSpeed, TargetWindSpeed, windSpeedStep);
        WindDegree = MoveTowardsAngle(WindDegree, TargetWindDegree, windDirectionStep);
        
        timeSinceLastChange = 0.0;
        
        // Sprawdź czy osiągnięto cel
        bool speedReached = Mathf.Approximately((float)WindSpeed, (float)TargetWindSpeed);
        bool directionReached = Mathf.Approximately((float)WindDegree, (float)TargetWindDegree);
        
        UpdateSteadyWind = !(speedReached && directionReached);
        
        // Jeśli osiągnięto cel, wygeneruj nowy
        if (!UpdateSteadyWind)
        {
            GenerateSteadyWindFlag = true;
        }
    }
    
    private double MoveTowards(double current, double target, double maxDelta)
    {
        if (current < target)
        {
            return Mathf.Min((float)(current + maxDelta), (float)target);
        }
        else
        {
            return Mathf.Max((float)(current - maxDelta), (float)target);
        }
    }
    
    private double MoveTowardsAngle(double current, double target, double maxDelta)
    {
        float delta = Mathf.DeltaAngle((float)current, (float)target);
        
        if (Mathf.Abs(delta) <= maxDelta)
        {
            return target;
        }
        
        return Mathf.Repeat((float)(current + Mathf.Sign(delta) * maxDelta), 360f);
    }
    
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        style.alignment = TextAnchor.UpperLeft;
        
        string info = $"Wind System\n" +
                      $"Direction: {WindDegree:F1}°\n" +
                      $"Speed: {WindSpeed:F1} m/s\n" +
                      $"Target Speed: {TargetWindSpeed:F1} m/s\n" +
                      $"Target Dir: {TargetWindDegree:F1}°\n";
        
        if (enableManualControls)
        {
            info += $"\nManual Controls:\n" +
                    $"← → : Rotate wind\n" +
                    $"↑ ↓ : Change speed";
        }
        
        GUI.Box(new Rect(10, 10, 220, 150), info, style);
    }
}