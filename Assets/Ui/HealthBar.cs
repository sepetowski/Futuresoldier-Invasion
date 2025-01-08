using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI healthText;

    public void SetMaxHealth(float maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = maxHealth;
        UpdateHealthText(maxHealth, maxHealth);
    }

    public void SetHealth(float health)
    {
        slider.value = health;
        UpdateHealthText(health, slider.maxValue);
    }

    private void UpdateHealthText(float currentHealth, float maxHealth)
    {
        healthText.text = $"{currentHealth} / {maxHealth}";
    }

}
