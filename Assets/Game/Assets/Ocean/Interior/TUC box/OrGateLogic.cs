using UnityEngine;

public class OrGateLogic : MonoBehaviour
{
    public GameObject inputA;
    public GameObject inputB;
    public GameObject output;

    public LogicPort portA;
    public LogicPort portB;
    public LogicPort portOut;

    private void Start()
    {
        portA = inputA.GetComponent<LogicPort>();
        portB = inputB.GetComponent<LogicPort>();
        portOut = output.GetComponent<LogicPort>();

    }
    void Update()
    {
        if (portA != null && portB != null && portOut != null)
        {
            bool result = portA.signal || portB.signal;
            portOut.SetSignal(result);
        }
    }
}
