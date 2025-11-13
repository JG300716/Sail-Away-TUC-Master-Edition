using UnityEngine;

[CreateAssetMenu(fileName = "SailSkin", menuName = "Scriptable Objects/SailSkin")]
public class SailSkin : ScriptableObject
{
    public new string name;
    public Color color;
    public Sprite skinImg;
}
