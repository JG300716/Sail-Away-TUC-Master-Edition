using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using Random = UnityEngine.Random;

namespace Game.Scripts
{
    public class Lightning : MonoBehaviour
    {
        [Header("Lightning Settings")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float lightningHeight = 30f;
        [SerializeField] private float groundLevel = 0f;

        [Header("Generation Settings")]
        [SerializeField, Range(1, 8)] private int generations = 4;
        [SerializeField, Range(1, 10)] private int boltCount = 4;
        [SerializeField, Range(0.1f, 5f)] private float duration = 1.49f;
        
        [Header("Distance Settings")]
        [SerializeField] private float minDistance = 400f;
        [SerializeField] private float maxDistance = 600f;
        [SerializeField] private float startPointSpread = 5f;
        [SerializeField] private float endPointSpread = 10f;
        
        [Header("Angle Settings")]
        [SerializeField, Range(0f, 180f)] private float horizontalAngleRange = 90f;
        [SerializeField, Range(0f, 90f)] private float verticalAngleDeviation = 15f;
        
        [Header("Bolt Shape")]
        [SerializeField, Range(0f, 1f)] private float chaosFactor = 0.2f;
        [SerializeField] private float trunkWidth = 5.8f;
        [SerializeField, Range(0f, 1f)] private float forkedness = 0.27f;
        [SerializeField] private float segmentDensity = 2f; // Punktów na jednostkę odległości
        [SerializeField, Range(2, 50)] private int minSegments = 8;
        [SerializeField, Range(10, 200)] private int maxSegments = 100;
        
        [Header("Fork Settings")]
        [SerializeField, Range(0f, 90f)] private float minForkAngle = 20f;
        [SerializeField, Range(0f, 90f)] private float maxForkAngle = 60f;
        [SerializeField, Range(0f, 1f)] private float minForkLength = 0.3f;
        [SerializeField, Range(0f, 1f)] private float maxForkLength = 0.7f;
        [SerializeField, Range(0f, 1f)] private float forkWidthMultiplier = 0.6f;
        [SerializeField, Range(1, 5)] private int minForksPerBolt = 1;
        [SerializeField, Range(1, 8)] private int maxForksPerBolt = 3;
        [SerializeField, Range(0f, 1f)] private float forkStartPosition = 0.33f; // Gdzie mogą zaczynać się forki (0-1)
        [SerializeField, Range(0f, 1f)] private float forkEndPosition = 0.67f;
        
        [Header("Chaos Settings")]
        [SerializeField, Range(0f, 2f)] private float chaosDistanceMultiplier = 0.1f;
        [SerializeField, Range(0f, 1f)] private float midPointChaosBoost = 1f; // Multiplier dla środka pioruna
        [SerializeField, Range(0f, 1f)] private float verticalChaosMultiplier = 0.5f;
        
        [Header("Visual Settings")]
        [SerializeField] private float glowIntensity = 0.1f;
        [SerializeField] private float glowWidthMultiplier = 4.0f;
        [SerializeField] private Material lightningMaterial;
        [SerializeField] private Material glowMaterial;
        [SerializeField] private Color lightningStartColor = Color.white;
        [SerializeField] private Color lightningEndColor = Color.white;
        [SerializeField] private Color glowStartColor = new Color(1f, 1f, 1f, 0.1f);
        [SerializeField] private Color glowEndColor = new Color(1f, 1f, 1f, 0.05f);

        [Header("Debug")] 
        public bool generateLightning = false;

        private LineRenderer mainLineRenderer;
        private List<LineRenderer> allBolts = new();
        private List<GameObject> allBoltObjects = new();

        private void Update()
        {
            if (!generateLightning) return;

            ClearOldBolts();
            GenerateProceduralLightning();
            Invoke(nameof(DestroyBolt), duration);
            generateLightning = false;
        }

        public void Initialize(Camera camera = null)
        {
            if (!camera.IsUnityNull()) playerCamera = camera;
            if (playerCamera.IsUnityNull()) playerCamera = Camera.main;

            mainLineRenderer = GetComponent<LineRenderer>();
            if (mainLineRenderer.IsUnityNull()) 
                mainLineRenderer = gameObject.AddComponent<LineRenderer>();

            if (!lightningMaterial.IsUnityNull()) 
                mainLineRenderer.material = lightningMaterial;
        }

        public void Generate(Camera camera = null)
        {
            if (mainLineRenderer.IsUnityNull()) Initialize(camera);

            ClearOldBolts();
            GenerateProceduralLightning();
            Invoke(nameof(DestroyBolt), duration);
        }

        void GenerateProceduralLightning()
        {
            if (playerCamera.IsUnityNull()) return;

            // Wyznacz bazowy kierunek
            Vector3 cameraForward = playerCamera.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            // Losowy kąt w poziomie
            float horizontalAngle = Random.Range(-horizontalAngleRange, horizontalAngleRange);
            Vector3 direction = Quaternion.Euler(0, horizontalAngle, 0) * cameraForward;

            // Losowa odległość
            float distance = Random.Range(minDistance, maxDistance);

            // Punkt bazowy
            Vector3 horizonPoint = playerCamera.transform.position + direction * distance;
            
            // Punkt startowy z lekkim odchyleniem wertykalnym
            float verticalDeviation = Random.Range(-verticalAngleDeviation, verticalAngleDeviation);
            Vector3 startDirection = Quaternion.Euler(verticalDeviation, 0, 0) * Vector3.down;
            Vector3 baseStartPoint = new Vector3(horizonPoint.x, lightningHeight, horizonPoint.z);

            // Generuj wiele piorunów (bolt count)
            for (int boltIndex = 0; boltIndex < boltCount; boltIndex++)
            {
                // Wariacja punktu startowego
                Vector3 boltStart = baseStartPoint + new Vector3(
                    Random.Range(-startPointSpread, startPointSpread),
                    Random.Range(-startPointSpread * 0.5f, startPointSpread * 0.5f),
                    Random.Range(-startPointSpread, startPointSpread)
                );

                // Wariacja punktu końcowego
                Vector3 boltEnd = new Vector3(
                    horizonPoint.x + Random.Range(-endPointSpread, endPointSpread),
                    groundLevel,
                    horizonPoint.z + Random.Range(-endPointSpread, endPointSpread)
                );

                // Zastosuj odchylenie wertykalne
                Vector3 targetDirection = (boltEnd - boltStart).normalized;
                float adjustedDistance = Vector3.Distance(boltStart, boltEnd);
                boltEnd = boltStart + Quaternion.Euler(verticalDeviation * 0.5f, 0, 0) * targetDirection * adjustedDistance;
                boltEnd.y = Mathf.Max(boltEnd.y, groundLevel);

                GenerateBoltWithGenerations(boltStart, boltEnd, generations, trunkWidth, 0);
            }
        }

        void GenerateBoltWithGenerations(Vector3 start, Vector3 end, int generationsLeft, float width, int depth)
        {
            if (generationsLeft <= 0) return;

            // Twórz GameObject dla tego pioruna
            GameObject boltObj = new GameObject($"Bolt_Gen{generations - generationsLeft}_Depth{depth}");
            boltObj.transform.parent = transform;
            allBoltObjects.Add(boltObj);

            // Główny LineRenderer (jasny piorun)
            LineRenderer mainLR = boltObj.AddComponent<LineRenderer>();
            mainLR.widthMultiplier = width;
            mainLR.startColor = lightningStartColor;
            mainLR.endColor = lightningEndColor;
            if (!lightningMaterial.IsUnityNull()) 
                mainLR.material = lightningMaterial;
            allBolts.Add(mainLR);

            // Glow LineRenderer (poświata)
            GameObject glowObj = new GameObject("Glow");
            glowObj.transform.parent = boltObj.transform;
            LineRenderer glowLR = glowObj.AddComponent<LineRenderer>();
            glowLR.widthMultiplier = width * glowWidthMultiplier;
            glowLR.startColor = glowStartColor;
            glowLR.endColor = glowEndColor;
            if (!glowMaterial.IsUnityNull())
                glowLR.material = glowMaterial;
            else if (!lightningMaterial.IsUnityNull())
                glowLR.material = lightningMaterial;
            
            allBolts.Add(glowLR);

            // Oblicz liczbę segmentów na podstawie odległości
            float distance = Vector3.Distance(start, end);
            int segments = Mathf.RoundToInt(distance * segmentDensity);
            segments = Mathf.Clamp(segments, minSegments, maxSegments);

            // Generuj punkty głównego pioruna
            Vector3[] points = GenerateBoltPoints(start, end, segments);
            
            mainLR.positionCount = points.Length;
            mainLR.SetPositions(points);
            
            glowLR.positionCount = points.Length;
            glowLR.SetPositions(points);

            // Rekurencyjnie generuj rozgałęzienia (fork)
            if (generationsLeft > 1 && Random.value < forkedness)
            {
                int forkCount = Random.Range(minForksPerBolt, maxForksPerBolt + 1);
                
                for (int i = 0; i < forkCount; i++)
                {
                    // Wybierz punkt rozgałęzienia w określonym zakresie
                    int forkStartIndex = Mathf.RoundToInt(segments * forkStartPosition);
                    int forkEndIndex = Mathf.RoundToInt(segments * forkEndPosition);
                    int forkIndex = Random.Range(forkStartIndex, forkEndIndex);
                    
                    if (forkIndex >= points.Length) forkIndex = points.Length / 2;
                    
                    Vector3 forkStart = points[forkIndex];
                    
                    // Oblicz nowy punkt końcowy dla rozgałęzienia
                    Vector3 mainDirection = (end - start).normalized;
                    float forkAngle = Random.Range(minForkAngle, maxForkAngle);
                    float leftOrRight = Random.value > 0.5f ? 1f : -1f;
                    
                    // Rotacja wokół osi pionowej i dodatkowa wokół kierunku ruchu
                    Quaternion forkRotation = Quaternion.AngleAxis(forkAngle * leftOrRight, Vector3.up);
                    forkRotation *= Quaternion.AngleAxis(Random.Range(-15f, 15f), mainDirection);
                    
                    Vector3 forkDirection = forkRotation * mainDirection;
                    float forkLength = distance * Random.Range(minForkLength, maxForkLength);
                    
                    Vector3 forkEnd = forkStart + forkDirection * forkLength;
                    
                    // Upewnij się że nie jest pod ziemią
                    if (forkEnd.y < groundLevel) 
                        forkEnd.y = groundLevel + Random.Range(0f, 5f);
                    
                    // Zmniejsz grubość dla rozgałęzień
                    float widthVariation = Random.Range(0.8f, 1.2f);
                    float newWidth = width * forkWidthMultiplier * widthVariation;
                    
                    // Rekurencyjnie generuj rozgałęzienie
                    GenerateBoltWithGenerations(forkStart, forkEnd, generationsLeft - 1, newWidth, depth + 1);
                }
            }
        }

        Vector3[] GenerateBoltPoints(Vector3 start, Vector3 end, int segments)
        {
            Vector3[] points = new Vector3[segments];
            Vector3 mainDirection = (end - start).normalized;
            float totalDistance = Vector3.Distance(start, end);

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                
                // Podstawowa interpolacja
                Vector3 basePosition = Vector3.Lerp(start, end, t);
                
                // Dodaj chaos - więcej na środku, mniej na końcach
                float chaosAmount = chaosFactor * totalDistance * chaosDistanceMultiplier;
                
                // Boost chaos w środku pioruna
                float midPointMultiplier = 1f - Mathf.Abs(t * 2f - 1f); // 0 na końcach, 1 w środku
                chaosAmount *= Mathf.Lerp(1f, midPointChaosBoost, midPointMultiplier);
                
                // Perpendicular vectors dla offsetu
                Vector3 perpendicular1 = Vector3.Cross(mainDirection, Vector3.up).normalized;
                Vector3 perpendicular2 = Vector3.Cross(mainDirection, perpendicular1).normalized;
                
                Vector3 offset = perpendicular1 * Random.Range(-chaosAmount, chaosAmount) +
                                perpendicular2 * Random.Range(-chaosAmount, chaosAmount);
                
                // Dodaj zakłócenia w kierunku pionowym (mniejsze)
                offset.y += Random.Range(-chaosAmount * verticalChaosMultiplier, chaosAmount * verticalChaosMultiplier);
                
                points[i] = basePosition + offset;
            }

            return points;
        }

