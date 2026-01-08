using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class healthBar : MonoBehaviour 
{
    public Slider healthSlider;
    private PlayerMovementQ playerHealth;
    public Image fillImage; 

    private Color normalColor = Color.red; 
    private Color immortalColor = Color.yellow; 

    void Start()
    {
        playerHealth = FindObjectOfType<PlayerMovementQ>();
        healthSlider.maxValue = playerHealth.maxHealth;
        healthSlider.value = playerHealth.currentHealth;
        

        if (fillImage == null)
        {
            fillImage = healthSlider.fillRect.GetComponent<Image>();
        }
    }

    void Update()
    {
        healthSlider.value = playerHealth.currentHealth;
        
        if (playerHealth.maxHealth == 9999) 
        {
            fillImage.color = immortalColor;
        }
        else
        {
            fillImage.color = normalColor;
        }
    }
}
