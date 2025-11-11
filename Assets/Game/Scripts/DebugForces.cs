using UnityEngine;

namespace Game.Scripts
{
    public class DebugForces : MonoBehaviour
    {
        private Vector3 lastPosition;
        private Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            lastPosition = transform.position;
        }

        void FixedUpdate()
        {
            float distance = Vector3.Distance(transform.position, lastPosition);
            float velocity = rb.linearVelocity.magnitude;

            if (distance > 0.5f) // Jeśli przeskoczył więcej niż 0.5 jednostek
            {
                Debug.LogWarning($"⚠️ TELEPORT! Distance: {distance:F3}, Velocity: {velocity:F3}");
            }

            if (velocity > 50f) // Nienaturalnie wysoka prędkość
            {
                Debug.LogWarning($"⚠️ HIGH VELOCITY! {velocity:F3}");
            }

            lastPosition = transform.position;
        }
    }
}