using UnityEngine;

public class SourceLogic : MonoBehaviour
{

    public GameObject output;

    public bool signal;
    public LogicPort portOut;

    private void Start()
    {

        portOut = output.GetComponent<LogicPort>();

        portOut.origin = PortOrigin.Source;

    }
    void Update()
    {
        if(portOut == null) return;
        portOut.SetSignal(signal);

    }
}
