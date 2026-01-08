using System.Collections.Generic;
using Game.Scripts;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class CircuitChecker : MonoBehaviour
{
    [Header("Gates")]

    public AndGateLogic[] andGates;

    public OrGateLogic[] orGates;

    [Header("Textures to choose from")]
    public Texture[] images = new Texture[4];

    [Header("Material of the cube face")]
    public Renderer targetRenderer; // assign the face's renderer
    public int materialIndex = 0;    // index of material on that face (0 if only one)

    private int circuit_number;

    public bool circuitGood = false;

    public Material red;
    public Material green;

    public Renderer faze1;
    public Renderer faze2;
    public Renderer faze3;

    void Start()
    {
        
        circuit_number = Random.Range(0, 4);
        ApplyRandomImage();
    }

    public void ApplyRandomImage()
    {
        if (images == null || images.Length == 0)
        {
            Debug.LogWarning("No images assigned!");
            return;
        }
        Texture chosen = images[circuit_number];

        // Get material instance
        Material mat = targetRenderer.materials[materialIndex];
        mat.mainTexture = chosen;
    }

    void Update()
    {
        if (CheckWin() && !circuitGood)
        {
            Debug.Log("WYGRANA");
            GameManager.Instance.TucSolved();
            circuitGood = true;
        }
    }

    bool CheckWin()
    {
        faze1.material.color = Color.red;
        faze2.material.color = Color.red;
        faze3.material.color = Color.red;
        bool first_good = false;
        bool second_good = false;
        bool third_good = false;

        if (circuit_number == 0)
        {
            //Debug.Log($"chosen {circuit_number}");
            //Debug.Log($"Nr of gates {orGates.Length}");


            foreach (OrGateLogic orGate in orGates)
            {
                //if (orGate.portA.connectedPort != null && orGate.portA.connectedPort.origin == PortOrigin.Source)
                //{
                //    Debug.Log("1 git");
                //}

                //if (orGate.portB.connectedPort != null && orGate.portB.connectedPort.origin == PortOrigin.Source)
                //{
                //    Debug.Log("2 git");
                //}

                //if (orGate.portOut.connectedPort != null && orGate.portOut.connectedPort.origin == PortOrigin.And)
                //{
                //    Debug.Log("3 git");
                //}

                if (!(orGate.portA.connectedPort != null && orGate.portB.connectedPort != null && orGate.portOut.connectedPort != null))
                {
                    continue;
                }
                //Debug.Log("bramka 1 moze");


                if (!(orGate.portA.connectedPort.origin == PortOrigin.Source &&
                    orGate.portB.connectedPort.origin == PortOrigin.Source &&
                    orGate.portOut.connectedPort.origin == PortOrigin.And))
                {
                    continue;
                }

                

                //Debug.Log("bramka 1 ogarnieta");

                faze1.material.color = Color.green;

                first_good = true;
            }



            foreach (AndGateLogic andGate in andGates)
            {


                //if (andGate.portA.connectedPort != null && andGate.portA.connectedPort.origin == PortOrigin.Source)
                //{
                //    Debug.Log("21 git");

                //}

                //if (andGate.portB.connectedPort != null && andGate.portB.connectedPort.origin == PortOrigin.Source)
                //{
                //    Debug.Log("22 git");

                //}

                //if (andGate.portOut.connectedPort != null && andGate.portOut.connectedPort.origin == PortOrigin.And)
                //{
                //    Debug.Log("23 git");

                //}

                if (!(andGate.portA.connectedPort != null && andGate.portB.connectedPort != null && andGate.portOut.connectedPort != null))
                {
                    continue;
                }

                //Debug.Log("bramka 2 moze");

                if (!(andGate.portA.connectedPort.origin == PortOrigin.Source &&
                    andGate.portB.connectedPort.origin == PortOrigin.Source &&
                    andGate.portOut.connectedPort.origin == PortOrigin.And))
                {
                    continue;
                }
                //Debug.Log("bramka 2 ogarnieta");


                faze2.material.color = Color.green;

                second_good = true;
                
            }


            foreach (AndGateLogic andGate in andGates)
            {
                
                

                if (!(andGate.portA.connectedPort != null && andGate.portB.connectedPort != null))
                {
                    continue;
                }
                if (!((andGate.portA.connectedPort.origin == PortOrigin.And &&
                    andGate.portB.connectedPort.origin == PortOrigin.Or) ||
                    (andGate.portB.connectedPort.origin == PortOrigin.And &&
                    andGate.portA.connectedPort.origin == PortOrigin.Or)))
                {
                    continue;
                }

                faze3.material.color = Color.green;

                third_good = true;

            }

            if (first_good && second_good && third_good)
            {
                return true;
            }
            return false;
        }
        if (circuit_number == 1)
        {
            foreach (OrGateLogic orGate in orGates)
            {
                if (!(orGate.portA.connectedPort != null && orGate.portB.connectedPort != null && orGate.portOut.connectedPort != null))
                {
                    continue;
                }
                if (!(orGate.portA.connectedPort.origin == PortOrigin.Source &&
                    orGate.portB.connectedPort.origin == PortOrigin.Source &&
                    orGate.portOut.connectedPort.origin == PortOrigin.And))
                {
                    continue;
                }

                faze1.material.color = Color.green;
                first_good = true;
                
            }

            foreach (OrGateLogic orGate2 in orGates)
            {
                
                if (!(orGate2.portA.connectedPort != null && orGate2.portB.connectedPort != null && orGate2.portOut.connectedPort != null))
                {
                    continue;
                }
                if (!(orGate2.portA.connectedPort.origin == PortOrigin.Source &&
                    orGate2.portB.connectedPort.origin == PortOrigin.Source &&
                    orGate2.portOut.connectedPort.origin == PortOrigin.And))
                {
                    continue;
                }
                faze2.material.color = Color.green;

                second_good = true;
            }

            foreach (AndGateLogic andGate in andGates)
            {

                if (!(andGate.portA.connectedPort != null && andGate.portB.connectedPort != null))
                {
                    continue;
                }
                if (!(andGate.portA.connectedPort.origin == PortOrigin.Or &&
                    andGate.portB.connectedPort.origin == PortOrigin.Or))
                {
                    continue;
                }
                faze3.material.color = Color.green;
                third_good = true;
            }

            if (first_good && second_good && third_good)
            {
                return true;
            }
            return false;
        }
        if (circuit_number == 2)
        {
            foreach (OrGateLogic orGate in orGates)
            {
                if (!(orGate.portA.connectedPort != null && orGate.portB.connectedPort != null && orGate.portOut.connectedPort != null))
                {
                    continue;
                }
                if (!(orGate.portA.connectedPort.origin == PortOrigin.Source &&
                    orGate.portB.connectedPort.origin == PortOrigin.Source &&
                    orGate.portOut.connectedPort.origin == PortOrigin.And))
                {
                    continue;
                }

                faze1.material.color = Color.green;

                first_good = true;

            }

            foreach (AndGateLogic andGate in andGates)
            {

                if (!(andGate.portA.connectedPort != null && andGate.portB.connectedPort != null && andGate.portOut.connectedPort != null))
                {
                    continue;
                }
                if (!((andGate.portA.connectedPort.origin == PortOrigin.Source &&
                    andGate.portB.connectedPort.origin == PortOrigin.Or &&
                    andGate.portOut.connectedPort.origin == PortOrigin.Or) ||
                    (andGate.portB.connectedPort.origin == PortOrigin.Source &&
                    andGate.portA.connectedPort.origin == PortOrigin.Or &&
                    andGate.portOut.connectedPort.origin == PortOrigin.Or)))
                {
                    continue;
                }
                faze2.material.color = Color.green;

                second_good = true;
                
            }

            foreach (OrGateLogic orGate2 in orGates)
            {
                
                if (!(orGate2.portA.connectedPort != null && orGate2.portB.connectedPort != null))
                {
                    continue;
                }
                if (!((orGate2.portA.connectedPort.origin == PortOrigin.Source &&
                    orGate2.portB.connectedPort.origin == PortOrigin.And) ||
                    (orGate2.portB.connectedPort.origin == PortOrigin.Source &&
                    orGate2.portA.connectedPort.origin == PortOrigin.And)))
                {
                    continue;
                }
                faze3.material.color = Color.green;

                third_good = true;
            }

            if (first_good && second_good && third_good)
            {
                return true;
            }
            return false;
        }
        if (circuit_number == 3)
        {
            foreach (AndGateLogic andGate in andGates)
            {
                if (!(andGate.portA.connectedPort != null && andGate.portB.connectedPort != null && andGate.portOut.connectedPort != null))
                {
                    continue;
                }
                if (!(andGate.portA.connectedPort.origin == PortOrigin.Source &&
                    andGate.portB.connectedPort.origin == PortOrigin.Source &&
                    andGate.portOut.connectedPort.origin == PortOrigin.And))
                {
                    continue;
                }
                faze1.material.color = Color.green;

                first_good = true;
                

            }

            foreach (AndGateLogic andGate in andGates)
            {
                
                if (!(andGate.portA.connectedPort != null && andGate.portB.connectedPort != null && andGate.portOut.connectedPort != null))
                {
                    continue;
                }
                if (!((andGate.portA.connectedPort.origin == PortOrigin.And &&
                    andGate.portB.connectedPort.origin == PortOrigin.Source &&
                    andGate.portOut.connectedPort.origin == PortOrigin.Or) ||
                    (andGate.portB.connectedPort.origin == PortOrigin.And &&
                    andGate.portA.connectedPort.origin == PortOrigin.Source &&
                    andGate.portOut.connectedPort.origin == PortOrigin.Or)))
                {
                    continue;
                }
                faze2.material.color = Color.green;
                second_good = true;
                
            }

            foreach (OrGateLogic orGate in orGates)
            {
                if (!(orGate.portA.connectedPort != null && orGate.portB.connectedPort != null))
                {
                    continue;
                }
                if (!((orGate.portA.connectedPort.origin == PortOrigin.Source &&
                    orGate.portB.connectedPort.origin == PortOrigin.And) ||
                    (orGate.portB.connectedPort.origin == PortOrigin.Source &&
                    orGate.portA.connectedPort.origin == PortOrigin.And)))
                {
                    continue;
                }
                faze3.material.color = Color.green;
                third_good = true;
            }

            if (first_good && second_good && third_good)
            {
                return true;
            }
            return false;
        }


        return false;
    }

}



    