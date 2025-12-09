//#define ENABLE_DRAWING

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.VisualScripting;
using static Game.Scripts.SplineWalkableArea;

[CustomEditor(typeof(Game.Scripts.SplineWalkableArea))]
public class SplineWalkableAreaEditor : Editor
{
    private Game.Scripts.SplineWalkableArea area;

    private int activeHoleIndex = -1; // -1 = main polygon
    private bool showMainPolygon = true; // widoczność punktów głównego polygonu
    private List<bool> showHoles = new List<bool>(); // widoczność dziur
    private bool enableAddingPoints = true; // możliwość dodawania punktów Ctrl+LPM

    #if ENABLE_DRAWING
    private void OnEnable()
    {
        area = (Game.Scripts.SplineWalkableArea)target;
        SceneView.duringSceneGui += OnSceneGUI;
        SyncHolesVisibility();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (area.IsUnityNull()) return;
        Transform tr = area.transform;
        Event e = Event.current;

        // ===== GUI w SceneView =====
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 300, 280), GUI.skin.box);

        GUILayout.Label("Add Points Target");

        string[] options = new string[area.holes.Count + 1];
        options[0] = "Main Polygon";
        for (int i = 0; i < area.holes.Count; i++) options[i + 1] = "Hole " + i;

        activeHoleIndex = GUILayout.SelectionGrid(activeHoleIndex + 1, options, 1) - 1;

        if (GUILayout.Button("Add New Hole"))
        {
            Undo.RecordObject(area, "Add Hole");
            area.holes.Add(new PolygonHole());
            SyncHolesVisibility();
        }

        // ===== Widoczność =====
        GUILayout.Space(5);
        GUILayout.Label("Visibility");
        showMainPolygon = GUILayout.Toggle(showMainPolygon, "Show Main Polygon Points");

        for (int i = 0; i < area.holes.Count; i++)
        {
            showHoles[i] = GUILayout.Toggle(showHoles[i], $"Show Hole {i} Points");
        }

        // ===== Możliwość dodawania punktów =====
        GUILayout.Space(5);
        enableAddingPoints = GUILayout.Toggle(enableAddingPoints, "Enable Adding Points (Ctrl+LMB)");

        GUILayout.EndArea();
        Handles.EndGUI();

        // ===== Rysowanie polygonu głównego =====
        Handles.color = Color.yellow;
        DrawPolygonLines(area.polygon, tr);

        if (showMainPolygon)
            DrawAndMovePoints(area.polygon, tr, (i, pos) => { area.polygon[i] = pos; area.GenerateMesh(); });

        // ===== Rysowanie dziur =====
        Handles.color = Color.blue;
        for (int h = 0; h < area.holes.Count; h++)
        {
            DrawPolygonLines(area.holes[h].points, tr);

            if (showHoles[h])
                DrawAndMovePoints(area.holes[h].points, tr, (i, pos) => { area.holes[h].points[i] = pos; area.GenerateMesh(); });
        }

        // ===== Dodawanie punktów tylko Ctrl + LPM i jeśli enableAddingPoints = true =====
        if (enableAddingPoints && e.type == EventType.MouseDown && e.button == 0 && e.control)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (new Plane(Vector3.up, tr.position).Raycast(ray, out float dist))
            {
                Vector3 hit = ray.GetPoint(dist);
                Vector3 local = tr.InverseTransformPoint(hit);

                Undo.RecordObject(area, "Add Point");

                if (activeHoleIndex < 0)
                    area.polygon.Add(new Vector2(local.x, local.z));
                else
                {
                    while (activeHoleIndex >= area.holes.Count)
                        area.holes.Add(new PolygonHole());
                    area.holes[activeHoleIndex].points.Add(new Vector2(local.x, local.z));
                    SyncHolesVisibility();
                }

                area.GenerateMesh();
                e.Use();
            }
        }

        // ===== Height Spline Points =====
        Handles.color = Color.cyan;
        for (int i = 0; i < area.heightSpline.Count; i++)
        {
            Vector3 wp = tr.TransformPoint(new Vector3(area.heightSpline[i].x, area.heightSpline[i].height, 0));
            EditorGUI.BeginChangeCheck();
            Vector3 newWp = Handles.PositionHandle(wp, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(area, "Move spline point");
                Vector3 local = tr.InverseTransformPoint(newWp);
                area.heightSpline[i].x = local.x;
                area.heightSpline[i].height = local.y;
                area.GenerateMesh();
            }
        }

        // ===== Height Spline Curve =====
        Handles.color = Color.green;
        int steps = 50;
        if (area.heightSpline.Count >= 2)
        {
            for (int s = 0; s < steps; s++)
            {
                float t0 = s / (float)steps;
                float t1 = (s + 1) / (float)steps;

                float x0 = Mathf.Lerp(area.heightSpline[0].x, area.heightSpline[area.heightSpline.Count - 1].x, t0);
                float x1 = Mathf.Lerp(area.heightSpline[0].x, area.heightSpline[area.heightSpline.Count - 1].x, t1);

                float y0 = area.SampleHeight(x0);
                float y1 = area.SampleHeight(x1);

                Vector3 p0 = tr.TransformPoint(new Vector3(x0, y0, 0));
                Vector3 p1 = tr.TransformPoint(new Vector3(x1, y1, 0));
                Handles.DrawLine(p0, p1);
            }
        }
    }

    #endif
    private void SyncHolesVisibility()
    {
        while (showHoles.Count < area.holes.Count) showHoles.Add(true);
        while (showHoles.Count > area.holes.Count) showHoles.RemoveAt(showHoles.Count - 1);
    }
    
    void DrawPolygonLines(List<Vector2> poly, Transform tr)
    {
        for (int i = 0; i < poly.Count; i++)
        {
            Vector3 p1 = tr.TransformPoint(new Vector3(poly[i].x, 0, poly[i].y));
            Vector3 p2 = tr.TransformPoint(new Vector3(poly[(i + 1) % poly.Count].x, 0, poly[(i + 1) % poly.Count].y));
            Handles.DrawLine(p1, p2);
        }
    }

    void DrawAndMovePoints(List<Vector2> points, Transform tr, System.Action<int, Vector2> onMoved)
    {
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 worldPos = tr.TransformPoint(new Vector3(points[i].x, 0, points[i].y));
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 local = tr.InverseTransformPoint(moved);
                onMoved(i, new Vector2(local.x, local.z));
            }
        }
    }
}
