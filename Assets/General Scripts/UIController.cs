using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public PlayerStamina playerStamina;

    public Slider healthSlider;
    public Slider staminaSlider;
    void Start()
    {
        healthSlider.maxValue = playerHealth.maxHealth;
        staminaSlider.maxValue = playerStamina.maxStamina;

        healthSlider.value = playerHealth.currentHealth;
        staminaSlider.value = playerStamina.currentStamina;
    }

    void Update()
    {
        healthSlider.value = playerHealth.currentHealth;
        staminaSlider.value = playerStamina.currentStamina;
    }
}
