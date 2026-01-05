using UnityEngine;

public class TreasureChestScript : MonoBehaviour
{
    public TreasureChestController controller;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger skrzynki");
        if (other.CompareTag("Boat"))
        {
            Debug.Log("Łódka zebrała skrzynkę!");

            // np. dodaj złoto
            // GameManager.Instance.AddGold(10);
            controller.OnCollectChest(this);
            Destroy(gameObject);
        }
    }
}
