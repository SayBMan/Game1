using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PlayerBarsUIController : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public PlayerStamina playerStamina;

    public Slider healthSlider;
    public Slider staminaSlider;

    public RectTransform healthFill; // Slider'ın Fill objesi
    public RectTransform staminaFill;
    public float widthPerHealth = 10f;
    public float widthPerStamina = 10f;

    private float lastMaxHealth;

    void Start()
    {
        lastMaxHealth = playerHealth.maxHealth;

        // Slider'ın çubuklarını ayarla
        healthFill.sizeDelta = new Vector2(playerHealth.maxHealth * widthPerHealth, healthFill.sizeDelta.y);
        staminaFill.sizeDelta = new Vector2(playerStamina.maxStamina * widthPerStamina, staminaFill.sizeDelta.y);

        // Başlangıçta değerleri ayarla
        healthSlider.maxValue = playerHealth.maxHealth;
        healthSlider.value = playerHealth.currentHealth;

        staminaSlider.maxValue = playerStamina.maxStamina;
        staminaSlider.value = playerStamina.currentStamina;
    }

    void Update()
    {
        healthSlider.value = playerHealth.currentHealth;

        // Stamina’yı güncelle
        staminaSlider.maxValue = playerStamina.maxStamina;
        staminaSlider.value = playerStamina.currentStamina;

        // Önce max value’yu güncelle
        if (playerHealth.maxHealth != lastMaxHealth)
        {
            healthSlider.maxValue = playerHealth.maxHealth;

            // Fiziksel genişliği sağa doğru büyüt
            float newWidth = playerHealth.maxHealth * widthPerHealth;
            healthFill.DOSizeDelta(
                new Vector2(newWidth, healthFill.sizeDelta.y),
                0.3f
            ).SetEase(Ease.OutQuad);

            lastMaxHealth = playerHealth.maxHealth;
        }
    }
}
