using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using Game.Scripts.Interface;

namespace Game.Scripts.Weather
{
    public class WeatherManager : SingletonInterface<WeatherManager>
    {
        [Header("References")]   
        [SerializeField] private DirectionalLight sunLight;

        // TODO : Wind Manager implementation
        [SerializeField] private UnifiedWindManager windManager => UnifiedWindManager.Instance;
        private Camera mainCamera;
        
        [SerializeField] private Volume globalVolume;
        
        [Header("Volume Profiles")]
        [SerializeField] private VolumeProfile dayProfile;
        [SerializeField] private VolumeProfile nightProfile;
        [SerializeField] private VolumeProfile bloodMoonProfile;
        
        [Header("Weather Settings")]
        [SerializeField] private WeatherType currentWeather = WeatherType.Clear;
        [SerializeField, Range(0f, 100f)] private float weatherChangeChance = 10f;
        [SerializeField] private float weatherCheckInterval = 30f;
        
        [Header("Rain Settings")]
        [SerializeField, Range(0f, 100f)] private float rainChance = 20f;
        [SerializeField] private float minRainDuration = 60f;
        [SerializeField] private float maxRainDuration = 180f;
        
        [Header("Storm Settings")]
        [SerializeField, Range(0f, 100f)] private float stormChance = 5f;
        [SerializeField] private float minStormDuration = 30f;
        [SerializeField] private float maxStormDuration = 120f;
        [SerializeField] private float lightningMinInterval = 3f;
        [SerializeField] private float lightningMaxInterval = 10f;
        
        [Header("Lightning Settings")]
        [SerializeField] private Lightning lightningPrefab;
        
        private float weatherCheckTimer;
        private float currentWeatherDuration;
        private float weatherTimer;
        private bool isRaining;
        private bool isStorming;
        private Coroutine stormCoroutine;
        
        public enum WeatherType
        {
            Clear,
            Rain,
            Storm
        }
        
        public void Initialize(Camera cam)
        {
            this.mainCamera = cam;
            if (globalVolume != null && dayProfile != null)
            {
                globalVolume.profile = dayProfile;
            }

            if (windManager.IsUnityNull()) return;
            weatherCheckTimer = weatherCheckInterval;
        }
        void Update()
        {
            // Timer sprawdzający zmiany pogody
            weatherCheckTimer -= Time.deltaTime;
            
            if (weatherCheckTimer <= 0f)
            {
                CheckWeatherChange();
                weatherCheckTimer = weatherCheckInterval;
            }
            
            // Timer trwania obecnej pogody
            if (currentWeatherDuration <= 0f) return;
            weatherTimer += Time.deltaTime;
            
            if (weatherTimer >= currentWeatherDuration) EndCurrentWeather();
        }
        
        private void CheckWeatherChange()
        {
            if (currentWeather != WeatherType.Clear) return;
            
            var roll = Random.Range(0f, 100f);

            if (roll >= weatherChangeChance) return;
            var weatherRoll = Random.Range(0f, 100f);
            
            if (weatherRoll < stormChance)
            {
                StartStorm();
            }
            else if (weatherRoll < stormChance + rainChance)
            {
                StartRain();
            }
        }
        
        private void StartRain()
        {
            currentWeather = WeatherType.Rain;
            isRaining = true;
            currentWeatherDuration = Random.Range(minRainDuration, maxRainDuration);
            weatherTimer = 0f;
            
            GenerateRain();
        }
        
        private void StartStorm()
        {
            currentWeather = WeatherType.Storm;
            isStorming = true;
            isRaining = true;
            currentWeatherDuration = Random.Range(minStormDuration, maxStormDuration);
            weatherTimer = 0f;
            
            GenerateRain();
            
            if (stormCoroutine != null)
            {
                StopCoroutine(stormCoroutine);
            }
            stormCoroutine = StartCoroutine(StormRoutine());
        }
        
        private void EndCurrentWeather()
        {
            
            if (isStorming && stormCoroutine != null)
            {
                StopCoroutine(stormCoroutine);
                stormCoroutine = null;
            }
            
            // TODO: Zatrzymaj deszcz
            
            currentWeather = WeatherType.Clear;
            isRaining = false;
            isStorming = false;
            currentWeatherDuration = 0f;
            weatherTimer = 0f;
        }
        
        private void GenerateRain()
        {
            Debug.Log("GenerateRain() - Do implementacji");
        }
        
        private IEnumerator StormRoutine()
        {
            while (isStorming)
            {
                float waitTime = Random.Range(lightningMinInterval, lightningMaxInterval);
                yield return new WaitForSeconds(waitTime);
                GenerateLightning();
            }
        }
        
        private void GenerateLightning()
        {
            Lightning lightning = Instantiate(lightningPrefab);
            if (lightning.IsUnityNull()) return;
            lightning.Generate(mainCamera);
        }
        
        public void SetDayProfile()
        {
            if (globalVolume.IsUnityNull() || dayProfile.IsUnityNull()) return;
            globalVolume.profile = dayProfile;
        }
        
        public void SetNightProfile()
        {
            if (globalVolume.IsUnityNull() || nightProfile.IsUnityNull()) return;
            globalVolume.profile = nightProfile;
        }
        
        public void SetBloodMoonProfile()
        {
            if (globalVolume.IsUnityNull() || bloodMoonProfile.IsUnityNull()) return;
            globalVolume.profile = bloodMoonProfile;
        }
        
        public bool IsRaining => isRaining;
        public bool IsStorming => isStorming;
        public WeatherType CurrentWeather => currentWeather;
    }
}