using UnityEngine;
using Game.Scripts.UI;
using Unity.VisualScripting;
using UnityEngine.Serialization;
using Game.Scripts.Interface;

namespace Game.Scripts.Controllers
{
    public class WalkingController : ControllerInterface
    {
        [FormerlySerializedAs("currentCamera")]
        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private SplineWalkableArea walkableArea;
        
        [Header("Walking Controller Settings")]
        [SerializeField] private float speed = 5f;
        [SerializeField] private LayerMask meshLayer;
        [SerializeField] private float heightOffset = 2f;
        [SerializeField] private float slideRadius = 2f;
        
        private bool triggerBuffor = false;
        public override void Initialize(){}

        public override void EnableController(){}

        public override void DisableController(){}
        public void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;
            if (other.IsUnityNull()) return;
            if (other.CompareTag("Enter"))
            {
                UIManager.TriggerUI(UIManager.basicEnterTriggerMessage);
            }
            else if (other.CompareTag("Exit"))
            {
                UIManager.TriggerUI(UIManager.basicExitTriggerMessage);
            }
            else if (other.CompareTag("Steer"))
            {
                UIManager.TriggerUI(UIManager.basicSteerTriggerMessage);
            }
        }

        public void OnTriggerStay(Collider other)
        {
            if (!isActive) return;
            if (!triggerBuffor) return;
            triggerBuffor = false;
            if (other.IsUnityNull()) return;
            if (other.CompareTag("Enter")) GameManager.EnterYacht();
            if (other.CompareTag("Exit")) GameManager.LeaveYacht();
            if (other.CompareTag("Steer")) GameManager.SteerYacht();
        }

        public override void UpdateController()
        {
            if (Input.GetKeyDown(KeyCode.F)) triggerBuffor = true;
        }

        public override void FixedUpdateController()
        {
            if (mainCamera.IsUnityNull()) return;
            float h = 0f;
            float v = 0f;
            
            if (Input.GetKey(KeyCode.W)) v += 1f;
            if (Input.GetKey(KeyCode.S)) v -= 1f;
            if (Input.GetKey(KeyCode.A)) h -= 1f;
            if (Input.GetKey(KeyCode.D)) h += 1f;
            
            if (h == 0f && v == 0f) return;
            
            Vector3 moveDir = (mainCamera.transform.forward * v + mainCamera.transform.right * h).normalized;
            Vector3 targetPos = transform.position + moveDir * (speed * Time.deltaTime);

            Ray ray = new Ray(new Vector3(targetPos.x, 5f, targetPos.z), Vector3.down);
            Debug.DrawLine(ray.origin, ray.origin + ray.direction * 10f, Color.red, 1f);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 10f, meshLayer))
            {
                transform.position = hit.point + Vector3.up * heightOffset;
            }
            else
            {
                if (walkableArea.IsUnityNull()) return;
                if (walkableArea.polygon.IsUnityNull()) return;
                if (walkableArea.polygon.Count == 0) return;
                
                // Find nearest edge point
                Vector3 nearest = Vector3.zero;
                float minDist = float.MaxValue;

                foreach (var pt in walkableArea.polygon)
                {
                    Vector3 worldPt = walkableArea.transform.TransformPoint(new Vector3(pt.x, 0, pt.y));
                    float dist = Vector3.Distance(new Vector3(targetPos.x, 0, targetPos.z), new Vector3(worldPt.x, 0, worldPt.z));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = worldPt;
                    }
                }

                // Calculate position at slideRadius distance from boundary
                Vector3 currentPos2D = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 nearest2D = new Vector3(nearest.x, 0, nearest.z);
                Vector3 toBoundary = nearest2D - currentPos2D;
                
                // Create a boundary line at slideRadius distance
                Vector3 slideBoundaryPoint = nearest2D - toBoundary.normalized * slideRadius;
                
                // Calculate sliding direction along the boundary
                Vector3 moveDir2D = new Vector3(moveDir.x, 0, moveDir.z).normalized;
                
                // Project movement onto the tangent of the boundary (perpendicular to toBoundary)
                Vector3 tangent = Vector3.Cross(toBoundary.normalized, Vector3.up);
                Vector3 slideDir = Vector3.Project(moveDir2D, tangent);
                
                // Apply sliding movement
                Vector3 slidePos = transform.position + slideDir * (speed * Time.deltaTime);
                
                // Clamp the slide position to maintain slideRadius distance from boundary
                Vector3 slidePos2D = new Vector3(slidePos.x, 0, slidePos.z);
                Vector3 toSlidePos = slidePos2D - nearest2D;
                if (toSlidePos.magnitude < slideRadius)
                {
                    slidePos2D = nearest2D + toSlidePos.normalized * slideRadius;
                    slidePos = new Vector3(slidePos2D.x, slidePos.y, slidePos2D.z);
                }
                
                // Raycast at the slide position to get proper height
                Ray slideRay = new Ray(new Vector3(slidePos.x, 5f, slidePos.z), Vector3.down);
                if (Physics.Raycast(slideRay, out RaycastHit slideHit, 10f, meshLayer))
                {
                    transform.position = slideHit.point + Vector3.up * heightOffset;
                }
                else
                {
                    // If slide position is also out of bounds, stay at current position
                    transform.position = transform.position;
                }
            }
        }
    }
}