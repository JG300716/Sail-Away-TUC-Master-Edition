using UnityEngine;
using Unity.VisualScripting;
using System;
using System.Collections.Generic;
using Game.Scripts.Interface;

namespace Game.Scripts.Controllers
{

    public class ControllerManager : SingletonInterface<ControllerManager>
    {
        [Header("Controllers")] 
        [SerializeField] private float mouseSensitivity = 1.0f;
        
        [SerializeField] private List<ControllerPackage> controllers;
        [SerializeField] private List<ControllerInterface> cameraControllers;
        [SerializeField] private ControllerInterface defaultCameraController;

        private List<int> currentInputController;
        private List<int> inputControllersToActivate;
        private List<int> inputControllersToDeactivate;
        
        private ControllerInterface currentCameraController;
        
        void Start()
        {
            if (controllers.IsUnityNull()) throw new System.Exception("Controllers list is null in ControllerManager.");
            if (controllers.Count == 0) throw new System.Exception("No controllers assigned to ControllerManager.");
            
            currentInputController = new List<int>();
            inputControllersToActivate = new List<int>();
            inputControllersToDeactivate = new List<int>();
            
            foreach (var controller in controllers)
            {
                if (controller.IsUnityNull()) continue;
                controller.inputController.Initialize();
                controller.inputController.package = controller;
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
            defaultCameraController.Initialize();
            defaultCameraController.EnableController();
            currentCameraController = defaultCameraController;
        }
        
        public void ActivateInputController(params int[] indexes)
        {
            foreach (var index in indexes)
            {
                if (index < 0 || index >= controllers.Count) throw new IndexOutOfRangeException("Controller index out of range in ControllerManager.");
                var controllerPackage = controllers[index];
                if (controllerPackage.IsUnityNull()) throw new NullReferenceException("Controller package is null in ControllerManager.");
                if (controllerPackage.isActive) continue;
                
                inputControllersToActivate.Add(controllers.IndexOf(controllerPackage));
            }
        }
        
        public void DeactivateInputController(params int[] indexes)
        {
            foreach (var index in indexes)
            {
                if (index < 0 || index >= controllers.Count) throw new IndexOutOfRangeException("Controller index out of range in ControllerManager.");
                var controllerPackage = controllers[index];
                if (controllerPackage.IsUnityNull()) throw new NullReferenceException("Controller package is null in ControllerManager.");
                if (!controllerPackage.isActive) continue;
                
                inputControllersToDeactivate.Add(controllers.IndexOf(controllerPackage));
            }
        }
        
        public void ActivateCameraController(int index)
        {
            if (index < 0 || index >= cameraControllers.Count) throw new IndexOutOfRangeException("Camera controller index out of range in ControllerManager.");
            var cameraController = cameraControllers[index];
            if (cameraController.IsUnityNull()) throw new NullReferenceException("Camera controller is null in ControllerManager.");
            cameraController.EnableController();
            currentCameraController = cameraController;
        }
        
        public void DeactivateCameraController()
        {
            if (currentCameraController.IsUnityNull()) return;
            currentCameraController.DisableController();
            currentCameraController = null;
        }
        
        void Update()
        {
            RunControllers(c => c.UpdateController());
            currentCameraController?.UpdateController();
        }

        void FixedUpdate()
        {
            RunControllers(c => c.FixedUpdateController());
            currentCameraController?.FixedUpdateController();
        }

        private void RunControllers(System.Action<ControllerInterface> updateFunc)
        {
            if (controllers.IsUnityNull()) return;

            // Update active controllers
            foreach (var input in currentInputController.ToArray()) // ToArray dla bezpieczeństwa przy ewentualnym usuwaniu
            {
                var ctrl = controllers[input];
                if (ctrl.IsUnityNull() || ctrl.inputController.IsUnityNull() || !ctrl.isActive) continue;
                updateFunc(ctrl.inputController);
            }

            // Deactivate controllers
            foreach (var index in inputControllersToDeactivate)
            {
                var ctrl = controllers[index];
                ctrl.isActive = false;
                currentInputController.Remove(index);
            }
            inputControllersToDeactivate.Clear();

            // Activate controllers
            foreach (var index in inputControllersToActivate)
            {
                if (currentInputController.Contains(index)) continue;
                var ctrl = controllers[index];
                ctrl.isActive = true;
                currentInputController.Add(index);
            }
            inputControllersToActivate.Clear();
        }
        
    }
}
