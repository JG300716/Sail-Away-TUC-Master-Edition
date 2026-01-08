using System.Collections.Generic;
using Game.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Game.Scripts;
 
namespace Game.Scripts.Controllers
{

     public class TreasureChestController : MonoBehaviour
     {
         [SerializeField] private GameObject chestPrefab;
         [SerializeField] private GameObject boat;
         [SerializeField] private float chestGenerationRadius = 50f;
         [SerializeField] private int chestCount = 10;
         [SerializeField] private TMP_Text collectedChestCounterText;
         [SerializeField] private TMP_Text unlockedChestCounterText;
         [SerializeField] private WaterSurface waterSurface;
         private List<GameObject> spawnedChests = new List<GameObject>();
         private int collectedChests = 0;

         // Start is called once before the first execution of Update after the MonoBehaviour is created
         void Start()
         {
             for (int i = 0; i < chestCount; i++)
             {
                 AddNewChest();
             }

             AddChestOnYacht();
         }

         // Update is called once per frame
         void Update()
         {

         }
         
         private void OnEnable()
         {
             GameManager.Instance.OnTucSolved += HandleTucSolved;
         }

         private void OnDisable()
         {
             GameManager.Instance.OnTucSolved -= HandleTucSolved;
         }

         private void HandleTucSolved()
         {
             int collectedChests = int.Parse(collectedChestCounterText.text) - 1;
             collectedChestCounterText.text =  collectedChests.ToString();
             int unlockedChests = int.Parse(unlockedChestCounterText.text) + 1;
             unlockedChestCounterText.text = unlockedChests.ToString();
         }

         public void OnCollectChest(TreasureChestScript chest)
         {
             collectedChests++;
             collectedChestCounterText.text = collectedChests.ToString();
             RemoveChest(chest);
             AddNewChest();
         }

         private void RemoveChest(TreasureChestScript chest)
         {
             spawnedChests.Remove(chest.gameObject);
         }

         private void AddNewChest()
         {
             Vector2 randomPoint = Random.insideUnitCircle * chestGenerationRadius;

             Vector3 spawnPosition = new Vector3(
                 boat.transform.position.x + randomPoint.x,
                 boat.transform.position.y + 10,
                 boat.transform.position.z + randomPoint.y
             );
             GameObject chest = Instantiate(chestPrefab, spawnPosition, Quaternion.identity);
             TreasureChestScript chestScript = chest.GetComponent<TreasureChestScript>();
             chestScript.controller = this;
             BoatFloatingAdvanced treasureChestFloating = chest.GetComponent<BoatFloatingAdvanced>();
             treasureChestFloating.waterSurface = waterSurface;
             spawnedChests.Add(chest);
         }

         private void AddChestOnYacht()
         {
             Vector3 spawnPosition = new Vector3(
                 52.93299f,
                 10.39f,
                 -81.26144f
             );
             GameObject chest = Instantiate(chestPrefab, spawnPosition, Quaternion.identity);
             TreasureChestScript chestScript = chest.GetComponent<TreasureChestScript>();
             chestScript.controller = this;
             BoatFloatingAdvanced treasureChestFloating = chest.GetComponent<BoatFloatingAdvanced>();
             treasureChestFloating.waterSurface = waterSurface;
             spawnedChests.Add(chest);
         }
     }
 }