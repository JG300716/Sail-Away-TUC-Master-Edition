//using System.Collections.Generic;
//using System.Net;
//using UnityEngine;
//using UnityEngine.InputSystem;

//public class CableSpawner : MonoBehaviour
//{
//    [Header("Prefab & settings")]
//    public GameObject segmentPrefab;   // CableSegmentPrefab (must have Rigidbody + Collider)
//    public Transform startPoint;       // world position to spawn from
//    public int minSegments = 15;
//    public int maxSegments = 30;
//    public float segmentSpacing = 0.08f;
//    public float spring = 800f;        // joint spring
//    public float damper = 60f;         // joint damper
//    public bool makeHeadKinematicWhileGrabbing = false;

//    [Header("LineRenderer")]
//    public LineRenderer lineRenderer; // assign in inspector or created in code

//    [Header("Cable Colors")]
//    public Material[] cableMaterials; // assign 6 materials in inspector


//    private List<GameObject> cables = new List<GameObject>();


//    void Start()
//    {
//        if (segmentPrefab == null || startPoint == null) return;
//        CreateCable();
//    }

//    public void CreateCable()
//    {
//        GameObject prev = null;
//        int segmentCount = Random.Range(minSegments, maxSegments + 1);
//        Material chosenMat = cableMaterials[Random.Range(0, cableMaterials.Length)];
//        lineRenderer.material = chosenMat;

//        GameObject cableObj = new GameObject("Cable_" + cables.Count);
//        cableObj.transform.parent = transform;
//        cables.Add(cableObj);

//        List<GameObject> segments = new List<GameObject>();
//        //Rigidbody prevRb = null;
//        //Vector3 dir = (endPoint.position - startPoint.position).normalized;
//        //Vector3 pos = startPoint.position;
//        for (int i = 0; i < segmentCount; i++)
//        {
//            Vector3 pos = startPoint.position + Vector3.down * segmentSpacing * i;
//            GameObject s = Instantiate(segmentPrefab, pos, Quaternion.identity, transform);
//            s.name = "CableSegment_" + i;
//            Rigidbody rb = s.GetComponent<Rigidbody>();
//            if (rb == null) rb = s.AddComponent<Rigidbody>();

//            // recommended Rigidbody settings (you can set in prefab too)
//            rb.mass = 0.00001f;
//            rb.interpolation = RigidbodyInterpolation.Interpolate;
//            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

//            // mark head
//            var segComp = s.GetComponent<CableSegment>();
//            if (segComp == null) segComp = s.AddComponent<CableSegment>();
//            segComp.index = i;
//            segComp.isHead = (i == segmentCount - 1);

//            // connect to previous segment with SpringJoint
//            if (prev != null)
//            {
//                SpringJoint sj = s.AddComponent<SpringJoint>();
//                sj.connectedBody = prev.GetComponent<Rigidbody>();
//                sj.autoConfigureConnectedAnchor = true;
//                sj.anchor = Vector3.zero;
//                sj.connectedAnchor = Vector3.zero;
//                sj.spring = spring;
//                sj.damper = damper;
//                sj.maxDistance = segmentSpacing * 0.002f;
//                sj.minDistance = segmentSpacing * 0.001f;


//            }

//            segments.Add(s);

//            // optional: avoid immediate self-collision with previous segment to reduce jitter
//            Collider a = s.GetComponent<Collider>();
//            Collider b = prev != null ? prev.GetComponent<Collider>() : null;
//            if (a != null && b != null) Physics.IgnoreCollision(a, b, false);

//            prev = s;


//        }


//        // --- Add a LineRenderer to the cable container ---
//        LineRenderer lr = cableObj.AddComponent<LineRenderer>();
//        lr.positionCount = segmentCount;
//        lr.material = chosenMat;
//        lr.startWidth = lineWidth;
//        lr.endWidth = lineWidth;
//        lr.useWorldSpace = true;

//        // Start coroutine to update LineRenderer positions
//        StartCoroutine(UpdateLineRenderer(lr, segments));


//        // --- Create a container for this cable ---





//        // --- Spawn physical segments ---
//        for (int i = 0; i < segmentCount; i++)
//        {
//            GameObject seg = Instantiate(segmentPrefab, pos, Quaternion.identity, cableObj.transform);
//            segments.Add(seg.transform);

//            Rigidbody rb = seg.GetComponent<Rigidbody>();
//            SpringJoint sj = seg.GetComponent<SpringJoint>();

//            if (prevRb != null && sj != null)
//            {
//                sj.connectedBody = prevRb;
//                sj.minDistance = segmentLength * 0.95f;
//                sj.maxDistance = segmentLength * 1.0f;
//                sj.spring = 4000f;
//                sj.damper = 80f;
//                sj.enableCollision = true;
//            }

//            prevRb = rb;
//            pos += dir * segmentLength;
//        }




//    }
//    void Update()
//    {
//        // --- Press Space to generate a new random cable ---
//        if (Keyboard.current.spaceKey.wasPressedThisFrame)
//        {

//            CreateCable();
//        }
//    }

//    // --- Coroutine to update LineRenderer positions ---
//    System.Collections.IEnumerator UpdateLineRenderer(LineRenderer lr, List<Transform> segments)
//    {
//        while (lr != null && segments.Count > 0)
//        {
//            for (int i = 0; i < segments.Count; i++)
//            {
//                if (segments[i] != null)
//                    lr.SetPosition(i, segments[i].position);
//            }
//            yield return new WaitForFixedUpdate();
//        }
//    }
//}















