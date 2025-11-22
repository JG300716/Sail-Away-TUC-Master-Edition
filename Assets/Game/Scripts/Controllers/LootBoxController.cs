using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Scripts.Controllers
{
    public class LootBoxController : ControllerInterface
    {
        [Header("Chest Opening")]
        [SerializeField] ChestOpening ChestOpening;
        [SerializeField] private SkinnedMeshRenderer grotSkinnedMeshRenderer;
        [SerializeField] private SkinnedMeshRenderer fokSkinnedMeshRenderer;
        
        private Coroutine chestOpeningCoroutine;

        public override void Initialize()
        {
            if (ChestOpening.IsUnityNull()) Debug.LogError("ChestOpening reference is not set in LootBoxController.");
            if (grotSkinnedMeshRenderer.IsUnityNull()) Debug.LogError("grotSkinnedMeshRenderer reference is not set in LootBoxController.");
            if (fokSkinnedMeshRenderer.IsUnityNull()) Debug.LogError("fokSkinnedMeshRenderer reference is not set in LootBoxController.");
        }

        public override void UpdateController()
        {
            if (!Input.GetKeyDown(KeyCode.F1)) return;
            if (ChestOpening.isRolling) return;
            StartCoroutine(HandleChestOpeningCoroutine());
        }

        public override void EnableController()
        {
            if (ChestOpening.IsUnityNull()) return;
            ChestOpening.isPaused = false;
        }

        public override void DisableController()
        {
            if (ChestOpening.IsUnityNull()) return;
            ChestOpening.isPaused = true;
        }
        
        IEnumerator HandleChestOpeningCoroutine()
        {
            Time.timeScale = 0f;

            if (ChestOpening.IsUnityNull()) yield break;
            chestOpeningCoroutine = StartCoroutine(ChestOpening.StartRolling());
            yield return chestOpeningCoroutine;

            Material material = ChestOpening.GetLastWinnerImageName();
            if (!material.IsUnityNull() && !grotSkinnedMeshRenderer.IsUnityNull() && !fokSkinnedMeshRenderer.IsUnityNull())
            {
                grotSkinnedMeshRenderer.material = material;
                fokSkinnedMeshRenderer.material = material;
            }
            chestOpeningCoroutine = null;
            Time.timeScale = 1f;
        }
    }
}
