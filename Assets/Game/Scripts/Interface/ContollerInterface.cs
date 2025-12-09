using UnityEngine;
using Unity.VisualScripting;

namespace Game.Scripts.Interface
{
    public class ReadOnlyAttribute : PropertyAttribute { }

    public abstract class ControllerInterface : MonoBehaviour
    {
        [HideInInspector]
        public ControllerPackage package;
        public abstract void Initialize();
        public abstract void EnableController();
        public abstract void DisableController();
        public abstract void UpdateController();
        public abstract void FixedUpdateController(); 

        public bool isActive
        {
            get => package != null && package.isActive;
            set
            {
                if (package == null) return;
                package.isActive = value;

                if (value) EnableController();
                else DisableController();
            }
        }
    }
        
    [System.Serializable]
    public class ControllerPackage
    {
        public ControllerInterface inputController;
        public bool isActive = false;
    }
}
