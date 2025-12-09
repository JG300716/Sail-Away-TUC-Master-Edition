using UnityEngine;
using Game.Scripts.Interface;
using Game.Scripts.Controllers;
using Game.Scripts.Weather;
using Unity.VisualScripting;

namespace Game.Scripts
{

    public class GameManager : SingletonInterface<GameManager>
    {
        private static WeatherManager weatherManager => WeatherManager.Instance;
        private static ControllerManager controllerManager => ControllerManager.Instance;
        
        [Header("Game References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform HumanPlayer;
        [SerializeField] private Transform Yacht;
        [SerializeField] private Transform playerSteeringAttachPoint;

        void Start()
        {
            if (mainCamera.IsUnityNull()) mainCamera = Camera.main;
            if (weatherManager.IsUnityNull()) throw new System.Exception("WeatherManager instance is null in GameManager.");
            
            weatherManager.Initialize(mainCamera);
            EnterYacht();
        }

        public static void EnterYacht()
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
        }
        
        public static void LeaveYacht()
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
        }
    }
}
