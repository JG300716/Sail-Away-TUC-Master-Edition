using UnityEngine;
using Game.Scripts.Interface;

namespace Game.Scripts
{

    public class ObjectHighlightManager : SingletonInterface<ObjectHighlightManager>
    {
        [Header("Highlight Settings")] [SerializeField]
        private Material highlightMaterial; // Outline shader material

        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private float highlightThickness = 0.05f;

        private GameObject currentHighlightedObject;
        private Renderer currentRenderer;
        private Material[] originalMaterials;
        private Material highlightMaterialFront;
        private Material highlightMaterialBack;

        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int ThicknessProperty = Shader.PropertyToID("_Thickness");
        private static readonly int CullModeProperty = Shader.PropertyToID("_CullMode");

        private void Start()
        {
            if (highlightMaterial != null)
            {
                // Stwórz dwie instancje materiału
                highlightMaterialFront = new Material(highlightMaterial);
                highlightMaterialBack = new Material(highlightMaterial);

                // Ustaw Cull Mode - Front renderuje tylną stronę (outline zewnętrzny)
                // Back renderuje przednią stronę (outline wewnętrzny)
                highlightMaterialFront.SetFloat(CullModeProperty, 1); // Cull Front
                highlightMaterialBack.SetFloat(CullModeProperty, 2); // Cull Back
            }
        }

        public static void HighlightObject(GameObject obj)
        {
            if (obj == null) return;
            if (obj == Instance.currentHighlightedObject) return;

            Instance.RemoveHighlight();
            Instance.ApplyHighlight(obj);
        }

        public void RemoveHighlight()
        {
            if (currentHighlightedObject != null && currentRenderer != null && originalMaterials != null)
            {
                currentRenderer.materials = originalMaterials;

                currentHighlightedObject = null;
                currentRenderer = null;
                originalMaterials = null;
            }
        }

        public bool IsHighlighted(GameObject obj)
        {
            return currentHighlightedObject == obj;
        }

        public GameObject GetCurrentHighlightedObject()
        {
            return currentHighlightedObject;
        }

        private void ApplyHighlight(GameObject obj)
        {
            currentHighlightedObject = obj;
            currentRenderer = obj.GetComponent<Renderer>();

            if (currentRenderer != null && highlightMaterialFront != null && highlightMaterialBack != null)
            {
                originalMaterials = currentRenderer.sharedMaterials;

                // Ustaw właściwości dla obu materiałów
                highlightMaterialFront.SetColor(ColorProperty, highlightColor);
                highlightMaterialFront.SetFloat(ThicknessProperty, highlightThickness);

                highlightMaterialBack.SetColor(ColorProperty, highlightColor);
                highlightMaterialBack.SetFloat(ThicknessProperty, highlightThickness);

                // Dodaj oba materiały outline (front i back cull)
                Material[] newMaterials = new Material[originalMaterials.Length + 2];
                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    newMaterials[i] = originalMaterials[i];
                }

                newMaterials[originalMaterials.Length] = highlightMaterialFront;
                newMaterials[originalMaterials.Length + 1] = highlightMaterialBack;

                currentRenderer.materials = newMaterials;
            }
        }

        public void SetHighlightColor(Color color)
        {
            highlightColor = color;
            if (highlightMaterialFront != null)
            {
                highlightMaterialFront.SetColor(ColorProperty, color);
            }

            if (highlightMaterialBack != null)
            {
                highlightMaterialBack.SetColor(ColorProperty, color);
            }
        }

        public void SetHighlightThickness(float thickness)
        {
            highlightThickness = thickness;
            if (highlightMaterialFront != null)
            {
                highlightMaterialFront.SetFloat(ThicknessProperty, thickness);
            }

            if (highlightMaterialBack != null)
            {
                highlightMaterialBack.SetFloat(ThicknessProperty, thickness);
            }
        }

        private void OnDestroy()
        {
            RemoveHighlight();

            if (highlightMaterialFront != null)
            {
                Destroy(highlightMaterialFront);
            }

            if (highlightMaterialBack != null)
            {
                Destroy(highlightMaterialBack);
            }
        }
    }

}