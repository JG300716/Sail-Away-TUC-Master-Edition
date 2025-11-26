using UnityEngine;
using Unity.VisualScripting;
using System;
using System.Collections.Generic;

namespace Game.Scripts.Controllers
{

    public class ControllerManager : MonoBehaviour
    {
        
        [Header("Controllers")] 
        [SerializeField] private float mouseSensitivity = 1.0f;
        [SerializeField] private List<ControllerPackage> controllers;
        [SerializeField] private List<ControllerInterface> cameraControllers;
        [SerializeField] private ControllerInterface defaultCameraController;

        private List<int> currentInputController;
        private Action currentCameraController;

        void Start()
        {
            if (controllers.IsUnityNull()) throw new System.Exception("Controllers list is null in ControllerManager.");
            if (controllers.Count == 0) throw new System.Exception("No controllers assigned to ControllerManager.");
            
            currentInputController = new List<int>();

            foreach (var controller in controllers)
            {
                if (controller.IsUnityNull()) continue;
                controller.inputController.Initialize();
                if (!controller.isActive) continue;
                currentInputController.Add(controllers.IndexOf(controller));
                controller.inputController.EnableController();
            }

            foreach (var cameraController in cameraControllers)
            {
                if (cameraController.IsUnityNull()) continue;
                cameraController.Initialize();
            }
            if (defaultCameraController.IsUnityNull()) throw new NullReferenceException("Default camera controller is null in ControllerManager.");
            defaultCameraController.EnableController();
            currentCameraController = defaultCameraController.UpdateController;
        }

        void ActivateInputController(params int[] indexes)
        {
            foreach (var index in indexes)
            {
                if (index < 0 || index >= controllers.Count) throw new IndexOutOfRangeException("Controller index out of range in ControllerManager.");
                var controllerPackage = controllers[index];
                if (controllerPackage.IsUnityNull()) throw new NullReferenceException("Controller package is null in ControllerManager.");
                if (controllerPackage.isActive) continue;
                currentInputController.Add(controllers.IndexOf(controllerPackage));

                controllerPackage.inputController.EnableController();
                controllerPackage.isActive = true;
            }
        }
        
        void DeactivateInputController(params int[] indexes)
        {
            foreach (var index in indexes)
            {
                if (index < 0 || index >= controllers.Count) throw new IndexOutOfRangeException("Controller index out of range in ControllerManager.");
                var controllerPackage = controllers[index];
                if (controllerPackage.IsUnityNull()) throw new NullReferenceException("Controller package is null in ControllerManager.");
                if (!controllerPackage.isActive) continue;
                currentInputController.Remove(controllers.IndexOf(controllerPackage));

                controllerPackage.isActive = false;
                controllerPackage.inputController.DisableController();
            }
        }
        
        void SetCameraController(int index)
        {
            if (index < 0 || index >= cameraControllers.Count) throw new IndexOutOfRangeException("Camera controller index out of range in ControllerManager.");
            var cameraController = cameraControllers[index];
            if (cameraController.IsUnityNull()) throw new NullReferenceException("Camera controller is null in ControllerManager.");
            currentCameraController = cameraController.UpdateController;
        }
        
        void ClearCameraController()
        {
            currentCameraController = null;
        }
        
        void Update()
        {
            foreach (var input in currentInputController)
            {
                if (controllers[input].IsUnityNull()) continue;
                if (controllers[input].inputController.IsUnityNull()) continue;
                if (!controllers[input].isActive) continue;
                controllers[input].inputController.UpdateController();
            }
            if (currentCameraController.IsUnityNull()) return;
            currentCameraController();
        }

    }
}
