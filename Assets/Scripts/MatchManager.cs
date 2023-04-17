using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using Random = UnityEngine.Random;

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
        UpdateStat,
        NextMatch,
        TimerSync
    }

    public enum GameState
    {
        Waiting,
        Playing,
        MatchOver
    }

    [SerializeField] private int killsToWin = 3;
    [SerializeField] private Transform endCameraPoint;
    [SerializeField] private float waitAfterMatchOver = 5f;
    [SerializeField] private float maxMatchTime = 180f;

    // game state
    private GameState _gameState = GameState.Waiting;
    private float _currentMatchTime;
    private float _timerSyncTime;

    // player info
    private readonly List<PlayerInfo> _playerInfos = new();
    private int _localPlayerIndex;
    public bool perpetualMatch;

    // leaderboard
    private readonly List<LeaderboardPlayer> _leaderboardPlayers = new();
    
    // properties
    public Transform EndCameraPoint => endCameraPoint;
    public GameState CurrentGameState => _gameState;


    // Start is called before the first frame update
    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(Scenes.MainMenuBuildIndex);
            return;
        }

        NewPlayerSend(PhotonNetwork.NickName);
        _gameState = GameState.Playing;
        SetupTimer();

        if (!PhotonNetwork.IsMasterClient)
        {
            UIController.Instance.SetTimerActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && _gameState != GameState.MatchOver)
        {
            if (UIController.Instance.LeaderboardScreen.activeSelf)
                UIController.Instance.LeaderboardScreen.SetActive(false);
            else
                UpdateLeaderboard();
        }

        if (!PhotonNetwork.IsMasterClient || !(_gameState == GameState.Playing && _currentMatchTime > 0)) return;
        _currentMatchTime -= Time.deltaTime;
        if (_currentMatchTime <= 0)
        {
            _currentMatchTime = 0;
            _gameState = GameState.MatchOver;
            if (PhotonNetwork.IsMasterClient)
            {
                ListPlayersSend();
                StateCheck();
            }
        }
        UpdateTimerDisplay();
        _timerSyncTime -= Time.deltaTime;
        if (!(_timerSyncTime <= 0)) return;
        _timerSyncTime += 1f;
        TimerSyncSend();
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
            case EventCodes.NextMatch:
                NextMatchReceive();
                break;
            case EventCodes.TimerSync:
                TimerSyncReceive(data);
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
        object[] package = { username, PhotonNetwork.LocalPlayer.ActorNumber, 0, 0 };
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
        var package = new object[_playerInfos.Count + 1];
        package[0] = _gameState;
        for (var i = 0; i < _playerInfos.Count; i++)
        {
            package[i + 1] = new object[]
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
        _gameState = (GameState)data[0];
        foreach (object[] playerInfoData in data.Skip(1))
        {
            var playerInfo = new PlayerInfo(
                (string)playerInfoData[0],
                (int)playerInfoData[1],
                (int)playerInfoData[2],
                (int)playerInfoData[3]
            );
            _playerInfos.Add(playerInfo);
            if (playerInfo.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                _localPlayerIndex = _playerInfos.Count - 1;
        }
        StateCheck();
    }

    public void UpdateStatSend(int actorSending, int statToUpdate, int changeAmount)
    {
        object[] package = { actorSending, statToUpdate, changeAmount };
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
        if (UIController.Instance.LeaderboardScreen.activeSelf) UpdateLeaderboard();
        ScoreCheck();
    }
    
    private void NextMatchSend()
    {
        PhotonNetwork.RaiseEvent((byte)EventCodes.NextMatch, null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
    }
    
    private void NextMatchReceive()
    {
        _gameState = GameState.Playing;
        UIController.Instance.SetMatchOverScreenActive(false);
        UIController.Instance.SetLeaderboardScreenActive(false);
        foreach (var playerInfo in _playerInfos)
        {
            playerInfo.Kills = 0;
            playerInfo.Deaths = 0;
        }
        UpdateStatsDisplay();
        PlayerSpawner.Instance.SpawnPlayer();
        SetupTimer();
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

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(Scenes.MainMenuBuildIndex);
    }

    private void ScoreCheck()
    {
        var winnerFound = _playerInfos.Any(playerInfo => playerInfo.Kills >= killsToWin);
        if (!winnerFound || !PhotonNetwork.IsMasterClient || _gameState == GameState.MatchOver) return;
        _gameState = GameState.MatchOver;
        ListPlayersSend();
    }

    private void StateCheck()
    {
        if(_gameState == GameState.MatchOver) EndGame();
    }

    private void EndGame()
    {
        _gameState = GameState.MatchOver;
        if(PhotonNetwork.IsMasterClient) PhotonNetwork.DestroyAll();
        UIController.Instance.SetMatchOverScreenActive(true);
        UpdateLeaderboard();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (Camera.main != null)
        {
            Camera.main.transform.position = endCameraPoint.position;
            Camera.main.transform.rotation = endCameraPoint.rotation;
        }

        StartCoroutine(EndCoroutine());
    }

    private IEnumerator EndCoroutine()
    {
        yield return new WaitForSeconds(waitAfterMatchOver);
        if (!perpetualMatch)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
        else if(PhotonNetwork.IsMasterClient)
        {
            if (!Launcher.Instance.ChangeMapsBetweenRounds)
            {
                NextMatchSend();
            }
            else
            {
                var newLevel = Random.Range(0, Launcher.Instance.Levels.Length);
                if (Launcher.Instance.Levels[newLevel] == SceneManager.GetActiveScene().buildIndex)
                {
                    NextMatchSend();
                }
                else
                {
                    PhotonNetwork.LoadLevel(Launcher.Instance.Levels[newLevel]);
                }
            }
        }
    }
    
    private void SetupTimer()
    {
        if (!(maxMatchTime > 0)) return;
        _currentMatchTime = maxMatchTime;
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        UIController.Instance.SetTimerText(_currentMatchTime);
    }
    
    private void TimerSyncSend()
    {
        var package = new object[] { (int)_currentMatchTime, _gameState };
        PhotonNetwork.RaiseEvent((byte)EventCodes.TimerSync, package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
    }
    
    private void TimerSyncReceive(object[] data)
    {
        _currentMatchTime = (int)data[0];
        _gameState = (GameState)data[1];
        UpdateTimerDisplay();
        UIController.Instance.SetTimerActive(true);
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