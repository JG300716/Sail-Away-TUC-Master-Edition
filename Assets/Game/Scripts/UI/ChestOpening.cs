using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.UI;

namespace Game.Scripts
{

    public class ChestOpening : MonoBehaviour
    {
        [Header("UI Elements")] public RectTransform container; // Panel z Image skinów
        public List<RectTransform> skins; // Lista wszystkich skinów (15 w Twoim przypadku)
        public float spacing = 110f; // 100 szerokość skina + 10 odstęp
        public Image centerMarker; // Pionowa kreska

        [SerializeField] List<Material> skinMaterials;

        [Header("Rolling Settings")] public float startSpeed = 2000f; // Początkowa prędkość przesuwania
        public float slowDownDuration = 3f; // Czas spowolnienia
        public float minStopTime = 2f;
        public float maxStopTime = 4f;
        public float hideDelay = 2f;

        private RectTransform lastWinner;

        public bool isRolling = false;
        public bool isPaused = false;

        private void Start()
        {
            for (int i = 0; i < skins.Count; i++)
            {
                skins[i].anchoredPosition = new Vector2(i * spacing, 0);
            }
            gameObject.SetActive(false);
        }

        private IEnumerator RollSkins()
        {
            isRolling = true;
            float elapsed = 0f;
            Random.InitState(System.DateTime.Now.Millisecond);
            float stopTime = Random.Range(minStopTime, maxStopTime);
            float speed = startSpeed;

            while (elapsed < stopTime)
            {
                while (isPaused)
                    yield return null;
                
                elapsed += Time.unscaledDeltaTime;

                // Spowolnienie ease-out
                float t = Mathf.Clamp01(elapsed / stopTime);
                float currentSpeed = Mathf.Lerp(speed, 0f, t * t); // kwadratowe spowolnienie

                // Przesuwanie skinów w poziomie
                for (int i = 0; i < skins.Count; i++)
                {
                    Vector2 pos = skins[i].anchoredPosition;
                    pos.x -= currentSpeed * Time.unscaledDeltaTime;

                    // Zawijanie: jeśli skin wyszedł poza lewą krawędź
                    float totalWidth = spacing * skins.Count;
                    if (pos.x < -spacing)
                    {
                        pos.x += totalWidth;
                    }

                    skins[i].anchoredPosition = pos;
                }

                yield return null;
            }
            
            RectTransform winner = GetWinningSkin();
            lastWinner = winner;

            yield return new WaitForSecondsRealtime(hideDelay);
            isRolling = false;
        }

        private RectTransform GetWinningSkin()
        {
            RectTransform winner = null;
            float closestDistance = float.MaxValue;

            // Pozycja znacznika w lokalnych współrzędnych panelu
            float markerX = centerMarker.rectTransform.anchoredPosition.x + 200;

            foreach (var skin in skins)
            {
                // Odległość od markera
                float distance = Mathf.Abs(skin.anchoredPosition.x - markerX);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    winner = skin;
                }
            }

            return winner;
        }

        public IEnumerator StartRolling()
        {
            yield return RollSkins();
        }

        public Material GetLastWinnerImageName()
        {
            if (skinMaterials.Count == 0 || lastWinner.IsUnityNull()) return null;

            if (lastWinner.name.Contains("Carpet")) return skinMaterials[0];
            if (lastWinner.name.Contains("Ladybug")) return skinMaterials[3];
            if (lastWinner.name.Contains("Shrek")) return skinMaterials[4];
            if (lastWinner.name.Contains("Galaxy")) return skinMaterials[2];
            if (lastWinner.name.Contains("Supra")) return skinMaterials[5];
            if (lastWinner.name.Contains("GD")) return skinMaterials[1];
            return null;
        }
    }
}