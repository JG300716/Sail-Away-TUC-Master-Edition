using UnityEngine;

namespace Game.Assets.Ocean
{
    public class LightManager : SingletonInterface<LightManager>
    {
        public static LightManager Instance;

        [Header("Lights")] public GameObject lightA;
        public GameObject lightB;

        public int mode = 0;
        // 0 - wiecz�r - razem daj� taki fajny efekt
        // 1 - noc
        // 2 - blood moon

        void Awake()
        {
            // prosty singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            ApplyMode();
        }

        static public void SwitchLight()
        {
            LightManager.Instance.mode = (mode + 1) % 3;
            Debug.Log("Light mode switched to: " + LightManager.Instance.mode);
            LightManager.Instance.ApplyMode();
        }

        void ApplyMode()
        {
            if (lightA == null || lightB == null)
                return;

            switch (mode)
            {
                case 0: // afternoon
                    lightA.SetActive(true);
                    lightB.SetActive(true);
                    break;

                case 1:
                    lightA.SetActive(true);
                    lightB.SetActive(false);
                    break;

                case 2:
                    lightA.SetActive(false);
                    lightB.SetActive(true);
                    break;
            }
        }

        //public void SetMode(int newMode)
        //{
        //    mode = Mathf.Clamp(newMode, 0, 2);
        //    ApplyMode();
        //}
    }
}