using UnityEngine;
using TMPro;

public class LeaderboardPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerKillsText;
    [SerializeField] private TMP_Text playerDeathsText;
    
    public void SetPlayerDetails(string playerName, int playerKills, int playerDeaths)
    {
        playerNameText.text = playerName;
        playerKillsText.text = playerKills.ToString();
        playerDeathsText.text = playerDeaths.ToString();
    }
}
