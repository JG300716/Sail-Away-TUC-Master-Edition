using UnityEngine;
using System.Collections;

public class SkullRetreat : MonoBehaviour
{
    [Header("Movement")]
    public float retreatDistance = 10f;
    public float duration = 2f;

    private bool isMoving = false;
    private Vector3 startLocalPos;
    private Vector3 targetLocalPos;

    // wywo³ywane przez raycast
    public void OnRayClicked()
    {
        Debug.Log("Skull retreating!");
        if (isMoving) return;

        startLocalPos = transform.localPosition;
        targetLocalPos = startLocalPos + Vector3.left * retreatDistance;

        StartCoroutine(Retreat());
    }

    IEnumerator Retreat()
    {
        isMoving = true;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            yield return null;
        }

        transform.localPosition = targetLocalPos;
        isMoving = false;
    }
}