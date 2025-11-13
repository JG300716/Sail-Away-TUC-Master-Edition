using UnityEngine;

/// <summary>
/// Symuluje szoty (liny sterujące) które ciągną róg żagla
/// Używa colliderów jako fizycznych "lin"
/// </summary>
public class ClothSheetControl : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Cloth cloth;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Transform sheetAnchor; // Fok_shot_Anchor - punkt mocowania szotów
    
    [Header("Sheet Corner Vertex")]
    [SerializeField] private int cornerVertexIndex = 23;
    [SerializeField] private bool autoFind = true;
    [SerializeField] private float searchRadius = 0.5f;
    
    [Header("Sheet Rope Settings")]
    [SerializeField] private float ropeRadius = 0.01f; // Grubość "liny"
    [SerializeField] private float ropePullStrength = 200f; // Jak mocno ciągnie
    [SerializeField] private int ropeSegments = 3; // Ilość segmentów liny
    
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool showDebugLogs = false;
    
    private GameObject[] ropeSegmentObjects;
    private CapsuleCollider[] ropeColliders;
    private Rigidbody[] ropeRigidbodies;
    private bool isSetup = false;

    void Start()
    {
        if (cloth == null)
            cloth = GetComponent<Cloth>();
        
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();
        
        Setup();
    }

    private void Setup()
    {
        if (cloth == null || meshFilter == null || sheetAnchor == null)
        {
            Debug.LogError("[ClothSheetControl] Brak komponentów!");
            return;
        }
        
        // Auto-find corner vertex
        if (autoFind)
        {
            cornerVertexIndex = FindClosestVertex(sheetAnchor.position);
        }
        
        Mesh mesh = meshFilter.sharedMesh;
        if (cornerVertexIndex < 0 || cornerVertexIndex >= mesh.vertexCount)
        {
            Debug.LogError($"[ClothSheetControl] Zły vertex index: {cornerVertexIndex}");
            return;
        }
        
        // Stwórz "linę" (rope)
        CreateSheetRope();
        
        // Stats
        int pinnedCount = 0;
        if (cloth.coefficients != null)
        {
            foreach (var c in cloth.coefficients)
            {
                if (c.maxDistance < 0.01f)
                    pinnedCount++;
            }
        }
        
        Debug.Log($"[ClothSheetControl] ✓ Setup complete!");
        Debug.Log($"  Static pins: {pinnedCount}");
        Debug.Log($"  Corner vertex: {cornerVertexIndex}");
        Debug.Log($"  Rope segments: {ropeSegments}");
        
        isSetup = true;
    }

    /// <summary>
    /// Tworzy "linę" z colliderów łączącą corner vertex z sheet anchor
    /// </summary>
    private void CreateSheetRope()
    {
        Mesh mesh = meshFilter.sharedMesh;
        Vector3 cornerWorldPos = transform.TransformPoint(mesh.vertices[cornerVertexIndex]);
        
        ropeSegmentObjects = new GameObject[ropeSegments];
        ropeColliders = new CapsuleCollider[ropeSegments];
        ropeRigidbodies = new Rigidbody[ropeSegments];
        
        for (int i = 0; i < ropeSegments; i++)
        {
            // Pozycja segmentu (interpolacja między corner a anchor)
            float t = (float)(i + 1) / (ropeSegments + 1);
            Vector3 segmentPos = Vector3.Lerp(cornerWorldPos, sheetAnchor.position, t);
            
            // Stwórz segment
            GameObject segment = new GameObject($"RopeSegment_{i}");
            segment.transform.position = segmentPos;
            segment.transform.SetParent(transform);
            
            // Rigidbody (kinematic dla pierwszego i ostatniego, dynamiczny dla środkowych)
            Rigidbody rb = segment.AddComponent<Rigidbody>();
            
            if (i == ropeSegments - 1)
            {
                // Ostatni segment - kinematic, podąża za anchor
                rb.isKinematic = true;
                segment.transform.SetParent(sheetAnchor);
            }
            else
            {
                // Środkowe segmenty - dynamiczne
                rb.isKinematic = false;
                rb.mass = 0.01f;
                rb.linearDamping = 1f;
                rb.angularDamping = 1f;
                rb.useGravity = false;
            }
            
            // Capsule Collider jako "lina"
            CapsuleCollider capsule = segment.AddComponent<CapsuleCollider>();
            capsule.radius = ropeRadius;
            capsule.height = 0.1f; // Długość segmentu
            capsule.direction = 1; // Y-axis
            
            ropeSegmentObjects[i] = segment;
            ropeColliders[i] = capsule;
            ropeRigidbodies[i] = rb;
        }
        
        // Dodaj colliders do Cloth
        AddRopeCollidersToCloth();
        
        Debug.Log($"[ClothSheetControl] Utworzono {ropeSegments} segmentów liny");
    }

    /// <summary>
    /// Dodaje rope colliders do Cloth
    /// </summary>
    private void AddRopeCollidersToCloth()
    {
        ClothSphereColliderPair[] existing = cloth.sphereColliders ?? new ClothSphereColliderPair[0];
        ClothSphereColliderPair[] newArray = new ClothSphereColliderPair[existing.Length + ropeSegments];
        
        existing.CopyTo(newArray, 0);
        
        // Dla każdego rope segment, dodaj jego collider
        // (konwertuj CapsuleCollider na SphereCollider dla Cloth)
        for (int i = 0; i < ropeSegments; i++)
        {
            // Dodaj mały SphereCollider obok CapsuleCollider
            SphereCollider sphere = ropeSegmentObjects[i].AddComponent<SphereCollider>();
            sphere.radius = ropeRadius * 2f;
            sphere.isTrigger = false;
            
            newArray[existing.Length + i] = new ClothSphereColliderPair(sphere);
        }
        
        cloth.sphereColliders = newArray;
        
        Debug.Log($"[ClothSheetControl] Dodano {ropeSegments} rope colliders do Cloth");
    }

    void FixedUpdate()
    {
        if (!isSetup)
            return;
        
        UpdateRopePhysics();
    }

    /// <summary>
    /// Aktualizuje fizykę liny - ciągnie corner vertex
    /// </summary>
    private void UpdateRopePhysics()
    {
        if (cloth.vertices == null || cornerVertexIndex >= cloth.vertices.Length)
            return;
        
        // Pozycja corner vertex w world space
        Vector3 cornerWorldPos = transform.TransformPoint(cloth.vertices[cornerVertexIndex]);
        
        // Pierwszy segment liny powinien być blisko corner vertex
        if (ropeRigidbodies.Length > 0 && !ropeRigidbodies[0].isKinematic)
        {
            Vector3 firstSegmentPos = ropeSegmentObjects[0].transform.position;
            
            // Oblicz siłę ciągnącą segment do corner vertex
            Vector3 toCorner = cornerWorldPos - firstSegmentPos;
            float distance = toCorner.magnitude;
            
            if (distance > 0.01f)
            {
                Vector3 force = toCorner.normalized * (distance * ropePullStrength);
                ropeRigidbodies[0].AddForce(force);
                
                if (showDebugLogs && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[ClothSheetControl] Rope force: {force.magnitude:F2}N, dist: {distance:F3}m");
                }
            }
        }
        
        // Połącz segmenty "sprężynami"
        for (int i = 0; i < ropeSegments - 1; i++)
        {
            if (ropeRigidbodies[i].isKinematic || ropeRigidbodies[i + 1].isKinematic)
                continue;
            
            Vector3 pos1 = ropeSegmentObjects[i].transform.position;
            Vector3 pos2 = ropeSegmentObjects[i + 1].transform.position;
            
            Vector3 delta = pos2 - pos1;
            float dist = delta.magnitude;
            float targetDist = 0.05f; // Docelowa odległość między segmentami
            
            if (dist > targetDist)
            {
                // Ciągną się do siebie
                Vector3 force = delta.normalized * (dist - targetDist) * ropePullStrength * 0.5f;
                ropeRigidbodies[i].AddForce(force);
                ropeRigidbodies[i + 1].AddForce(-force);
            }
        }
    }

    private int FindClosestVertex(Vector3 worldPosition)
    {
        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        
        int closest = 0;
        float minDist = float.MaxValue;
        
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldVertex = transform.TransformPoint(vertices[i]);
            float dist = Vector3.Distance(worldVertex, worldPosition);
            
            if (dist < minDist && dist < searchRadius)
            {
                minDist = dist;
                closest = i;
            }
        }
        
        Debug.Log($"[ClothSheetControl] Znaleziono corner vertex {closest} (dist: {minDist:F3}m)");
        return closest;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;
        
        // Sheet Anchor (czerwony)
        if (sheetAnchor != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(sheetAnchor.position, 0.05f);
            Gizmos.DrawWireSphere(sheetAnchor.position, 0.1f);
        }
        
        // Corner vertex (żółty)
        if (cloth != null && cloth.vertices != null && 
            cornerVertexIndex >= 0 && cornerVertexIndex < cloth.vertices.Length)
        {
            Vector3 cornerWorld = transform.TransformPoint(cloth.vertices[cornerVertexIndex]);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(cornerWorld, 0.06f);
            
            // Linia do anchor
            if (sheetAnchor != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(cornerWorld, sheetAnchor.position);
            }
        }
        
        // Rope segments (magenta)
        if (ropeSegmentObjects != null)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < ropeSegmentObjects.Length; i++)
            {
                if (ropeSegmentObjects[i] != null)
                {
                    Gizmos.DrawWireSphere(ropeSegmentObjects[i].transform.position, ropeRadius * 5f);
                    
                    // Linia między segmentami
                    if (i > 0 && ropeSegmentObjects[i - 1] != null)
                    {
                        Gizmos.DrawLine(
                            ropeSegmentObjects[i - 1].transform.position,
                            ropeSegmentObjects[i].transform.position
                        );
                    }
                }
            }
        }
        
        // Static pins (zielony)
        if (cloth != null && cloth.coefficients != null && cloth.vertices != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < cloth.coefficients.Length && i < cloth.vertices.Length; i++)
            {
                if (cloth.coefficients[i].maxDistance < 0.01f)
                {
                    Vector3 v = transform.TransformPoint(cloth.vertices[i]);
                    Gizmos.DrawSphere(v, 0.025f);
                }
            }
        }
    }

    void OnDestroy()
    {
        // Cleanup
        if (ropeSegmentObjects != null)
        {
            foreach (var obj in ropeSegmentObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
        }
    }

    [ContextMenu("Rebuild Rope")]
    private void RebuildRope()
    {
        // Cleanup old rope
        if (ropeSegmentObjects != null)
        {
            foreach (var obj in ropeSegmentObjects)
            {
                if (obj != null)
                    DestroyImmediate(obj);
            }
        }
        
        CreateSheetRope();
    }
}