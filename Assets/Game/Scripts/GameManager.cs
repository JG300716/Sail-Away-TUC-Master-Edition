using System;
using UnityEngine;
using Game.Scripts.Interface;
using Game.Scripts.Controllers;
using Game.Scripts.UI;
using Game.Scripts.Weather;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using Game.Scripts.Weather;

namespace Game.Scripts
{

    public class GameManager : SingletonInterface<GameManager>
    {
        public event Action OnTucSolved;
        private static WeatherManager weatherManager => WeatherManager.Instance;
        private static ControllerManager controllerManager => ControllerManager.Instance;
        
        private static UIManager uiManager => UIManager.Instance;
        
        [Header("Game References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform HumanPlayer;
        [SerializeField] private Transform Yacht;
        [SerializeField] private Transform playerSteeringAttachPoint;
        [SerializeField] private TMP_Text collectedChestCounterText;

        private static bool secondaryLoaded = false;
        private bool isPaused = false;

        void Start()
        {
            if (mainCamera.IsUnityNull()) mainCamera = Camera.main;
            if (weatherManager.IsUnityNull()) throw new System.Exception("WeatherManager instance is null in GameManager.");
            if (uiManager.IsUnityNull()) throw new System.Exception("UIManager instance is null in GameManager.");
            if (controllerManager.IsUnityNull()) throw new System.Exception("ControllerManager instance is null in GameManager.");
            
            MusicController.Instance.PlayMain();
            weatherManager.Initialize(mainCamera);
            SteerYacht();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !isPaused)
            {
                PauseGame();
            }
        }

        public void TucSolved()
        {
            OnTucSolved?.Invoke();
        }

        public void PauseGame()
        {
            Time.timeScale = 0f;
            MusicController.Instance.PauseMusic();
            SceneManager.LoadScene(3, LoadSceneMode.Additive);
            isPaused = true;
        }
        
        public void ResumeGame()
        {
            Time.timeScale = 1f;
            MusicController.Instance.ResumeMusic();
            SceneManager.UnloadSceneAsync(3);
            isPaused = false;
        }

        public static void SteerYacht()
        {
            if (controllerManager.IsUnityNull()) return;
            controllerManager.DeactivateInputController(1);
            controllerManager.ActivateInputController(0);

            controllerManager.DeactivateCameraController();
            controllerManager.ActivateCameraController(0);
            
            if (Instance.IsUnityNull()) return;
            if (Instance.HumanPlayer.IsUnityNull()) return;
            if (Instance.playerSteeringAttachPoint.IsUnityNull()) return;
            Instance.HumanPlayer.transform.SetParent(Instance.playerSteeringAttachPoint);
            Instance.HumanPlayer.transform.localPosition = Vector3.zero;
            Instance.HumanPlayer.transform.localRotation = Quaternion.identity;
            
            UIManager.TriggerUI(UIManager.basicUnsteerTriggerMessage);
        }
        
        public static void UnsteerYacht()
        {
            if (controllerManager.IsUnityNull()) return;
            controllerManager.DeactivateInputController(0);
            controllerManager.ActivateInputController(1);

            controllerManager.DeactivateCameraController();
            controllerManager.ActivateCameraController(0);
            
            if (Instance.IsUnityNull()) return;
            if (Instance.HumanPlayer.IsUnityNull()) return;
            if (Instance.Yacht.IsUnityNull()) return;
            Instance.HumanPlayer.transform.SetParent(Instance.Yacht);
            
            if (uiManager.IsUnityNull()) return;
            UIManager.TriggerUI(UIManager.basicSteerTriggerMessage);
        }

        public static void EnterYacht()
        {
            if (!secondaryLoaded)
            {
                if (int.Parse(Instance.collectedChestCounterText.text) == 0)
                {
                    return;
                }
                MusicController.Instance.PlayRemix();
                SceneManager.LoadScene(2, LoadSceneMode.Additive);
                UIManager.HideCanvasGroup(UIManager.Instance.steeringHudUI);
                UIManager.TriggerUI(UIManager.basicExitTriggerMessage);
                //Time.timeScale = 0f;
            }
            else
            {
                MusicController.Instance.PlayMain();
                LightManager.SwitchLight();
                SceneManager.UnloadSceneAsync(2);
                UIManager.ShowCanvasGroup(UIManager.Instance.steeringHudUI);
                Cursor.lockState = CursorLockMode.Locked;
                //Time.timeScale = 1f;
            }        
            secondaryLoaded = !secondaryLoaded;
            
        }

        public static void LeaveYacht()
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            UIManager.ShowCanvasGroup(UIManager.Instance.steeringHudUI);
        }
    }
}
