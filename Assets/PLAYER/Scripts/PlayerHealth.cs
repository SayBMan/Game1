using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth;
    public float currentHealth;
    private PlayerController playerController;
    void Start()
    {
        currentHealth = maxHealth;
        playerController = GetComponent<PlayerController>();
    }

    public void ChangeHealth(float amount)
    {
        float lastHealth = currentHealth;
        currentHealth += amount;
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
