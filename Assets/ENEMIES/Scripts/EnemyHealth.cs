using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth;
    public float currentHealth;
    private EnemyController enemyController;

    void Start()
    {
        enemyController = GetComponent<EnemyController>();
        currentHealth = maxHealth;
    }

    public void ChangeHealth(float amount)
    {
        float lastHealth = currentHealth;
        currentHealth += amount;
        float healthDiff = currentHealth - lastHealth;

        if (healthDiff < 0)
        {
            enemyController.GetHurt();
        }

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        else if (currentHealth <= 0)
        {
            enemyController.GetDeath();
        }
    }

}
