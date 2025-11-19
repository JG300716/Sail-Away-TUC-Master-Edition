using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ChestOpening : MonoBehaviour
{
    [Header("UI Elements")]
    public RectTransform container;      // Panel z Image skinów
    public List<RectTransform> skins;    // Lista wszystkich skinów (15 w Twoim przypadku)
    public float spacing = 110f;         // 100 szerokość skina + 10 odstęp
    public Image centerMarker;           // Pionowa kreska

    [SerializeField] List<Material> skinMaterials;
    
    [Header("Rolling Settings")]
    public float startSpeed = 2000f;     // Początkowa prędkość przesuwania
    public float slowDownDuration = 3f;  // Czas spowolnienia
    public float minStopTime = 2f;
    public float maxStopTime = 4f;
    public float hideDelay = 2f;

    private RectTransform lastWinner;

    private bool isRolling = false;

    void Start()
    {
        // Ustaw początkowe pozycje skinów w rzędzie
        for (int i = 0; i < skins.Count; i++)
        {
            skins[i].anchoredPosition = new Vector2(i * spacing, 0);
        }

        // Start przewijania
        //StartCoroutine(RollSkins());

        gameObject.SetActive(false);
    }

    IEnumerator RollSkins()
    {
        isRolling = true;
        float elapsed = 0f;
        Random.InitState(System.DateTime.Now.Millisecond);
        float stopTime = Random.Range(minStopTime, maxStopTime);
        float speed = startSpeed;

        while (elapsed < stopTime)
        {
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

        isRolling = false;

        // Wyłonienie zwycięskiego skina pod markerem
        RectTransform winner = GetWinningSkin();
        lastWinner = winner;
        // Teraz winner jest dokładnie pod środkiem
        Debug.Log("🎁 Wylosowany skin: " + winner.name);

        yield return new WaitForSecondsRealtime(hideDelay);
        gameObject.SetActive(false);
    }

    // Funkcja pomocnicza do zawijania
    float GetMaxSkinX()
    {
        float max = float.MinValue;
        foreach (var s in skins)
        {
            if (s.anchoredPosition.x > max)
                max = s.anchoredPosition.x;
        }
        return max;
    }

    // Funkcja zwracająca skin pod markerem (najbliżej środka)
    RectTransform GetWinningSkin()
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
        Debug.Log("Center:" + centerMarker.rectTransform.anchoredPosition.x + 110);
        Debug.Log("Skin:" + winner.anchoredPosition.x);
        return winner;
    }

    public IEnumerator StartRolling()
    {
        gameObject.SetActive(true);
        yield return StartCoroutine(RollSkins());
    }

    public Material GetLastWinnerImageName()
    {
        if (skinMaterials.Count == 0 || lastWinner == null) return null;
        if (lastWinner.name.Contains("Carpet")) return skinMaterials[0];
        else if (lastWinner.name.Contains("Ladybug")) return skinMaterials[3];
        else if (lastWinner.name.Contains("Shrek"))
        {
            return skinMaterials[4];
        } else if (lastWinner.name.Contains("Galaxy"))
        {
            return skinMaterials[2];
        } else if (lastWinner.name.Contains("Supra"))
        {
            return skinMaterials[5];
        } else if (lastWinner.name.Contains("GD"))
        {
            return skinMaterials[1];    
        } else
        {
            return null;
        }
    }
}
