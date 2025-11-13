using Game.Scripts;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

namespace Game.Scripts
{
    public class WindCompassUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private YachtState yachtState;
        [SerializeField] private YachtPhysics yachtPhysics;

        [Header("UI Elements")]
        [SerializeField] private RectTransform compassCircle; // okrąg kompasu
        [SerializeField] private RectTransform windIndicator; // ikona wiatru
        [SerializeField] private Image windIndicatorImage; // obraz ikony wiatru
        [SerializeField] private RectTransform boatIcon; // ikona łódki (środek)

        [Header("Wind Indicator Sprites")]
        [SerializeField] private Sprite windSlow; // wiatr słaby / cofanie
        [SerializeField] private Sprite windNormal; // wiatr normalny
        [SerializeField] private Sprite windFast; // wiatr silny

        [Header("Settings")]
        [SerializeField] private float compassRadius = 150f; // promień kompasu
        [SerializeField] private float slowSpeedThreshold = 0.5f; // próg wolnej prędkości
        [SerializeField] private float fastSpeedThreshold = 5.0f; // próg szybkiej prędkości
        [SerializeField] private Color slowColor = Color.red;
        [SerializeField] private Color normalColor = Color.yellow;
        [SerializeField] private Color fastColor = Color.green;

        private bool initialized = false;
        private WindManager Wind => WindManager.Instance;

        void Start()
        {
            initialized = yachtState != null && Wind != null && yachtPhysics != null;
            
            if (!initialized)
            {
                Debug.LogError("WindCompassUI: Missing references!");
                return;
            }

            // Opcjonalne: ustaw ikonę łódki w środku
            if (boatIcon != null)
            {
                boatIcon.anchoredPosition = Vector2.zero;
            }
        }

        void Update()
        {
            if (!initialized) return;

            UpdateWindIndicator();
        }

        private void UpdateWindIndicator()
        {
            // Oblicz względny kąt wiatru (od 0° do 360°)
            double relativeWindAngle = (Wind.WindDegree - yachtState.Deg_from_north + 270.0) % 360.0;

            // Pozycja ikony wiatru na okręgu
            if (!windIndicator.IsUnityNull())
            {
                float angleRad = (float)relativeWindAngle * Mathf.Deg2Rad;
                Vector2 position = new Vector2(
                    Mathf.Sin(angleRad) * compassRadius,
                    Mathf.Cos(angleRad) * compassRadius
                );
                windIndicator.anchoredPosition = position;

                // Rotacja ikony wiatru (wskazuje kierunek wiatru)
                windIndicator.rotation = Quaternion.Euler(0, 0, -(float)relativeWindAngle);
            }

            // Zmiana ikony i koloru w zależności od przyspieszenia/prędkości
            UpdateWindIndicatorAppearance();

        }

        private void UpdateWindIndicatorAppearance()
        {
            var acceleration = yachtState.Acceleration;
            var speed = yachtState.V_current;

            var selectedSprite = windNormal;
            var selectedColor = normalColor;

            // Cofanie lub bardzo wolno
            if (speed < slowSpeedThreshold || acceleration < -0.1)
            {
                selectedSprite = windSlow;
                selectedColor = slowColor;
            }
            // Szybko
            else if (speed > fastSpeedThreshold && acceleration > 0.1)
            {
                selectedSprite = windFast;
                selectedColor = fastColor;
            }
            // Normalna prędkość
            else
            {
                selectedSprite = windNormal;
                selectedColor = normalColor;
            }

            if (windIndicatorImage.IsUnityNull()) return;
            windIndicatorImage.color = selectedColor;
            if (!selectedSprite.IsUnityNull()) windIndicatorImage.sprite = selectedSprite;
        }
        
        // Funkcja pomocnicza do rysowania kompasu w edytorze
        
    }
}