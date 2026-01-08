using UnityEngine;
using System.Collections;
using Game.Scripts.UI;
using Unity.VisualScripting;
using Game.Scripts.Interface;
using TMPro;


namespace Game.Scripts.Controllers
{
    public class LootBoxController : ControllerInterface
    {
        [Header("Chest Opening")]
        [SerializeField] ChestOpening ChestOpening;
        [SerializeField] private SkinnedMeshRenderer grotSkinnedMeshRenderer;
        [SerializeField] private SkinnedMeshRenderer fokSkinnedMeshRenderer;
        [SerializeField] private TMP_Text unlockedChestCounterText;
        public override void Initialize()
        {
            if (ChestOpening.IsUnityNull()) Debug.LogError("ChestOpening reference is not set in LootBoxController.");
            if (grotSkinnedMeshRenderer.IsUnityNull()) Debug.LogError("grotSkinnedMeshRenderer reference is not set in LootBoxController.");
            if (fokSkinnedMeshRenderer.IsUnityNull()) Debug.LogError("fokSkinnedMeshRenderer reference is not set in LootBoxController.");
        }

        public override void UpdateController()
        {
            int unlockedChestsAmount = int.Parse(unlockedChestCounterText.text);
            if (!Input.GetKeyDown(KeyCode.F1) || unlockedChestsAmount == 0) return;
            if (ChestOpening.isRolling) return;
            unlockedChestsAmount--;
            unlockedChestCounterText.text = unlockedChestsAmount.ToString();
            UIManager.OpeningScreenShow();
            StartCoroutine(HandleChestOpeningCoroutine());
        }
        public override void FixedUpdateController(){}

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

            yield return ChestOpening?.StartRolling();

            Material material = ChestOpening?.GetLastWinnerImageName();
            if (!material.IsUnityNull() && !grotSkinnedMeshRenderer.IsUnityNull() && !fokSkinnedMeshRenderer.IsUnityNull())
            {
                grotSkinnedMeshRenderer.material = material;
                fokSkinnedMeshRenderer.material = material;
            }
            
            UIManager.OpeningScreenHide();
            Time.timeScale = 1f;
        }
    }
}
