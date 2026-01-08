using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    //[SerializeField] private string mainSceneName = "ocean";
    //[SerializeField] private string secondarySceneName = "inside";

    private bool secondaryLoaded = false;

    void Update()
    {
        // Wczytaj scenê A (g³ówn¹)
        if (Input.GetKeyDown(KeyCode.O))
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            LightManager.Instance.SwitchLight();
            //Time.timeScale = 1f; // na wszelki wypadek
        }

        // Wczytaj scenê B jako additive i zapauzuj A
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!secondaryLoaded)
            {
                SceneManager.LoadScene(1, LoadSceneMode.Additive);
                //Time.timeScale = 0f;
                secondaryLoaded = true;
            }
            else
            {
                // Zamknij B i wznow czas
                SceneManager.UnloadSceneAsync(1);
                //Time.timeScale = 1f;
                secondaryLoaded = false;
            }
        }
    }
}