using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CableLineRenderer : MonoBehaviour
{
    public CableSpawner spawner;
    LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    void LateUpdate()
    {
        if (spawner == null || spawner.segments.Count == 0) return;
        lr.positionCount = spawner.segments.Count;
        for (int i = 0; i < spawner.segments.Count; i++)
        {
            lr.SetPosition(i, spawner.segments[i].transform.position);
        }
    }
}
