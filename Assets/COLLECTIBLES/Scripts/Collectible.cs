using UnityEngine;
using DG.Tweening;

public class Collectible : MonoBehaviour
{
    public CollectibleData data;

    private GameObject player;
    private PlayerHealth playerHealth;
    private PlayerAttack playerAttack;
    private PlayerStamina playerStamina;
    private PlayerController playerController;

    private bool isCollected = false; // Çift tetiklemeyi engellemek için

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
        if (isCollected) return; // Daha önce toplandıysa çık
        if (collision.CompareTag("Player"))
        {
            isCollected = true;
            PlayCollectAnimation();
        }
    }

    private void PlayCollectAnimation()
    {
        Vector3 startPos = transform.position;
        Vector3 upPos = startPos + Vector3.up * 0.2f;
        float duration = 0.15f;

        Sequence seq = DOTween.Sequence();

        // Yukarı çıkış
        seq.Append(transform.DOMove(upPos, duration).SetEase(Ease.OutQuad));

        // Geri iniş
        seq.Append(transform.DOMove(startPos, duration).SetEase(Ease.InQuad));

        // Bitince efekti uygula ve objeyi yok et
        seq.OnComplete(() =>
        {
            ApplyEffect();
            Destroy(gameObject);
        });

        // Hedef olarak transform'u ayarla (objeyle birlikte tween yok olur)
        seq.SetTarget(transform);
    }

    private void ApplyEffect()
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
                // İleride eklenebilir
                break;

            case ItemType.Gloves:
                playerAttack.attackSpeed += playerAttack.attackSpeed * data.value / 100;
                playerAttack.attackCooldown -= playerAttack.attackCooldown * data.value / 100;
                break;

            case ItemType.Boots:
                playerController.moveSpeed += playerController.moveSpeed * data.value / 100;
                break;

            case ItemType.Weapon:
                playerAttack.attackDamage += data.value;
                break;

            case ItemType.HealthPotion:
                playerHealth.ChangeHealth(data.value);
                break;

            case ItemType.StaminaPotion:
                playerStamina.GetStamina(data.value);
                break;

            case ItemType.Health:
                playerHealth.maxHealth += data.value;
                break;

            case ItemType.Stamina:
                playerStamina.maxStamina += data.value;
                break;

            case ItemType.Skull:
                playerAttack.attackDamage += data.value;
                playerHealth.maxHealth -= 40;
                break;

            default:
                Debug.Log("Unknown item type");
                break;
        }
    }

    private void OnDestroy()
    {
        // Obje yok olurken aktif tüm tween'leri öldür
        transform.DOKill();
    }
}
