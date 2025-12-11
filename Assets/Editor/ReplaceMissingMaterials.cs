using UnityEditor;
using UnityEngine;

public class ReplaceMissingMaterials
{
    [MenuItem("Tools/Replace Missing Materials With HDRP Lit")]
    static void Replace()
    {
        var selected = Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogWarning("No GameObject selected!");
            return;
        }

        var renderers = selected.GetComponentsInChildren<Renderer>(true);

        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null)
                {
                    mats[i] = new Material(Shader.Find("HDRP/Lit"));
                }
            }
            r.sharedMaterials = mats;
        }

        Debug.Log("Missing materials replaced with HDRP/Lit.");
    }
}
