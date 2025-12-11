using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class CableGrabber : MonoBehaviour
{
    [Header("Settings")]
    public Camera mainCamera;
    public float followSpeed = 50f;
    public float grabRadius = 0.15f; // how close your click must be to grab
    public LayerMask pickLayers = ~0;

    [Header("Scroll Depth Control")]
    public float scrollSensitivity = 5.5f;   // how much scroll changes distance
    public float minGrabDistance = 0.2f;     // prevent pulling inside camera
    public float maxGrabDistance = 50f;       // prevent pushing too far away
    private float grabDistance;              // current depth distance from camera



    private Rigidbody grabbedRb;
    private Plane dragPlane;
    private Vector3 targetPoint;
    private bool isGrabbing;

    void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Start grab
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Ray r = mainCamera.ScreenPointToRay(mouse.position.ReadValue());

            // Use a SphereCast so it's forgiving � easy to click near the cable
            if (Physics.SphereCast(r, grabRadius, out RaycastHit hit, 100f, pickLayers))
            {
                // Find the rigidbody (segment) you hit
                grabbedRb = hit.collider.attachedRigidbody;

                if (grabbedRb != null)
                {
                    dragPlane = new Plane(-mainCamera.transform.forward, hit.point);
                    isGrabbing = true;
                    grabDistance = Vector3.Distance(mainCamera.transform.position, hit.point); // store starting depth nowe

                }
            }
        }

        // Release grab
        if (mouse.leftButton.wasReleasedThisFrame && isGrabbing)
        {
            isGrabbing = false;
            grabbedRb = null;
        }

        // Update the target point under the mouse
        //if (isGrabbing)
        //{
        //    Ray r = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
        //    if (dragPlane.Raycast(r, out float enter))
        //    {
        //        targetPoint = r.GetPoint(enter);
        //    }
        //}

        // Handle mouse scroll for depth adjustment
        if (isGrabbing)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                grabDistance += scroll * scrollSensitivity * Time.deltaTime;
                grabDistance = Mathf.Clamp(grabDistance, minGrabDistance, maxGrabDistance);
            }

            // Update target point including depth offset
            Ray depthRay = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            targetPoint = depthRay.GetPoint(grabDistance);
        }
    }

    void FixedUpdate()
    {
        if (grabbedRb != null && isGrabbing)
        {
            // Move the grabbed rigidbody smoothly toward the target
            Vector3 toTarget = targetPoint - grabbedRb.position;
            Vector3 desiredVelocity = toTarget / Time.fixedDeltaTime;

            // Apply velocity with smoothing
            grabbedRb.linearVelocity = Vector3.Lerp(
                grabbedRb.linearVelocity,
                desiredVelocity,
                Mathf.Clamp01(followSpeed * Time.fixedDeltaTime)
            );
        }
    }

    // Optional debug: visualize grab radius
    void OnDrawGizmosSelected()
    {
        if (grabbedRb != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(grabbedRb.position, grabRadius);
        }
    }
}