using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CableSpawner : MonoBehaviour
{
    [Header("Prefab & settings")]
    public GameObject segmentPrefab;   // CableSegmentPrefab (must have Rigidbody + Collider)
    public Transform startPoint;       // world position to spawn from
    public int minSegments = 15;
    public int maxSegments = 30;
    //to przez 10
    public float segmentSpacing = 0.08f;
    public float spring = 800f;        // joint spring
    public float damper = 60f;         // joint damper
    public bool makeHeadKinematicWhileGrabbing = false;

    [Header("LineRenderer")]
    public LineRenderer lineRenderer; // assign in inspector or created in code

    [Header("Cable Colors")]
    public Material[] cableMaterials; // assign 6 materials in inspector

    [HideInInspector] public List<GameObject> segments = new List<GameObject>();

    public LogicPort hole1;
    public LogicPort hole2;

    private bool work;

    void Start()
    {
        if (segmentPrefab == null || startPoint == null) return;
        CreateCable();
        work = (Random.Range(0, 10) == 0);
  

    }

    public void CreateCable()
    {
        segments.Clear();
        GameObject prev = null;
        int segmentCount = Random.Range(minSegments, maxSegments + 1);
        Material chosenMat = cableMaterials[Random.Range(0, cableMaterials.Length)];
        lineRenderer.material = chosenMat;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 pos = startPoint.position + Vector3.down * segmentSpacing * i;
            GameObject s = Instantiate(segmentPrefab, pos, Quaternion.identity, transform);
            s.name = "CableSegment_" + i;
            Rigidbody rb = s.GetComponent<Rigidbody>();
            if (rb == null) rb = s.AddComponent<Rigidbody>();

            // recommended Rigidbody settings (you can set in prefab too)
            
            //to przez 10   
            rb.mass = 0.000001f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // mark head
            var segComp = s.GetComponent<CableSegment>();
            if (segComp == null) segComp = s.AddComponent<CableSegment>();
            segComp.index = i;
            segComp.isHead = (i == segmentCount - 1);

            // connect to previous segment with SpringJoint
            if (prev != null)
            {
                SpringJoint sj = s.AddComponent<SpringJoint>();
                sj.connectedBody = prev.GetComponent<Rigidbody>();
                sj.autoConfigureConnectedAnchor = true;
                sj.anchor = Vector3.zero;
                sj.connectedAnchor = Vector3.zero;
                sj.spring = spring;
                sj.damper = damper;
                sj.maxDistance = segmentSpacing * 0.002f;
                sj.minDistance = segmentSpacing * 0.001f;


            }

            segments.Add(s);

            // optional: avoid immediate self-collision with previous segment to reduce jitter
            Collider a = s.GetComponent<Collider>();
            Collider b = prev != null ? prev.GetComponent<Collider>() : null;
            if (a != null && b != null) Physics.IgnoreCollision(a, b, false);

            prev = s;
        }






    }
    void Update()
    {
        // --- Press Space to generate a new random cable ---
        //if (Keyboard.current.spaceKey.wasPressedThisFrame)
        //{

        //    CreateCable();
        //}
    }

    public void attachHole(LogicPort port)
    {
        if(hole1 == null && hole2 != port)
        {
            hole1 = port;
            Debug.Log("first hole");
            LinkPorts();
            return;
        }
        if (hole2 == null && hole1 != port)
        {
            hole2 = port;
            Debug.Log("second hole");
            LinkPorts();
            return;
        }
    }

    public void detachHole(LogicPort port)
    {
        if (hole1 == port)
        {
            unLinkPorts();
            
            hole1.SetSignal(false);
            if(hole2 != null)
            {
                hole2.SetSignal(false);
            }
            hole1 = null;
            Debug.Log("detached first hole");
            return;
        }
        if (hole2 == port)
        {
            unLinkPorts();
            if(hole1 != null)
            {
                hole1.SetSignal(false);

            }
            hole2.SetSignal(false);
            hole2 = null;
            Debug.Log("detached second hole");
            return;
        }
        hole1.SetSignal(false);
        hole1.SetSignal(false);

    }


    void LinkPorts()
    {
        if (hole1 != null && hole2 != null)
        {
            //if (!work)
            //    return;

            // Only allow Output -> Input connection
            if (hole1.portType == PortType.Output && hole2.portType == PortType.Input)
            {
                hole1.connectedPort = hole2;
                Debug.Log($"Connected {hole1.portID} -> {hole2.portID}");
            }
            else if (hole2.portType == PortType.Output && hole1.portType == PortType.Input)
            {
                hole2.connectedPort = hole1;
                Debug.Log($"Connected {hole2.portID} -> {hole1.portID}");
            }
        }
    }

    void unLinkPorts()
    {
        if (hole1 != null && hole2 != null)
        {
            // Only allow Output -> Input connection
            if (hole1.portType == PortType.Output && hole2.portType == PortType.Input)
            {
                hole1.connectedPort = null;
                Debug.Log($"Disconnected {hole1.portID} -> {hole2.portID}");
            }
            else if (hole2.portType == PortType.Output && hole1.portType == PortType.Input)
            {
                hole2.connectedPort = null;
                Debug.Log($"Disonnected {hole2.portID} -> {hole1.portID}");
            }
        }
    }
}




