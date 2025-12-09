using System;
using System.Collections;
using UnityEngine;
using Game.Scripts.Interface;
using TMPro;
using Unity.VisualScripting;

namespace Game.Scripts.UI
{
    public class UIManager : SingletonInterface<UIManager>
    {
        [Header("References")] 
        [SerializeField] public CanvasGroup steeringHudUI;
        [SerializeField] public CanvasGroup openingScreenUI;
        [SerializeField] public CanvasGroup triggerUI;
        
        [Header("Trigger UI Elements")]
        [SerializeField] private TextMeshProUGUI triggerText;
        public static string basicEnterTriggerMessage = "Press F to enter downstairs.";
        public static string basicExitTriggerMessage = "Press F to exit to upstairs.";
        public static string basicSteerTriggerMessage = "Press F to steer the yacht.";
        public static string basicUnsteerTriggerMessage = "Press F to stop steering the yacht.";
        
        private static Coroutine currentFadeRoutine;

        private void Start()
        {
            ActivateCanvasGroup(steeringHudUI);
            DeactivateCanvasGroup(openingScreenUI);
            ActivateCanvasGroup(triggerUI);
            
            HideCanvasGroup(triggerUI);
        }

        public static void ShowCanvasGroup(CanvasGroup cg)
        {
            if (cg.IsUnityNull()) return;
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
        
        public static void HideCanvasGroup(CanvasGroup cg)
        {
            if (cg.IsUnityNull()) return;
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        
        public static void ActivateCanvasGroup(CanvasGroup cg)
        {
            if (cg.IsUnityNull()) return;
            cg.gameObject.SetActive(true);
        }
        
        public static void DeactivateCanvasGroup(CanvasGroup cg)
        {
            if (cg.IsUnityNull()) return;
            cg.gameObject.SetActive(false);
        }
        
        public static void OpeningScreenShow()
        {
            if (Instance.IsUnityNull()) return;
            ActivateCanvasGroup(Instance.openingScreenUI);
            DeactivateCanvasGroup(Instance.steeringHudUI);
            DeactivateCanvasGroup(Instance.triggerUI);
        }
        
        public static void OpeningScreenHide()
        {
            if (Instance.IsUnityNull()) return;
            DeactivateCanvasGroup(Instance.openingScreenUI);
            ActivateCanvasGroup(Instance.steeringHudUI);
            ActivateCanvasGroup(Instance.triggerUI);
        }

        public static void TriggerUI(string msg)
        {
            if (Instance.IsUnityNull()) return;
            if (Instance.triggerUI.IsUnityNull()) return;
            if (Instance.triggerText.IsUnityNull()) return;
            Instance.triggerText.text = msg;
            ShowCanvasGroup(Instance.triggerUI);
            
            if (currentFadeRoutine != null) Instance.StopCoroutine(currentFadeRoutine);
            currentFadeRoutine = Instance.StartCoroutine(Fade(Instance.triggerUI, 0f, 10f));
        }
        
        private static IEnumerator Fade(CanvasGroup cg, float to, float duration)
        {
            float start = cg.alpha;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(start, to, t / duration);
                yield return null;
            }

            cg.alpha = to;
        }
        
    }
}
