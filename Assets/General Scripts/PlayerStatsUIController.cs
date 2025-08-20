using UnityEngine;
using TMPro;

public class PlayerStatsUIController : MonoBehaviour
{
    public GameObject[] statsSlots;
    public PlayerAttack playerAttack;
    public PlayerHealth playerHealth;
    public PlayerController playerController;
    public PlayerStamina playerStamina;

    void Start()
    {
        UpdateAllStats();
    }

    void Update()
    {
        UpdateAllStats();
    }

    private void UpdateDamage()
    {
        statsSlots[0].GetComponentInChildren<TextMeshProUGUI>().text = "Dmg: " + playerAttack.attackDamage;
    }

    private void UpdateArmor()
    {
        statsSlots[1].GetComponentInChildren<TextMeshProUGUI>().text = "Arm: " + playerHealth.currentArmor;
    }

    private void UpdateMoveSpeed()
    {
        statsSlots[2].GetComponentInChildren<TextMeshProUGUI>().text = "Speed: " + playerController.moveSpeed;
    }

    private void UpdateMaxHealth()
    {
        statsSlots[3].GetComponentInChildren<TextMeshProUGUI>().text = "MaxHP: " + playerHealth.maxHealth;
    }

    private void UpdateMaxStamina()
    {
        statsSlots[4].GetComponentInChildren<TextMeshProUGUI>().text = "MaxSta: " + playerStamina.maxStamina;
    }

    private void UpdateAllStats()
    {
        UpdateDamage();
        UpdateArmor();
        UpdateMoveSpeed();
        UpdateMaxHealth();
        UpdateMaxStamina();
    }
}