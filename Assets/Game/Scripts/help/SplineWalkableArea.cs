using UnityEngine;
using System.Collections.Generic;
using LibTessDotNet;

namespace Game.Scripts
{
    [ExecuteAlways]
    public class SplineWalkableArea : MonoBehaviour
    {
        [System.Serializable]
        public class PolygonHole
        {
            public List<Vector2> points = new List<Vector2>();
        }
        
        [Header("Polygon Points (local coords)")]
        public List<Vector2> polygon = new List<Vector2>();

        [Header("Height Spline (local coords)")]
        public List<SplinePoint> heightSpline = new List<SplinePoint>();

        [Header("Polygon Holes (local coords)")]
        public List<PolygonHole> holes = new List<PolygonHole>();

        [SerializeField] private MeshFilter mf;
        [SerializeField] private MeshCollider mc;

        void OnValidate()
        {
            GenerateMesh();
        }

        public void GenerateMesh()
        {
            if (polygon.Count < 3) return;

            // ======== Tessellation ========
            var tess = new Tess();

            // Główny kontur (zewnętrzny polygon)
            ContourVertex[] outer = new ContourVertex[polygon.Count];
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector3 v = new Vector3(polygon[i].x, SampleHeight(polygon[i].x), polygon[i].y);
                outer[i].Position = new Vec3 { X = v.x, Y = v.y, Z = v.z };
            }
            tess.AddContour(outer, ContourOrientation.Clockwise);

            // Jeśli masz dziurę:
            if (holes != null)
            {
                foreach (var hole in holes) // hole to PolygonHole
                {
                    if (hole.points.Count < 3) continue;
                    ContourVertex[] h = new ContourVertex[hole.points.Count];
                    for (int i = 0; i < hole.points.Count; i++)
                    {
                        Vector3 v = new Vector3(hole.points[i].x, SampleHeight(hole.points[i].x), hole.points[i].y);
                        h[i].Position = new Vec3 { X = v.x, Y = v.y, Z = v.z };
                    }
                    tess.AddContour(h, ContourOrientation.CounterClockwise);
                }
            }

            tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);

            Vector3[] verts = new Vector3[tess.Vertices.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = new Vector3(tess.Vertices[i].Position.X,
                    tess.Vertices[i].Position.Y,
                    tess.Vertices[i].Position.Z);
            }

            int[] triangles = new int[tess.Elements.Length];
            for (int i = 0; i < tess.Elements.Length; i++)
                triangles[i] = tess.Elements[i];

            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;
            mc.convex = true;
            mc.isTrigger = true;
        }

        // ===== Spline Height =====
        public float SampleHeight(float x)
        {
            if (heightSpline.Count < 2) return 0;

            for (int i = 0; i < heightSpline.Count - 1; i++)
            {
                float x1 = heightSpline[i].x;
                float x2 = heightSpline[i + 1].x;

                if (x >= x1 && x <= x2)
                {
                    float t = Mathf.InverseLerp(x1, x2, x);

                    int i0 = Mathf.Clamp(i - 1, 0, heightSpline.Count - 1);
                    int i1 = i;
                    int i2 = i + 1;
                    int i3 = Mathf.Clamp(i + 2, 0, heightSpline.Count - 1);

                    return CatmullRom(t,
                        heightSpline[i0].height,
                        heightSpline[i1].height,
                        heightSpline[i2].height,
                        heightSpline[i3].height
                    );
                }
            }

            return 0;
        }

        public static float CatmullRom(float t, float p0, float p1, float p2, float p3)
        {
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
            );
        }
    }

    [System.Serializable]
    public class SplinePoint
    {
        public float x;      // lokalne X względem obiektu
        public float height; // lokalne Y
    }
}
