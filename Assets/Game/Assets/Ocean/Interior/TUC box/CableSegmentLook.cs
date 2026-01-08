using UnityEngine;

public class CableSegmentLook : MonoBehaviour
{
    public Transform nextSegment;

    void LateUpdate()
    {
        if (nextSegment == null) return;
        Vector3 dir = nextSegment.position - transform.position;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}