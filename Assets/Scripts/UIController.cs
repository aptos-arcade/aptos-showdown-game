using System;
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
    
    [Header("KD References")]
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text deathsText;
    
    [Header("Leaderboard References")]
    [SerializeField] private GameObject leaderboardScreen;
    [SerializeField] private GameObject leaderboardPlayerInfo;

    [Header("Match Over References")]
    [SerializeField] private GameObject matchOverScreen;
    
    [Header("Timer References")]
    [SerializeField] private TMP_Text timerText;
    
    
    // properties
    public GameObject LeaderboardScreen => leaderboardScreen;
    public GameObject LeaderboardPlayerInfo => leaderboardPlayerInfo;
    
    

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
    
    public void SetKillsText(int kills)
    {
        killsText.text = $"Kills: {kills}";
    }
    
    public void SetDeathsText(int deaths)
    {
        deathsText.text = $"Deaths: {deaths}";
    }
    
    public void SetLeaderboardScreenActive(bool isActive)
    {
        leaderboardScreen.SetActive(isActive);
    }
    
    public void HideDefaultLeaderboardPlayerInfo()
    {
        leaderboardPlayerInfo.SetActive(false);
    }
    
    public void SetMatchOverScreenActive(bool isActive)
    {
        matchOverScreen.SetActive(isActive);
    }
    
    public void SetTimerActive(bool isActive)
    {
        timerText.gameObject.SetActive(isActive);
    }
    
    public void SetTimerText(float time)
    {
        timerText.text = TimeSpan.FromSeconds(time).ToString(@"mm\:ss");
    }
    
}