        void ClearOldBolts()
        {
            foreach (var bolt in allBolts)
            {
                if (!bolt.IsUnityNull()) 
                    Destroy(bolt.gameObject);
            }
            allBolts.Clear();

            foreach (var obj in allBoltObjects)
            {
                if (!obj.IsUnityNull()) 
                    Destroy(obj);
            }
            allBoltObjects.Clear();
        }

        void DestroyBolt()
        {
            ClearOldBolts();
            Destroy(gameObject);
        }

        private void OnDrawGizmos()
        {
            if (playerCamera == null) return;

            Vector3 cameraPos = playerCamera.transform.position;
            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0;
            forward.Normalize();

            // Wizualizacja zakresu kątowego
            Gizmos.color = Color.yellow;
            Vector3 leftDir = Quaternion.Euler(0, -horizontalAngleRange, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, horizontalAngleRange, 0) * forward;
            
            float avgDistance = (minDistance + maxDistance) / 2f;
            
            Gizmos.DrawLine(cameraPos, cameraPos + forward * avgDistance);
            Gizmos.DrawLine(cameraPos, cameraPos + leftDir * avgDistance);
            Gizmos.DrawLine(cameraPos, cameraPos + rightDir * avgDistance);

            // Wizualizacja obszaru spawnu
            Gizmos.color = Color.cyan;
            Vector3 centerPoint = cameraPos + forward * avgDistance;
            Gizmos.DrawWireSphere(new Vector3(centerPoint.x, lightningHeight, centerPoint.z), startPointSpread);
            Gizmos.DrawWireSphere(new Vector3(centerPoint.x, groundLevel, centerPoint.z), endPointSpread);
        }
    }
}