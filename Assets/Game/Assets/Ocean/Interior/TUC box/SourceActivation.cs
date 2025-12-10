using UnityEngine;

public class SourceActivation : MonoBehaviour
{
    [Header("Objects with the 'signal' bool")]
    public SourceLogic[] objects = new SourceLogic[4];

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            Toggle(0);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            Toggle(1);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            Toggle(2);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            Toggle(3);
    }

    void Toggle(int index)
    {
        if (objects[index] != null)
        {
            objects[index].signal = !objects[index].signal;
            Debug.Log($"Object {index + 1} signal = {objects[index].signal}");
        }
    }
}