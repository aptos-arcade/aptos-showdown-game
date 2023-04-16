using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MatchManager Instance;
    
    private void Awake()
    {
        Instance = this;
    }

    private enum EventCodes : byte
    {
        NewPlayer,
        ListPlayers,
        UpdateStat
    }
    
    // player info
    private readonly List<PlayerInfo> _playerInfos = new();
    private int _localPlayerIndex;
    
    // leaderboard
    private readonly List<LeaderboardPlayer> _leaderboardPlayers = new();

    // Start is called before the first frame update
    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(Scenes.MainMenuBuildIndex);
            return;
        }
        NewPlayerSend(PhotonNetwork.NickName);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;
        if(UIController.Instance.LeaderboardScreen.activeSelf)
            UIController.Instance.LeaderboardScreen.SetActive(false);
        else
            UpdateLeaderboard();
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;
        var eventCode = (EventCodes)photonEvent.Code;
        var data = (object[])photonEvent.CustomData;
        
        Debug.Log("Received event: " + eventCode);
        
        switch (eventCode)
        {
            case EventCodes.NewPlayer:
                NewPlayerReceive(data);
                break;
            case EventCodes.ListPlayers:
                ListPlayersReceive(data);
                break;
            case EventCodes.UpdateStat:
                UpdateStatReceive(data);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }
    
    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void NewPlayerSend(string username)
    {
        object[] package = {username, PhotonNetwork.LocalPlayer.ActorNumber, 0, 0};
        PhotonNetwork.RaiseEvent((byte)EventCodes.NewPlayer, package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
    }
    
    private void NewPlayerReceive(object[] data)
    {
        var playerInfo = new PlayerInfo((string)data[0], (int)data[1], (int)data[2], (int)data[3]);
        _playerInfos.Add(playerInfo);
        ListPlayersSend();
    }

    private void ListPlayersSend()
    {
        var package = new object[_playerInfos.Count];
        for (var i = 0; i < _playerInfos.Count; i++)
        {
            package[i] = new object[]
            {
                _playerInfos[i].Name, 
                _playerInfos[i].ActorNumber, 
                _playerInfos[i].Kills,
                _playerInfos[i].Deaths
            };
        }
        PhotonNetwork.RaiseEvent((byte)EventCodes.ListPlayers, package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
    }
    
    private void ListPlayersReceive(object[] data)
    {
        _playerInfos.Clear();
        foreach (object[] playerInfoData in data)
        {
            var playerInfo = new PlayerInfo(
                (string)playerInfoData[0],
                (int)playerInfoData[1],
                (int)playerInfoData[2],
                (int)playerInfoData[3]
            );
            _playerInfos.Add(playerInfo);
            if(playerInfo.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                _localPlayerIndex = _playerInfos.Count - 1;
        }
    }
    
    public void UpdateStatSend(int actorSending, int statToUpdate, int changeAmount)
    {
        object[] package = {actorSending, statToUpdate, changeAmount};
        PhotonNetwork.RaiseEvent((byte)EventCodes.UpdateStat, package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
    }

    private void UpdateStatReceive(object[] data)
    {
        var actorSending = (int)data[0];
        var statToUpdate = (int)data[1];
        var changeAmount = (int)data[2];
        var playerIndex = _playerInfos.FindIndex(playerInfo => playerInfo.ActorNumber == actorSending);
        if (playerIndex == -1) return;
        switch (statToUpdate)
        {
            case 0:
                _playerInfos[playerIndex].Kills += changeAmount;
                break;
            case 1:
                _playerInfos[playerIndex].Deaths += changeAmount;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        UpdateStatsDisplay();
        if(UIController.Instance.LeaderboardScreen.activeSelf) UpdateLeaderboard();
    }

    private void UpdateStatsDisplay()
    {
        if (_playerInfos.Count > _localPlayerIndex)
        {
            UIController.Instance.SetKillsText(_playerInfos[_localPlayerIndex].Kills);
            UIController.Instance.SetDeathsText(_playerInfos[_localPlayerIndex].Deaths);
        }
        else
        {
            UIController.Instance.SetKillsText(0);
            UIController.Instance.SetDeathsText(0);
        }
    }

    private void UpdateLeaderboard()
    {
        UIController.Instance.SetLeaderboardScreenActive(true);
        foreach (var leaderboardPlayer in _leaderboardPlayers)
        {
            Destroy(leaderboardPlayer.gameObject);
        }
        _leaderboardPlayers.Clear();
        
        UIController.Instance.HideDefaultLeaderboardPlayerInfo();
        
        var sortedPlayerInfos = SortPlayerInfos(_playerInfos);

        foreach (var playerInfo in sortedPlayerInfos)
        {
            var leaderboardPlayer = Instantiate(UIController.Instance.LeaderboardPlayerInfo,
                UIController.Instance.LeaderboardPlayerInfo.transform.parent);
            var leaderboardPlayerScript = leaderboardPlayer.GetComponent<LeaderboardPlayer>();
            leaderboardPlayerScript.SetPlayerDetails(playerInfo.Name, playerInfo.Kills, playerInfo.Deaths);
            leaderboardPlayer.SetActive(true);
            _leaderboardPlayers.Add(leaderboardPlayerScript);
        }
    }

    private static List<PlayerInfo> SortPlayerInfos(List<PlayerInfo> playerInfos)
    {
        playerInfos.Sort((playerInfo1, playerInfo2) => playerInfo1.Kills == playerInfo2.Kills
            ? playerInfo1.Deaths.CompareTo(playerInfo2.Deaths)
            : playerInfo2.Kills.CompareTo(playerInfo1.Kills));
        return playerInfos;
    }
}

    [Serializable]
public class PlayerInfo
{
    public string Name { get; set; }
    public int ActorNumber { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    
    public PlayerInfo(string name, int actorNumber, int kills, int deaths)
    {
        Name = name;
        ActorNumber = actorNumber;
        Kills = kills;
        Deaths = deaths;
    }
}