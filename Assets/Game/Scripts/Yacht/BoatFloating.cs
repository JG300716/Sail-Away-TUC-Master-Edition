using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Scripts
{
    [ExecuteInEditMode]
    public class BoatFloatingAdvanced : MonoBehaviour
    {
        [Header("Water Reference")]
        [SerializeField] public WaterSurface waterSurface;
        
        [Header("Floating Settings")]
        [SerializeField] private float floatStrength = 15f;
        [SerializeField] private float waterDrag = 0.99f;
        [SerializeField] private float angularDrag = 0.5f;
        [SerializeField] private float normalInfluence = 2f; // Wpływ normalnej na siłę
        
        [Header("Float Points")]
        [SerializeField] private Transform[] floatPoints;
        
        private Rigidbody rb;
        private WaterSearchParameters searchParameters;
        private WaterSearchResult searchResult;

        [Header("Gizmo Settings")]
        [SerializeField] private bool showMesh = true;
        [SerializeField] private bool showPoints = true;
        [SerializeField] private bool showWireframe = true;
        [SerializeField] private Color meshColor = new Color(0f, 1f, 1f, 0.3f);
        [SerializeField] private Color wireframeColor = Color.cyan;
        [SerializeField] private Color pointColor = Color.yellow;
        [SerializeField] private float pointSize = 0.15f;
        
        void Start()
        {
            rb = GetComponent<Rigidbody>();
            
            if (rb == null)
            {
                Debug.LogError("Brak Rigidbody!");
                return;
            }
            
            if (waterSurface == null)
            {
                Debug.LogError("Nie przypisano HDRP Water Surface!");
                return;
            }
            
            rb.linearDamping = waterDrag;
            rb.angularDamping = angularDrag;
            
            // Inicjalizacja parametrów wyszukiwania
            searchParameters = new WaterSearchParameters();
            searchResult = new WaterSearchResult();
        }

        void FixedUpdate()
        {
            if (rb == null || waterSurface == null || floatPoints == null)
                return;
            
            foreach (Transform floatPoint in floatPoints)
            {
                if (floatPoint == null)
                    continue;
                
                // Konfiguracja wyszukiwania
                searchParameters.startPositionWS = floatPoint.position + Vector3.up * 10f; // Zacznij powyżej
                searchParameters.targetPositionWS = floatPoint.position;
                searchParameters.error = 0.01f;
                searchParameters.maxIterations = 8;
                
                // Pobierz informacje o powierzchni wody
                if (waterSurface.ProjectPointOnWaterSurface(searchParameters, out searchResult))
                {
                    Vector3 waterPosition = searchResult.projectedPositionWS;
                    Vector3 waterNormal = searchResult.normalWS;
                    
                    // Sprawdź zanurzenie
                    if (floatPoint.position.y < waterPosition.y)
                    {
                        float depth = waterPosition.y - floatPoint.position.y;
                        
                        // Siła wyporu kierowana zgodnie z normalną powierzchni
                        Vector3 buoyancyDirection = Vector3.Lerp(Vector3.up, waterNormal, normalInfluence * 0.1f);
                        Vector3 floatForce = buoyancyDirection.normalized * (depth * floatStrength);
                        rb.AddForceAtPosition(floatForce, floatPoint.position, ForceMode.Force);
                        
                        // Opór wody
                        rb.AddForceAtPosition(waterDrag * 0.1f * -rb.GetPointVelocity(floatPoint.position), floatPoint.position, ForceMode.Force);                    }
                }
            }
        }
        
        #region Gizmos
            void OnDrawGizmos()
            {
                if (floatPoints == null || floatPoints.Length < 3)
                    return;
                
                // Rysuj punkty
                if (showPoints)
                {
                    Gizmos.color = pointColor;
                    for (int i = 0; i < floatPoints.Length; i++)
                    {
                        if (floatPoints[i] != null)
                        {
                            Gizmos.DrawSphere(floatPoints[i].position, pointSize);
                            
                            // Opcjonalnie: numery punktów
                            #if UNITY_EDITOR
                            UnityEditor.Handles.Label(floatPoints[i].position + Vector3.up * 0.3f, i.ToString());
                            #endif
                        }
                    }
                }
                
                // Rysuj siatkę
                if (showMesh || showWireframe)
                {
                    DrawMesh();
                }
            }
            
            void DrawMesh()
            {
                // Usuń nulle
                var validPoints = System.Array.FindAll(floatPoints, p => p != null);
                if (validPoints.Length < 3)
                    return;
                
                // Sortuj punkty według pozycji (opcjonalnie dla lepszego układu)
                Vector3[] positions = new Vector3[validPoints.Length];
                for (int i = 0; i < validPoints.Length; i++)
                {
                    positions[i] = validPoints[i].position;
                }
                
                // Rysuj trójkąty (triangulacja prosta)
                if (validPoints.Length >= 4)
                {
                    // Dla prostokątnej siatki (zakładając 4+ punkty)
                    DrawQuadMesh(positions);
                }
                else if (validPoints.Length == 3)
                {
                    // Pojedynczy trójkąt
                    if (showMesh)
                    {
                        Gizmos.color = meshColor;
                        DrawTriangle(positions[0], positions[1], positions[2]);
                    }
                    
                    if (showWireframe)
                    {
                        Gizmos.color = wireframeColor;
                        Gizmos.DrawLine(positions[0], positions[1]);
                        Gizmos.DrawLine(positions[1], positions[2]);
                        Gizmos.DrawLine(positions[2], positions[0]);
                    }
                }
            }
            
            void DrawQuadMesh(Vector3[] positions)
            {
                // Sortuj punkty do siatki (najprostszy sposób - po X i Z)
                var sortedPoints = new System.Collections.Generic.List<Vector3>(positions);
                sortedPoints.Sort((a, b) => {
                    int zComp = a.z.CompareTo(b.z);
                    return zComp != 0 ? zComp : a.x.CompareTo(b.x);
                });
                
                // Wykryj wymiary siatki
                int cols = Mathf.CeilToInt(Mathf.Sqrt(sortedPoints.Count));
                int rows = Mathf.CeilToInt((float)sortedPoints.Count / cols);
                
                // Rysuj quady
                for (int row = 0; row < rows - 1; row++)
                {
                    for (int col = 0; col < cols - 1; col++)
                    {
                        int i0 = row * cols + col;
                        int i1 = row * cols + col + 1;
                        int i2 = (row + 1) * cols + col;
                        int i3 = (row + 1) * cols + col + 1;
                        
                        if (i0 >= sortedPoints.Count || i1 >= sortedPoints.Count || 
                            i2 >= sortedPoints.Count || i3 >= sortedPoints.Count)
                            continue;
                        
                        Vector3 p0 = sortedPoints[i0];
                        Vector3 p1 = sortedPoints[i1];
                        Vector3 p2 = sortedPoints[i2];
                        Vector3 p3 = sortedPoints[i3];
                        
                        // Rysuj wypełnienie
                        if (showMesh)
                        {
                            Gizmos.color = meshColor;
                            DrawTriangle(p0, p1, p2);
                            DrawTriangle(p1, p3, p2);
                        }
                        
                        // Rysuj wireframe
                        if (showWireframe)
                        {
                            Gizmos.color = wireframeColor;
                            Gizmos.DrawLine(p0, p1);
                            Gizmos.DrawLine(p0, p2);
                            Gizmos.DrawLine(p1, p3);
                            Gizmos.DrawLine(p2, p3);
                        }
                    }
                }
            }
            
            void DrawTriangle(Vector3 p0, Vector3 p1, Vector3 p2)
            {
                // Rysuj trójkąt jako małe linie (Gizmos nie ma bezpośredniego DrawTriangle)
                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p0);
                
                // Opcjonalnie: wypełnienie (symulowane liniami)
                Vector3 center = (p0 + p1 + p2) / 3f;
                int steps = 5;
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Vector3 edge01 = Vector3.Lerp(p0, p1, t);
                    Vector3 edge12 = Vector3.Lerp(p1, p2, t);
                    Vector3 edge20 = Vector3.Lerp(p2, p0, t);
                    
                    Gizmos.DrawLine(edge01, center);
                    Gizmos.DrawLine(edge12, center);
                    Gizmos.DrawLine(edge20, center);
                }
            }
        #endregion
        
    }    
}
