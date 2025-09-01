using System.Collections;
using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    public float maxStamina;
    public float currentStamina;
    [SerializeField] private float staminaRegen;
    [SerializeField] private float staminaRegenDelay;
    [SerializeField] private float staminaRegenTimer;
    public float dashStaminaCost;
    public float attackStaminaCost;
    public float sprintStaminaCost;

    private PlayerController playerController;

    void Start()
    {
        currentStamina = maxStamina;
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (staminaRegenTimer > 0)
        {
            staminaRegenTimer -= Time.deltaTime;
        }

        if (currentStamina > maxStamina) currentStamina = maxStamina;

        // Regen
        if (currentStamina < maxStamina && !playerController.isDashing && !playerController.isAttacking && !playerController.isSprinting && staminaRegenTimer <= 0)
        {
            currentStamina += staminaRegen * Time.deltaTime;
        }
    }

    public bool HasStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            return true;
        }
        else
        {
            return false;
        }

    }

    public void UseStamina(float amount)
    {
        currentStamina -= amount;
        if (currentStamina < 0) currentStamina = 0;

        staminaRegenTimer = staminaRegenDelay;
    }

    public void GetStamina(float amount)
    {
        currentStamina += amount;
        if (currentStamina > maxStamina) currentStamina = maxStamina;
    }

    public void ChangeMaxStamina(float amount)
    {
        maxStamina += amount;
        currentStamina = maxStamina;
    }
}
