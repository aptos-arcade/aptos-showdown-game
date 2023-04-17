using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class Launcher : MonoBehaviourPunCallbacks
{

    public static Launcher Instance;

    private void Awake()
    {
        Instance = this;
    }

    [Header("Loading References")] 
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private TMP_Text loadingText;

    [Header("Menu Buttons References")] 
    [SerializeField] private GameObject menuButtons;
    [SerializeField] private GameObject roomTestButton;

    [Header("Create Room References")] 
    [SerializeField] private GameObject findRoomScreen;
    [SerializeField] private TMP_InputField roomNameInput;

    [Header("Room References")] 
    [SerializeField] private GameObject roomScreen;
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerNameLabel;
    [SerializeField] private GameObject startButton;

    [Header("Error References")] 
    [SerializeField] private GameObject errorScreen;
    [SerializeField] private TMP_Text errorText;

    [Header("Room Browser References")] 
    [SerializeField] private GameObject roomBrowserScreen;
    [SerializeField] private GameObject roomButton;

    [Header("Name Input References")] 
    [SerializeField] private GameObject nameInputScreen;
    [SerializeField] private TMP_InputField nameInput;
    
    [Header("Configuration")]
    [SerializeField] private bool changeMapsBetweenRounds = true;
    public bool ChangeMapsBetweenRounds => changeMapsBetweenRounds;

    // private menu state
    private readonly List<RoomButton> _allRoomButtons = new();
    private readonly List<TMP_Text> _allPlayerLabels = new();
    private static bool _hasSetNickname;

    // constants
    private const string PlayerNameKey = "playerName";
    private const string TestRoomName = "TestRoom";

    private readonly int[] _levels = { Scenes.Map1BuildIndex, Scenes.Map2BuildIndex };
    public int[] Levels => _levels;
    

// Start is called before the first frame update
    private void Start()
    {
        CloseMenus();
        
        loadingScreen.SetActive(true);
        loadingText.text = "Connecting to Network...";

        if(!PhotonNetwork.IsConnected) PhotonNetwork.ConnectUsingSettings();
        
        #if UNITY_EDITOR
            roomTestButton.SetActive(true);
        #endif
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void CloseMenus()
    {
        loadingScreen.SetActive(false);
        findRoomScreen.SetActive(false);
        menuButtons.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
        nameInputScreen.SetActive(false);
    }
    
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
        loadingText.text = "Connecting to Lobby...";
    }
    
    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);
        
        PhotonNetwork.NickName = Random.Range(0, 1000).ToString();

        if (!_hasSetNickname)
        {
            CloseMenus();
            nameInputScreen.SetActive(true);
            if (PlayerPrefs.HasKey(PlayerNameKey))
            {
                nameInput.text = PlayerPrefs.GetString(PlayerNameKey);
            }
        }
        else
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString(PlayerNameKey);
        }
        
    }
    
    public void OpenRoomCreate()
    {
        findRoomScreen.SetActive(true);
    }
    
    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text)) return;
        RoomOptions roomOptions = new()
        {
            MaxPlayers = 8
        };
        PhotonNetwork.CreateRoom(roomNameInput.text, roomOptions);
        
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Creating Room...";
    }

    public override void OnJoinedRoom()
    {
        CloseMenus();
        roomScreen.SetActive(true);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        ListAllPlayers();
        
        startButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    private void ListAllPlayers()
    {
        foreach (var playerLabel in _allPlayerLabels)
        {
            Destroy(playerLabel.gameObject);
        }
        _allPlayerLabels.Clear();
        
        foreach (var player in PhotonNetwork.PlayerList)
        {
            OnPlayerEnteredRoom(player);
        }
    }

    public override void OnPlayerEnteredRoom(Player player)
    {
        var newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
        newPlayerLabel.text = player.NickName;
        newPlayerLabel.gameObject.SetActive(true);
        _allPlayerLabels.Add(newPlayerLabel);
    }
    
    public override void OnPlayerLeftRoom(Player player)
    {
        ListAllPlayers();
    }
    
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CloseMenus();
        errorScreen.SetActive(true);
        errorText.text = "Room Creation Failed: " + message;
    }
    
    public void CloseErrorScreen()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }
    
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Leaving Room...";
    }
    
    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void OpenRoomBrowser()
    {
        roomBrowserScreen.SetActive(true);
    }
    
    public void CloseRoomBrowser()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // Destroy all room buttons
        foreach (var button in _allRoomButtons)
        {
            Destroy(button.gameObject);
        }
        _allRoomButtons.Clear();
        
        // hide the room button template
        roomButton.SetActive(false);
        
        // create a new room button for each room
        foreach (var roomInfo in roomList)
        {
            if (roomInfo.PlayerCount == roomInfo.MaxPlayers || roomInfo.RemovedFromList) return;
            var newRoomButton = Instantiate(roomButton, roomButton.transform.parent);
            var roomButtonScript = newRoomButton.GetComponent<RoomButton>();
            roomButtonScript.SetRoomDetails(roomInfo);
            _allRoomButtons.Add(roomButtonScript);
            newRoomButton.SetActive(true);
        }
    }
    
    public void JoinRoom(RoomInfo roomInfo)
    {
        PhotonNetwork.JoinRoom(roomInfo.Name);
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Joining Room...";
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }

    public void SetNickname()
    {
        if (string.IsNullOrEmpty(nameInput.text)) return;
        PlayerPrefs.SetString(PlayerNameKey, nameInput.text);
        PhotonNetwork.NickName = nameInput.text;
        _hasSetNickname = true;
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(_levels[Random.Range(0, _levels.Length)]);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startButton.SetActive(PhotonNetwork.IsMasterClient);
    }
    
    public void QuickJoin()
    {
        PhotonNetwork.CreateRoom(TestRoomName, new RoomOptions {MaxPlayers = 8});
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Creating Room...";
    }
}
