 using UnityEngine;

public class TreasureChestController : MonoBehaviour
{
    [SerializeField] GameObject chest;
    [SerializeField] GameObject boat;
    [SerializeField] float chestGenerationRadius = 50f;
    bool isChestCountdown = false;
    public float startCountdownTime = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startCountdownTime = Time.time;
        isChestCountdown = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isChestCountdown && Time.time - startCountdownTime > 30)
        {
            isChestCountdown = false;
            Vector2 randomPointOnCircle = Random.insideUnitCircle.normalized * chestGenerationRadius;
            chest.transform.position = new Vector3(
                boat.transform.position.x + randomPointOnCircle.x,
                boat.transform.position.y + 10,
                boat.transform.position.z + randomPointOnCircle.y
            );
            Debug.Log("Chest Activated. Chest positions: " + chest.transform.position);
            chest.SetActive(true);
        }
    }
}
