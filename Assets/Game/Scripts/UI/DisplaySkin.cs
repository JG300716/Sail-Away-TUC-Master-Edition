using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DisplaySkin : MonoBehaviour
{
    public SailSkin skin;
    public TextMeshProUGUI nameText;
    public RawImage spriteImage;
    public Image color;

    void Start()
    {
        nameText.text = skin.name;
        spriteImage.texture = skin.skinImg.texture;
        color.color = skin.color;
    }

}
