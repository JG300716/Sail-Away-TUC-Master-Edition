using UnityEngine;
using UnityEngine.InputSystem;

public class VRCableGrabber : MonoBehaviour
{
    [Header("Settings")]
    public float followSpeed = 50f;
    public float grabRadius = 0.1f;
    public LayerMask pickLayers = ~0;

    [Header("Input")]
    public InputActionProperty gripAction; // Grip button

    private Rigidbody grabbedRb;
    private bool isGrabbing;
    private Vector3 targetPoint;
    private bool isGripPressed = false; //only for gizmos

    [Header("Laser")]
    public LineRenderer line;
    public float laserLength = 5f;
    public Color idleColor = Color.green;
    public Color grabColor = Color.red;
    private float grabDistance;

    [Header("Input - Distance Control")]
    public InputActionProperty thumbstickAction;
    public float distanceSpeed = 2.0f;
    public float minGrabDistance = 0.2f;
    public float maxGrabDistance = 10f;


    void OnEnable()
    {
        gripAction.action.Enable();
        thumbstickAction.action.Enable();
    }

    void OnDisable()
    {
        gripAction.action.Disable();
        thumbstickAction.action.Disable();
    }

    void Update()
    {
        isGripPressed = gripAction.action.IsPressed();

        UpdateLaser();

        // START GRAB
        if (isGripPressed && !isGrabbing)
        {
            Ray ray = new Ray(transform.position, transform.forward);

            if (Physics.SphereCast(ray, grabRadius, out RaycastHit hit, maxGrabDistance, pickLayers))
            {
                grabbedRb = hit.collider.attachedRigidbody;
                if (grabbedRb != null)
                {
                    isGrabbing = true;
                grabDistance = hit.distance;
                }
            }
        }

        // RELEASE
        if (!isGripPressed && isGrabbing)
        {
            isGrabbing = false;
            grabbedRb = null;
        }

        if (isGrabbing && grabbedRb != null)
        {
            targetPoint = transform.position + transform.forward * grabDistance;
        }
        if (isGrabbing)
        {
            Vector2 stick = thumbstickAction.action.ReadValue<Vector2>();

            if (Mathf.Abs(stick.y) > 0.1f) // deadzone
            {
                grabDistance += stick.y * distanceSpeed * Time.deltaTime;
                grabDistance = Mathf.Clamp(grabDistance, minGrabDistance, maxGrabDistance);
            }
        }
    }

    void FixedUpdate()
    {
        if (isGrabbing && grabbedRb != null)
        {
            Vector3 toTarget = targetPoint - grabbedRb.position;
            Vector3 desiredVelocity = toTarget / Time.fixedDeltaTime;

            grabbedRb.linearVelocity = Vector3.Lerp(
                grabbedRb.linearVelocity,
                desiredVelocity,
                followSpeed * Time.fixedDeltaTime
            );
        }
    }
    void OnDrawGizmos()
    {
        if (isGripPressed)
            Gizmos.color = Color.red;     // trzymasz trigger
        else
            Gizmos.color = Color.green;   // nie trzymasz

        Gizmos.DrawRay(transform.position, transform.forward * 0.5f);
    }
    void UpdateLaser()
    {
        line.startColor = isGripPressed ? grabColor : idleColor;
        line.endColor = line.startColor;

        line.SetPosition(0, transform.position);
        line.SetPosition(1, transform.position + transform.forward * laserLength);
    }
}
