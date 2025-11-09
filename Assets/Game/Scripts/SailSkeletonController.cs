using UnityEngine;

namespace Game.Scripts
{
    /// <summary>
    /// System kontroli żagla z szkieletem bone'ów dla realistycznej animacji
    /// Działa zarówno z Skinned Mesh Renderer jak i z proceduralną deformacją mesh
    /// </summary>
    public class SailSkeletonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private YachtState yachtState;
        [SerializeField] private YachtPhysics yachtPhysics;
        [SerializeField] private Transform boomTransform; // Transform bomu (pivot)

        [Header("Sail Mesh")]
        [SerializeField] private SkinnedMeshRenderer sailMeshRenderer; // Jeśli używasz riggingu
        [SerializeField] private MeshFilter sailMeshFilter; // Jeśli używasz proceduralnej deformacji
        [SerializeField] private bool useSkinning = true; // true = Skinned Mesh, false = proceduralna deformacja

        [Header("Skeleton Bones (opcjonalne - dla Skinned Mesh)")]
        [SerializeField] private Transform[] sailBones; // Kości żagla (od dołu do góry)
        [SerializeField] private int boneCount = 5; // Liczba kości do wygenerowania

        [Header("Flapping Animation")]
        [SerializeField] private bool enableFlapping = true;
        [SerializeField] public float flappingIntensity = 0.3f; // Intensywność trzepotu
        [SerializeField] private float flappingSpeed = 2f; // Prędkość trzepotu
        [SerializeField] private float windInfluence = 1f; // Wpływ wiatru na trzepot
        
        [Header("Sail Physics")]
        [SerializeField] private float sailTension = 0.5f; // Napięcie żagla (0 = luźny, 1 = naprężony)
        [SerializeField] private float curvatureAmount = 0.3f; // Wybrzuszenie żagla
        [SerializeField] private AnimationCurve sailCurvature = AnimationCurve.EaseInOut(0, 0, 1, 1); // Krzywa wybrzuszenia

        [Header("Debug")]
        [SerializeField] private bool showDebugBones = true;
        [SerializeField] private bool showDebugNormals = false;

        private Mesh originalMesh;
        private Mesh deformedMesh;
        private Vector3[] originalVertices;
        private Vector3[] deformedVertices;
        private float[] vertexHeights; // Wysokość każdego werteksa (0-1)
        private float flappingTime = 0f;
        private bool initialized = false;
        private WindManager Wind => WindManager.Instance;

        void Start()
        {
            initialized = yachtState != null && Wind != null;

            if (!initialized)
            {
                Debug.LogError("SailSkeletonController: Missing references!");
                return;
            }

            if (useSkinning)
            {
                InitializeSkinnedMesh();
            }
            else
            {
                InitializeProceduralDeformation();
            }
        }

        #region Skinned Mesh (z bone'ami)
        
        private void InitializeSkinnedMesh()
        {
            if (sailMeshRenderer == null)
            {
                Debug.LogError("Skinned Mesh Renderer not assigned!");
                return;
            }

            // Jeśli nie mamy bone'ów, stwórz je proceduralnie
            if (sailBones == null || sailBones.Length == 0)
            {
                CreateProceduralBones();
            }

            Debug.Log($"Sail skeleton initialized with {sailBones.Length} bones");
        }

        private void CreateProceduralBones()
        {
            // Tworzy hierarchię bone'ów od dołu (boom) do góry żagla
            sailBones = new Transform[boneCount];
            
            Transform parent = boomTransform;
            float heightStep = 1f / (boneCount - 1);

            for (int i = 0; i < boneCount; i++)
            {
                GameObject boneObj = new GameObject($"SailBone_{i}");
                boneObj.transform.parent = parent;
                boneObj.transform.localPosition = Vector3.up * heightStep * i * 5f; // 5 = wysokość żagla
                boneObj.transform.localRotation = Quaternion.identity;
                
                sailBones[i] = boneObj.transform;
                parent = boneObj.transform; // Każda kolejna kość jest child poprzedniej
            }

            Debug.Log($"Created {boneCount} procedural bones for sail");
        }

        private void UpdateSkinnedMesh()
        {
            if (sailBones == null || sailBones.Length == 0) return;

            // Oblicz parametry wiatru
            float apparentWindSpeed = (float)yachtPhysics.GetApparentWindSpeed(
                yachtState.V_current, 
                yachtState.Deg_from_north
            );
            float apparentWindAngle = (float)yachtPhysics.GetApparentWindAngle(
                yachtState.V_current, 
                yachtState.Deg_from_north
            );

            // Normalizuj kąt do -180/180
            if (apparentWindAngle > 180) apparentWindAngle -= 360;

            // Oblicz trzepot
            flappingTime += Time.deltaTime * flappingSpeed;
            float flapping = enableFlapping ? Mathf.Sin(flappingTime) * flappingIntensity : 0f;
            
            // Wpływ wiatru na trzepot (słaby wiatr = więcej trzepotu)
            float windFactor = Mathf.Clamp01(1f - (apparentWindSpeed / 10f));
            flapping *= windFactor * windInfluence;

            // Animuj każdą kość
            for (int i = 0; i < sailBones.Length; i++)
            {
                if (sailBones[i] == null) continue;

                float t = i / (float)(sailBones.Length - 1); // 0 (dół) do 1 (góra)
                
                // Góra żagla trzepocze bardziej
                float localFlapping = flapping * t * t;
                
                // Wybrzuszenie żagla (więcej na środku)
                float curve = sailCurvature.Evaluate(t) * curvatureAmount * (1f - sailTension);
                
                // Kąt wiatru wpływa na wybrzuszenie
                float windCurve = Mathf.Abs(Mathf.Sin(apparentWindAngle * Mathf.Deg2Rad)) * curve;

                // Aplikuj rotację
                Quaternion flappingRotation = Quaternion.Euler(0, localFlapping * 30f, windCurve * 20f);
                sailBones[i].localRotation = flappingRotation;
            }
        }

