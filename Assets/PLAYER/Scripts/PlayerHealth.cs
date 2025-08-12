using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth;
    public float currentHealth;
    public float maxArmor;
    public float currentArmor;
    private PlayerController playerController;
    void Start()
    {
        currentHealth = maxHealth;
        playerController = GetComponent<PlayerController>();
    }

    public void ChangeHealth(float amount)
    {
        float lastHealth = currentHealth;
        if (amount < 0)
        {
            currentHealth += amount - amount * (currentArmor / 100);
        }
        else
        {
            currentHealth += amount;
        }
        
        float healthDiff = currentHealth - lastHealth;

        if (healthDiff < 0)
        {
            playerController.GetHurt();
        }

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        else if (currentHealth <= 0)
        {
            playerController.GetDeath();
        }
    }

}
