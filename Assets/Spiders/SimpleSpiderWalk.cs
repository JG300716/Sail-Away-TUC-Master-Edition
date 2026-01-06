using UnityEngine;

public class SimpleSpiderWalk : MonoBehaviour
{
    [Header("Path")]
    public Transform pointA;
    public Transform pointB;

    [Header("Movement")]
    public float speed = 0.5f;

    public float reachDistance = 0.0002f;

    // dotarł do końca → teleport
    void Update()
    {
        if (pointA == null || pointB == null)
            return;

        Vector3 dir = pointB.position - transform.position;

        // ruch do przodu
        transform.position += dir.normalized * speed * Time.deltaTime;

        // dotarł do końca → teleport
        if (dir.magnitude <= reachDistance)
        {
            transform.position = pointA.position;
        }
    }
}