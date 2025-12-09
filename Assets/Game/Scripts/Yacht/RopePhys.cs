using System;
using Unity.VisualScripting;
using UnityEngine;

public class RopePhys : MonoBehaviour
{
    private enum RopeType
    {
        FreeEnded,
        FixedEnded,
        Looped
    }
    private struct RopeSegment
    {
        public Vector3 posNow;
        public Vector3 posOld;

        public RopeSegment(Vector3 pos)
        {
            posNow = pos;
            posOld = pos;
        }
    } 
    
    [Header("Rope Settings")]
    [SerializeField] private float _segmentLength = 0;
    [SerializeField] private int _numOfSegments = 0;
    [SerializeField] private RopeType _ropeType = RopeType.FreeEnded;

    [Header("References")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform _startPoint;
    //[ShowIfEnum("_ropeType", 1,2)] 
    [SerializeField] private Transform _endPoint;

    [Header("Looped Rope Settings")]
    //[ShowIfEnum("_ropeType", 2)]
    [SerializeField] private int _radiusRopeSegments = 0;
    //[ShowIfEnum("_ropeType", 2)]
    [SerializeField] private float _loopedRopeRadius = 0.5f;
    //[ShowIfEnum("_ropeType", 2)]
    
    [Header("Physics")]
    [SerializeField] private Vector3 _gravity = new Vector3(0, -9.81f, 0);
    [SerializeField] private float _ropeDrag = 0.1f;
    [SerializeField] private float _elasticity = 0;

    [Header("Constraints")]
    [SerializeField] private int _constraintIterations = 50;
    
    [Header("Collisions")]
    [SerializeField] private int _collisionInterval = 5;
    [SerializeField] private float _collisionRadius = 0.1f;
    [SerializeField] private LayerMask _collisionLayerMask;
    
    private RopeSegment[] _ropeSegments;
    private Action _ropeConstrainsMethod;
    private void Awake()
    {
        _ropeSegments = new RopeSegment[_numOfSegments];
        Vector3 ropeDirection = Vector3.down;
        switch (_ropeType)
        {
            case RopeType.FixedEnded:
                if (_startPoint.IsUnityNull() || _endPoint.IsUnityNull()) return;
                ropeDirection = (_endPoint.position - _startPoint.position).normalized;
                _ropeConstrainsMethod = ApplyFixedEndConstraints;
            break;
            case RopeType.FreeEnded:
                if (_startPoint.IsUnityNull()) return;
                _ropeConstrainsMethod = ApplyFreeEndConstraints;
            break;
            case RopeType.Looped:
                if (_startPoint.IsUnityNull() || _endPoint.IsUnityNull()) return;

                _radiusRopeSegments += _radiusRopeSegments % 2;
                
                float lengthAB = (_endPoint.position - _startPoint.position).magnitude;
                float a = lengthAB / 2f;                 // horizontal radius
                float b = a * _loopedRopeRadius;         // vertical radius (adjustable)
                
                for (int i = 0; i < _numOfSegments; i++)
                {
                    float theta = (i / (float)_numOfSegments) * 2f * Mathf.PI;
                    Vector3 segmentPos = new Vector3( a * Mathf.Cos(theta), b * Mathf.Sin(theta), 0);
                    _ropeSegments[i] = new RopeSegment(segmentPos);
                }
                _ropeConstrainsMethod = ApplyLoopedConstraints;
            return;
        }
        for (int i = 0; i < _numOfSegments; i++)
        {
            Vector3 segmentPos = _startPoint.position + ropeDirection * _segmentLength * i;
            _ropeSegments[i] = new RopeSegment(segmentPos);
        }
    }

    void Update()
    {
        DrawRope();
    }

    void FixedUpdate()
    {
        SimulateRope();
        for (int i = 0; i < _constraintIterations; i++)
        {
            _ropeConstrainsMethod();
            if (i % _collisionInterval == 0) HandleCollisions();
        }
    }
    
    void DrawRope()
    {
        Vector3[] ropePositions = new Vector3[_numOfSegments];
        for (int i = 0; i < _numOfSegments; i++)
        {
            ropePositions[i] = _ropeSegments[i].posNow;
        }

        lineRenderer.positionCount = ropePositions.Length;
        lineRenderer.SetPositions(ropePositions);
    }

    void SimulateRope()
    {
        for (int i = 1; i < _numOfSegments; i++)
        {
            RopeSegment segment = _ropeSegments[i];
            Vector3 velocity = (segment.posNow - segment.posOld) * (1f - _ropeDrag);

            segment.posOld = segment.posNow;
            segment.posNow += velocity;
            segment.posNow += _gravity * Time.fixedDeltaTime;

            _ropeSegments[i] = segment;
        }
    }
    
    void ApplyFreeEndConstraints()
    {
        if (_startPoint.IsUnityNull()) return;
        
        RopeSegment firstSegment = _ropeSegments[0];
        firstSegment.posNow = _startPoint.position;
        _ropeSegments[0] = firstSegment;

        for (int i = 0; i < _numOfSegments - 1; i++)
        {
            RopeSegment segmentA = _ropeSegments[i];
            RopeSegment segmentB = _ropeSegments[i + 1];
            
            float dist = (segmentA.posNow - segmentB.posNow).magnitude;
            float error = dist - _segmentLength;
            
            Vector3 changeDir = (segmentA.posNow - segmentB.posNow).normalized;
            Vector3 changeAmount = changeDir * error;
            
            if (i != 0)
            {
                segmentA.posNow -= changeAmount * 0.5f;
                segmentB.posNow += changeAmount * 0.5f;
            }
            else
            {
                segmentB.posNow += changeAmount;
            }
            
            _ropeSegments[i] = segmentA;
            _ropeSegments[i + 1] = segmentB;
        }
    }

    void ApplyFixedEndConstraints()
    {
        if (_startPoint.IsUnityNull() || _endPoint.IsUnityNull()) return;
        
        RopeSegment firstSegment = _ropeSegments[0];
        firstSegment.posNow = _startPoint.position;
        _ropeSegments[0] = firstSegment;
        RopeSegment lastSegment = _ropeSegments[_numOfSegments - 1];
        lastSegment.posNow = _endPoint.position;
        _ropeSegments[_numOfSegments - 1] = lastSegment;

        for (int i = 0; i < _numOfSegments - 1; i++)
        {
            RopeSegment a = _ropeSegments[i];
            RopeSegment b = _ropeSegments[i + 1];

            Vector3 delta = b.posNow - a.posNow;
            float dist = delta.magnitude;
            if (dist < 0.0001f) continue;

            float error = dist - _segmentLength;
            Vector3 correction = (delta / dist) * error;

            // a is fixed (first point)
            if (i == 0)
            {
                b.posNow -= correction;
            }
            // b is fixed (last point)
            else if (i + 1 == _numOfSegments - 1)
            {
                a.posNow += correction;
            }
            else
            {
                // normal segment
                a.posNow += correction * 0.5f;
                b.posNow -= correction * 0.5f;
            }

            _ropeSegments[i] = a;
            _ropeSegments[i + 1] = b;
        }
        
        _ropeSegments[0] = firstSegment;
        _ropeSegments[_numOfSegments - 1] = lastSegment;
    }

    void ApplyLoopedConstraints()
    {
        if (_startPoint.IsUnityNull() || _endPoint.IsUnityNull()) return;

        Vector3 A = _startPoint.position;
        Vector3 B = _endPoint.position;

        // Vector from start to end
        Vector3 AB = B - A;
        float lengthAB = AB.magnitude;

        // Compute ellipse axes
        float a = lengthAB / 2f;                 // horizontal radius
        float b = a * _loopedRopeRadius;         // vertical radius (adjustable)

        Vector3 center = (A + B) / 2f;
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, AB.normalized);

        // Apply distance constraints between segments
        for (int i = 0; i < _numOfSegments - 1; i++)
        {
            RopeSegment seg = _ropeSegments[i];
            RopeSegment nextSeg = _ropeSegments[i + 1];
            
            Vector3 delta = nextSeg.posNow - seg.posNow;
            float dist = delta.magnitude;
            
            if (dist > 0.0001f)
            {
                Vector3 correction = delta.normalized * (dist - _segmentLength);
                seg.posNow += correction * 0.5f;
                nextSeg.posNow -= correction * 0.5f;
                
                _ropeSegments[i] = seg;
                _ropeSegments[i + 1] = nextSeg;
            }
        }

        int half = _numOfSegments / 2;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 1; j < _radiusRopeSegments; j++)
            {
                int index1 = i * half + j;
                int index2 = (i + 1) * half - j;
                RopeSegment firstHalfSeg = _ropeSegments[index1];
                RopeSegment secondHalfSeg = _ropeSegments[index2];
            
                float t1 = index1 / (float)(_numOfSegments - 1);
                float t2 = index2 / (float)(_numOfSegments - 1);
                float angle1 = t1 * 2f * Mathf.PI;
                float angle2 = t2 * 2f * Mathf.PI;
            
                // Base ellipse position
                Vector3 ellipsePos1 = new Vector3(
                    a * Mathf.Cos(angle1), 
                    b * Mathf.Sin(angle1), 
                    0f
                );
                ellipsePos1 = rot * ellipsePos1 + center;
                
                Vector3 ellipsePos2 = new Vector3(
                    a * Mathf.Cos(angle2), 
                    b * Mathf.Sin(angle2), 
                    0f
                );
                ellipsePos2 = rot * ellipsePos2 + center;
                
                firstHalfSeg.posNow = ellipsePos1;
                _ropeSegments[index1] = firstHalfSeg;
                
                secondHalfSeg.posNow = ellipsePos2;
                _ropeSegments[index2] = secondHalfSeg;
            }   
        }
        
        // Pin the endpoints to create a closed loop
        _ropeSegments[0].posNow = B;
        _ropeSegments[_numOfSegments/2].posNow = A;
        _ropeSegments[_numOfSegments - 1].posNow = B;
    }

    void HandleCollisions()
    {
        for (int i = 0; i < _ropeSegments.Length; i++)
        {
            RopeSegment segment = _ropeSegments[i];
            Vector3 velocity = segment.posNow - segment.posOld;

            Collider[] hits = Physics.OverlapSphere(segment.posNow, _collisionRadius, _collisionLayerMask);
            foreach (Collider hit in hits)
            {
                Vector3 closestPoint = hit.ClosestPoint(segment.posNow);
                float dist = Vector3.Distance(segment.posNow, closestPoint);

                if (dist < _collisionRadius)
                {
                    Vector3 collisionNormal = (segment.posNow - closestPoint).normalized;

                    // If the segment is exactly on a plane or zero distance
                    if (collisionNormal == Vector3.zero)
                    {
                        collisionNormal = Vector3.ProjectOnPlane(velocity, hit.transform.up).normalized;
                        if (collisionNormal == Vector3.zero)
                            collisionNormal = hit.transform.up;
                    }

                    float depth = _collisionRadius - dist;

                    segment.posNow += collisionNormal * depth;

                    Vector3 velocityTangent = Vector3.ProjectOnPlane(velocity, collisionNormal); // slide along surface

                    // Keep only tangential velocity (no bouncing into plane)
                    velocity = velocityTangent * (1f - _elasticity);
                }
            }

            // Update old position for Verlet integration
            segment.posOld = segment.posNow - velocity;
            _ropeSegments[i] = segment;
        }
    }



    private void OnDrawGizmos()
    {
        if (_startPoint.IsUnityNull()) return;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_startPoint.position, 0.1f);
        if (_endPoint.IsUnityNull()) return;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_endPoint.position, 0.1f);
    }
}
