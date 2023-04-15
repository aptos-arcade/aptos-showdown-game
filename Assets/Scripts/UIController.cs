using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    
    public static UIController Instance;
    
    private void Awake()
    {
        Instance = this;
    }

    [Header("Heat References")]
    [SerializeField] private TMP_Text overheatedMessage;
    [SerializeField] private Slider heatSlider;
    
    [Header("Death References")]
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private TMP_Text deathText;
    
    [Header("Health References")]
    [SerializeField] private Slider healthSlider;

    public void SetOverheatedMessageActive(bool isOverheated)
    {
        overheatedMessage.gameObject.SetActive(isOverheated);
    }
    
    public void SetHeatSliderValue(float heat)
    {
        heatSlider.value = heat;
    }
    
    public void SetMaxHealthSliderValue(float maxHeat)
    {
        healthSlider.maxValue = maxHeat;
    }
    
    public void SetHealthSliderValue(float health)
    {
        healthSlider.value = health;
    }
    
    public void SetDeathScreenActive(bool isActive)
    {
        deathScreen.SetActive(isActive);
    }

    public void SetDeathText(string killerName)
    {
        deathText.text = $"You were killed by {killerName}";
    }
    
}