        #endregion

        #region Procedural Mesh Deformation (bez bone'ów)

        private void InitializeProceduralDeformation()
        {
            if (sailMeshFilter == null)
            {
                Debug.LogError("Mesh Filter not assigned!");
                return;
            }

            originalMesh = sailMeshFilter.sharedMesh;
            if (originalMesh == null)
            {
                Debug.LogError("No mesh found on MeshFilter!");
                return;
            }

            // Skopiuj mesh do modyfikacji
            deformedMesh = Instantiate(originalMesh);
            sailMeshFilter.mesh = deformedMesh;

            originalVertices = originalMesh.vertices;
            deformedVertices = new Vector3[originalVertices.Length];
            vertexHeights = new float[originalVertices.Length];

            // Oblicz wysokość każdego werteksa (0 = dół, 1 = góra)
            Bounds bounds = originalMesh.bounds;
            for (int i = 0; i < originalVertices.Length; i++)
            {
                vertexHeights[i] = Mathf.InverseLerp(
                    bounds.min.y, 
                    bounds.max.y, 
                    originalVertices[i].y
                );
            }

            Debug.Log($"Procedural sail deformation initialized with {originalVertices.Length} vertices");
        }

        private void UpdateProceduralDeformation()
        {
            if (deformedMesh == null || originalVertices == null) return;

            // Oblicz parametry wiatru
            float apparentWindSpeed = (float)yachtPhysics.GetApparentWindSpeed(
                yachtState.V_current, 
                yachtState.Deg_from_north
            );
            float apparentWindAngle = (float)yachtPhysics.GetApparentWindAngle(
                yachtState.V_current, 
                yachtState.Deg_from_north
            );

            if (apparentWindAngle > 180) apparentWindAngle -= 360;

            // Oblicz trzepot
            flappingTime += Time.deltaTime * flappingSpeed;
            float flapping = enableFlapping ? Mathf.Sin(flappingTime) * flappingIntensity : 0f;
            
            float windFactor = Mathf.Clamp01(1f - (apparentWindSpeed / 10f));
            flapping *= windFactor * windInfluence;

            // Deformuj każdy werteks
            for (int i = 0; i < originalVertices.Length; i++)
            {
                Vector3 vertex = originalVertices[i];
                float height = vertexHeights[i];

                // Trzepot (bardziej na górze)
                float localFlapping = flapping * height * height;
                float flappingOffset = Mathf.Sin(flappingTime + vertex.x * 2f) * localFlapping;

                // Wybrzuszenie (więcej na środku wysokości)
                float curve = sailCurvature.Evaluate(height) * curvatureAmount * (1f - sailTension);
                float windCurve = Mathf.Abs(Mathf.Sin(apparentWindAngle * Mathf.Deg2Rad)) * curve;

                // Aplikuj deformację
                deformedVertices[i] = vertex;
                deformedVertices[i].z += windCurve * height; // Wybrzuszenie w kierunku wiatru
                deformedVertices[i].x += flappingOffset; // Trzepot boczny
            }

            // Zaktualizuj mesh
            deformedMesh.vertices = deformedVertices;
            deformedMesh.RecalculateNormals();
            deformedMesh.RecalculateBounds();
        }

        #endregion

        void Update()
        {
            if (!initialized) return;

            if (useSkinning)
            {
                UpdateSkinnedMesh();
            }
            else
            {
                UpdateProceduralDeformation();
            }
        }

        #region Debug Visualization

        void OnDrawGizmos()
        {
            if (!showDebugBones || !Application.isPlaying) return;

            // Rysuj bone'y
            if (useSkinning && sailBones != null)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < sailBones.Length; i++)
                {
                    if (sailBones[i] == null) continue;

                    Gizmos.DrawSphere(sailBones[i].position, 0.1f);
                    
                    if (i > 0 && sailBones[i - 1] != null)
                    {
                        Gizmos.DrawLine(sailBones[i - 1].position, sailBones[i].position);
                    }
                }
            }

            // Rysuj normalne mesh'a
            if (showDebugNormals && !useSkinning && deformedMesh != null)
            {
                Gizmos.color = Color.blue;
                Vector3[] vertices = deformedMesh.vertices;
                Vector3[] normals = deformedMesh.normals;
                
                for (int i = 0; i < vertices.Length; i += 10) // Co 10 werteks dla czytelności
                {
                    Vector3 worldPos = transform.TransformPoint(vertices[i]);
                    Vector3 worldNormal = transform.TransformDirection(normals[i]);
                    Gizmos.DrawLine(worldPos, worldPos + worldNormal * 0.5f);
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Ustawia napięcie żagla (0 = luźny, 1 = naprężony)
        /// </summary>
        public void SetSailTension(float tension)
        {
            sailTension = Mathf.Clamp01(tension);
        }

        /// <summary>
        /// Włącza/wyłącza trzepot żagla
        /// </summary>
        public void SetFlappingEnabled(bool enabled)
        {
            enableFlapping = enabled;
        }

        /// <summary>
        /// Resetuje mesh do stanu początkowego
        /// </summary>
        public void ResetSailDeformation()
        {
            if (!useSkinning && deformedMesh != null && originalVertices != null)
            {
                deformedMesh.vertices = originalVertices;
                deformedMesh.RecalculateNormals();
            }
        }

        #endregion
    }
}