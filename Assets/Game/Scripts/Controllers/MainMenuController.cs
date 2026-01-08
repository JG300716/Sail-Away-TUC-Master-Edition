using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    AsyncOperation asyncOp;
    
    void Start()
    {
        MusicController.Instance.PlayMenu();
        // Time.timeScale = 0f;
        // asyncOp = SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
        // asyncOp.allowSceneActivation = false;
    }

    public void PlayGame()
    {
        // asyncOp.allowSceneActivation = true;
        // SceneManager.UnloadSceneAsync(0);
        // Time.timeScale = 1f;
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
