using UnityEngine;
using Game.Scripts.Interface;
using Game.Scripts.Controllers;
using Game.Scripts.UI;
using Game.Scripts.Weather;
using Unity.VisualScripting;

namespace Game.Scripts
{

    public class GameManager : SingletonInterface<GameManager>
    {
        private static WeatherManager weatherManager => WeatherManager.Instance;
        private static ControllerManager controllerManager => ControllerManager.Instance;
        
        private static UIManager uiManager => UIManager.Instance;
        
        [Header("Game References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform HumanPlayer;
        [SerializeField] private Transform Yacht;
        [SerializeField] private Transform playerSteeringAttachPoint;

        void Start()
        {
            if (mainCamera.IsUnityNull()) mainCamera = Camera.main;
            if (weatherManager.IsUnityNull()) throw new System.Exception("WeatherManager instance is null in GameManager.");
            if (uiManager.IsUnityNull()) throw new System.Exception("UIManager instance is null in GameManager.");
            if (controllerManager.IsUnityNull()) throw new System.Exception("ControllerManager instance is null in GameManager.");
            
            weatherManager.Initialize(mainCamera);
            UnsteerYacht();
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
            //TODO: Implement entering yacht logic
        }

        public static void LeaveYacht()
        {
            //TODO: Implement leaving yacht logic
        }
    }
}
