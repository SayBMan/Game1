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
        statsSlots[0].GetComponentInChildren<TextMeshProUGUI>().text = "Damage: " + playerAttack.attackDamage;
    }

    private void UpdateArmor()
    {
        statsSlots[1].GetComponentInChildren<TextMeshProUGUI>().text = "Armor: " + playerHealth.currentArmor;
    }

    private void UpdateMoveSpeed()
    {
        statsSlots[2].GetComponentInChildren<TextMeshProUGUI>().text = "Speed: " + playerController.moveSpeed;
    }


    private void UpdateAllStats()
    {
        UpdateDamage();
        UpdateArmor();
        UpdateMoveSpeed();
    }
}