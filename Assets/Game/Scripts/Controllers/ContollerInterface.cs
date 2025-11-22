using UnityEngine;

namespace Game.Scripts.Controllers
{
    public abstract class ControllerInterface : MonoBehaviour
    {
        public abstract void Initialize();
        public abstract void EnableController();
        public abstract void DisableController();
        public abstract void UpdateController();
    }
        
    [System.Serializable]
    public class ControllerPackage
    {
        public ControllerInterface inputController;
        public bool isActive = false;
    }
}
