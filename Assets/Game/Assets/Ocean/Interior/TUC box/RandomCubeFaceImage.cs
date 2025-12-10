using UnityEngine;

public class RandomCubeFaceImage : MonoBehaviour
{
    [Header("Textures to choose from")]
    public Texture[] images = new Texture[4];

    [Header("Material of the cube face")]
    public Renderer targetRenderer; // assign the face's renderer
    public int materialIndex = 0;    // index of material on that face (0 if only one)

    void Start()
    {
        ApplyRandomImage();
    }

    public void ApplyRandomImage()
    {
        if (images == null || images.Length == 0)
        {
            Debug.LogWarning("No images assigned!");
            return;
        }

        int randomIndex = Random.Range(0, images.Length);
        Texture chosen = images[randomIndex];

        // Get material instance
        Material mat = targetRenderer.materials[materialIndex];
        mat.mainTexture = chosen;
    }
}