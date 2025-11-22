using UnityEngine;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace Game.Scripts
{
public class Lightning : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public int segments = 20;
    public float offset = 0.5f;
    public float lifetime = 0.05f;
    public int branches = 3;
    public float branchOffset = 0.5f;

    LineRenderer lr;
    List<LineRenderer> branchLRs = new();

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        GenerateLightning();
        Invoke(nameof(DestroyBolt), lifetime);
    }

    void GenerateLightning()
    {
        // Główny piorun
        Vector3[] points = new Vector3[segments];
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 pos = Vector3.Lerp(startPoint.position, endPoint.position, t);

            pos += new Vector3(
                Random.Range(-offset, offset),
                Random.Range(-offset, offset),
                Random.Range(-offset, offset)
            );

            points[i] = pos;
        }

        lr.positionCount = segments;
        lr.SetPositions(points);

        // Odgałęzienia
        for (int b = 0; b < branches; b++)
        {
            CreateBranch(points[Random.Range(0, segments - 2)]);
        }
    }

    void CreateBranch(Vector3 start)
    {
        GameObject g = new GameObject("branch");
        LineRenderer br = g.AddComponent<LineRenderer>();

        br.material = lr.material;
        br.widthMultiplier = lr.widthMultiplier * 0.5f;

        Vector3 end = start + new Vector3(
            Random.Range(-branchOffset, branchOffset),
            Random.Range(-branchOffset, branchOffset),
            Random.Range(-branchOffset, branchOffset)
        );

        br.positionCount = 2;
        br.SetPosition(0, start);
        br.SetPosition(1, end);

        branchLRs.Add(br);
    }

    void DestroyBolt()
    {
        Destroy(gameObject);
        foreach (var br in branchLRs)
            Destroy(br.gameObject);
    }

    private void OnDrawGizmos()
    {
        if (startPoint.IsUnityNull() || endPoint.IsUnityNull())
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
    }
}
}