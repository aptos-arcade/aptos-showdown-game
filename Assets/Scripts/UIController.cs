using System;
using System.Collections;
using Photon.Pun;
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
    [SerializeField] private GameObject hurtEffect;
    [SerializeField] private float hurtScreenDuration = 0.5f;

    [Header("KD References")]
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text deathsText;
    
    [Header("Leaderboard References")]
    [SerializeField] private GameObject leaderboardScreen;
    [SerializeField] private GameObject leaderboardPlayerInfo;

    [Header("Match Over References")]
    [SerializeField] private GameObject matchOverScreen;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Timer References")]
    [SerializeField] private TMP_Text timerText;
    
    [Header("Options Menu References")]
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Toggle invertYAxisToggle;
    [SerializeField] private Button optionsResumeButton;
    [SerializeField] private Button optionsMainMenuButton;
    [SerializeField] private Button optionsQuitButton;

    // properties
    public GameObject LeaderboardScreen => leaderboardScreen;
    public GameObject LeaderboardPlayerInfo => leaderboardPlayerInfo;
    public bool IsOptionsMenuActive => optionsMenu.activeSelf;

    private void Start()
    {
        AddListeners();
        
        invertYAxisToggle.onValueChanged.AddListener(isOn => PlayerPreferences.Instance.InvertLook = isOn);
        invertYAxisToggle.isOn = PlayerPreferences.Instance.InvertLook;
        
        mouseSensitivitySlider.onValueChanged.AddListener(value => PlayerPreferences.Instance.MouseSensitivity = value);
        mouseSensitivitySlider.value = PlayerPreferences.Instance.MouseSensitivity;
    }

    private void AddListeners()
    {
        // end screen
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        
        // options menu
        optionsResumeButton.onClick.AddListener(ShowHideOptions);
        optionsMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        optionsQuitButton.onClick.AddListener(QuitGame);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowHideOptions();
        }

        if (!IsOptionsMenuActive || Cursor.lockState == CursorLockMode.None) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

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
        killsText.text = $"Kills: {kills}/{MatchManager.Instance.KillsToWin}";
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
    
    public void SetHurtEffectActive(bool isActive)
    {
        hurtEffect.SetActive(isActive);
    }
    
    public IEnumerator ShowDamageCoroutine()
    {
        SetHurtEffectActive(true);
        yield return new WaitForSeconds(hurtScreenDuration);
        SetHurtEffectActive(false);
    }

    public void ShowHideOptions()
    {
        optionsMenu.SetActive(!IsOptionsMenuActive);
    }
    
    public void ReturnToMainMenu()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
}
