using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CableSegment : MonoBehaviour
{
    [HideInInspector] public int index;
    [HideInInspector] public bool isHead = false;

    // called automatically by Unity when a connected joint breaks
    void OnJointBreak(float breakForce)
    {
        // optional: visual feedback or play a sound
        // Debug.Log($"{name} joint broke with force {breakForce}");
    }
}


