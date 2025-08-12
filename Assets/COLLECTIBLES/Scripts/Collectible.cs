using UnityEngine;

public class Collectible : MonoBehaviour
{
    public CollectibleData data;

    private GameObject player;
    private PlayerHealth playerHealth;
    private PlayerAttack playerAttack;
    private PlayerStamina playerStamina;
    private PlayerController playerController;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerHealth = player.GetComponent<PlayerHealth>();
        playerAttack = player.GetComponent<PlayerAttack>();
        playerStamina = player.GetComponent<PlayerStamina>();
        playerController = player.GetComponent<PlayerController>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ApplyEffect(collision.gameObject);
            StartCoroutine(DestroyAfterDelay(0.05f));
        }
    }

    private void ApplyEffect(GameObject player)
    {
        switch (data.itemType)
        {
            case ItemType.Food:
                playerHealth.ChangeHealth(data.value);
                break;

            case ItemType.Armor:
                playerHealth.currentArmor += data.value;
                if (playerHealth.currentArmor > playerHealth.maxArmor)
                {
                    playerHealth.currentArmor = playerHealth.maxArmor;
                }
                break;

            case ItemType.Helmet:
                
                break;

            case ItemType.HealthPotion:
                playerHealth.ChangeHealth(data.value);
                break;

            case ItemType.StaminaPotion:
                playerStamina.GetStamina(data.value);
                break;

            case ItemType.Powerup:
                playerHealth.maxHealth += data.value;
                break;

            default:
                Debug.Log("Unknown item type");
                break;
        }
    }

    private System.Collections.IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
