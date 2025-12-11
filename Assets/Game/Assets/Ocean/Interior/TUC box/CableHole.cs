using System.Net;
using Unity.VisualScripting;
using UnityEngine;

public class CableHole : MonoBehaviour
{
    [Header("Anchor Settings")]
    [Tooltip("Kinematic Rigidbody used as the hole anchor point")]
    public Rigidbody anchorRigidbody;

    [Header("Auto-Attach Settings")]
    [Tooltip("How close (in meters) the cable head must be to attach automatically.")]
    //to przez 10
    public float attachDistance = 0.05f;
    [Tooltip("How strong the connection is before breaking.")]
    public float attachBreakForce = 200f;
    [Tooltip("Time before reattaching again after detaching (sec).")]
    public float reattachCooldown = 0.5f;

    private float lastDetachTime;
    private FixedJoint currentJoint;

    private LogicPort logicPort;

    void Start()
    {
        // Make a default kinematic anchor if none exists
        if (anchorRigidbody == null)
        {
            var anchorObj = new GameObject("HoleAnchor");
            anchorObj.transform.SetParent(transform);
            anchorObj.transform.localPosition = Vector3.zero;

            anchorRigidbody = anchorObj.AddComponent<Rigidbody>();
            anchorRigidbody.isKinematic = true;
            //Debug.Log("no anchor");
        }
        else
        {
            //Debug.Log("yes anchor");
        }
        // Make sure this object has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = attachDistance;
            //Debug.Log("no collider");

        }
        else
        {
            col.isTrigger = true;
            //Debug.Log("yes collider");

        }
        logicPort = gameObject.GetComponent<LogicPort>();
    }

    private void Update()
    {
        logicPort.UpdateVisual();
    }
    void OnTriggerStay(Collider other)
    {
        // If already attached, skip
        //Debug.Log("1");


        if (currentJoint != null) return;
        //Debug.Log("2");


        if (Time.time < lastDetachTime + reattachCooldown) return;
        //Debug.Log("3");

        // Find a cable segment (preferably the head)
        var seg = other.GetComponent<CableSegment>();
        if (seg == null) return;
        //Debug.Log("4");

        // Optional: only attach if it's near the anchor
        float dist = Vector3.Distance(other.transform.position, anchorRigidbody.position);
        if (dist > attachDistance) return;
        //Debug.Log("5");

        // Attach automatically
        AttachCable(seg.GetComponent<Rigidbody>());
    }

    void AttachCable(Rigidbody rb)
    {
        if (rb == null) return;

        FixedJoint fj = rb.gameObject.AddComponent<FixedJoint>();
        fj.connectedBody = anchorRigidbody;
        fj.breakForce = attachBreakForce;
        fj.breakTorque = attachBreakForce * 0.5f;

        currentJoint = fj;

        // Optional: move it precisely to the hole
        rb.position = anchorRigidbody.position;
        rb.linearVelocity = Vector3.zero;

        // Optional feedback
        //Debug.Log($"Cable attached to {name}");
        //if (currentJoint != null)
        //    Debug.Log("jest kabel");
        //else
        //    Debug.Log("nie ma go");


        GameObject cable = rb.transform.parent.gameObject;
        //Debug.Log("Parent name: " + cable.name);
        CableSpawner cableSpawner = cable.GetComponent<CableSpawner>();
        cableSpawner.attachHole(logicPort);




    }

    void OnJointBreak(float breakForce)
    {
        // Unity will call this on the object that had the joint when it breaks
        // We detect detachment here
        lastDetachTime = Time.time;
        currentJoint = null;

        // Optional: feedback
        //Debug.Log($"Cable detached from {name}");
    }

    void OnTriggerExit(Collider other)
    {
        // (Optional) If you want it to detach when pulled away gently
        // You can destroy the joint here if distance > threshold, but
        // the breakForce is usually enough.
        currentJoint = null;

        //Debug.Log($"Cable detached from {name}");
        //if (currentJoint != null)
        //    Debug.Log("jest kabel");
        //else
        //    Debug.Log("nie ma go");
        GameObject cable = other.transform.parent.gameObject;
        //Debug.Log("Parent name: " + cable.name);
        CableSpawner cableSpawner = cable.GetComponent<CableSpawner>();
        cableSpawner.detachHole(logicPort);

    }


    //void AttachToHole(Transform hole)
    //{
    //    // existing physics snapping logic...
    //    transform.position = hole.position;
    //    transform.parent = hole;

    //    var port = hole.GetComponent<LogicPort>();
    //    if (port != null)
    //    {
    //        if (isStartEnd)
    //            startPort = port;
    //        else
    //            endPort = port;

    //        TryLinkPorts();
    //    }
    //}

    //void TryLinkPorts()
    //{
    //    if (startPort != null && endPort != null)
    //    {
    //        // Only allow Output -> Input connection
    //        if (startPort.portType == PortType.Output && endPort.portType == PortType.Input)
    //        {
    //            startPort.connectedPort = endPort;
    //            Debug.Log($"Connected {startPort.portID} -> {endPort.portID}");
    //        }
    //        else if (endPort.portType == PortType.Output && startPort.portType == PortType.Input)
    //        {
    //            endPort.connectedPort = startPort;
    //            Debug.Log($"Connected {endPort.portID} -> {startPort.portID}");
    //        }
    //    }
    //}
}