using UnityEngine;

public enum PortType { Input, Output }
public enum PortOrigin { Source, Or, And}

public class LogicPort : MonoBehaviour
{
    public PortType portType;
    public PortOrigin origin;
    public bool signal; // current signal state
    public LogicPort connectedPort; // what this port is connected to
    public string portID; // optional unique name or identifier

    // For visuals
    public Renderer indicator;
    public Color offColor = Color.black;
    public Color onColor = Color.green;
    
    void Start()
    {
        if (indicator == null) indicator = GetComponent<Renderer>();
        UpdateVisual();
    }

    public void SetSignal(bool value)
    {
        signal = value;
        UpdateVisual();

        // Propagate to connected input
        if (portType == PortType.Output && connectedPort != null)
        {
            connectedPort.ReceiveSignal(value);
        }
    }
    
    public void ReceiveSignal(bool value)
    {
        signal = value;
        UpdateVisual();
    }

    public void UpdateVisual()
    { 
        if (indicator != null)
            indicator.material.color = signal ? onColor : offColor;
    }
}