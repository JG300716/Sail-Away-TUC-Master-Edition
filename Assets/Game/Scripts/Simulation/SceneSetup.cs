using UnityEngine;

public class SceneSetup : MonoBehaviour
{
    [Header("Scene Settings")]
    public Color backgroundColor = new Color(0.2f, 0.4f, 0.6f);
    public float cameraSize = 10f;
    
    void Start()
    {
        SetupCamera();
        SetupBackground();
    }
    
    void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = cameraSize;
            mainCamera.transform.position = new Vector3(0, 50, 0);
            mainCamera.backgroundColor = backgroundColor;
        }
    }
    
    void SetupBackground()
    {
        // Stworzenie prostego tła wody
        GameObject background = new GameObject("WaterBackground");
        SpriteRenderer sr = background.AddComponent<SpriteRenderer>();
        sr.sprite = CreateWaterTexture();
        sr.sortingOrder = -10;
        
        // Skalowanie tła do pokrycia widoku
        background.transform.localScale = new Vector3(2, 2, 1);
        background.transform.rotation = Quaternion.Euler(90, 0, 0);
        background.transform.position = Vector3.zero;
    }
    
    Sprite CreateWaterTexture()
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size);
        
        Color waterColor1 = new Color(0.2f, 0.4f, 0.7f);
        Color waterColor2 = new Color(0.15f, 0.35f, 0.65f);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Prosty wzór fal
                float wave = Mathf.Sin((x + y) * 0.1f) * 0.5f + 0.5f;
                Color color = Color.Lerp(waterColor1, waterColor2, wave);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), 
            new Vector2(0.5f, 0.5f), 10);
    }
}
