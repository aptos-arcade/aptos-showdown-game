using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;

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
    [SerializeField] private Button toFindRoomButton;
    [SerializeField] private Button toCreateRoomButton;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private Button roomTestButton;

    [Header("Create Room References")] 
    [SerializeField] private GameObject createRoomScreen;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button closeCreateRoomButton;

    [Header("Room References")] 
    [SerializeField] private GameObject roomScreen;
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerNameLabel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveRoomButton;

    [Header("Error References")] 
    [SerializeField] private GameObject errorScreen;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private Button errorCloseButton;

    [Header("Find Room References")] 
    [SerializeField] private GameObject findRoomScreen;
    [SerializeField] private GameObject roomButton;
    [SerializeField] private Button closeFindRoomButton;

    [Header("Name Input References")] 
    [SerializeField] private GameObject nameInputScreen;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button setNameButton;
    
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

    public int[] Levels { get; } = { Scenes.Map1BuildIndex, Scenes.Map2BuildIndex };


    // Start is called before the first frame update
    private void Start()
    {
        AddListeners();
        CloseMenus();

        loadingScreen.SetActive(true);
        loadingText.text = "Connecting to Network...";

        if(!PhotonNetwork.IsConnected) PhotonNetwork.ConnectUsingSettings();
        
        #if UNITY_EDITOR
            roomTestButton.gameObject.SetActive(true);
        #endif
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void AddListeners()
    {
        // set nickname screen
        setNameButton.onClick.AddListener(SetNickname);
        
        // main menu screen
        toFindRoomButton.onClick.AddListener(OpenRoomBrowser);
        toCreateRoomButton.onClick.AddListener(OpenRoomCreate);
        quitGameButton.onClick.AddListener(QuitGame);
        roomTestButton.onClick.AddListener(QuickJoin);
        
        // create room screen
        createRoomButton.onClick.AddListener(CreateRoom);
        closeCreateRoomButton.onClick.AddListener(BackToMenu);
        
        // error screen
        errorCloseButton.onClick.AddListener(BackToMenu);
        
        // find room screen
        closeFindRoomButton.onClick.AddListener(BackToMenu);
        
        // room screen
        leaveRoomButton.onClick.AddListener(LeaveRoom);
        startButton.onClick.AddListener(StartGame);
    }
    
    private void CloseMenus()
    {
        loadingScreen.SetActive(false);
        createRoomScreen.SetActive(false);
        menuButtons.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        findRoomScreen.SetActive(false);
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
        CloseMenus();
        createRoomScreen.SetActive(true);
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
        
        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
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

    private void BackToMenu()
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
        CloseMenus();
        findRoomScreen.SetActive(true);
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
        PhotonNetwork.LoadLevel(Levels[Random.Range(0, Levels.Length)]);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }
    
    public void QuickJoin()
    {
        PhotonNetwork.CreateRoom(TestRoomName, new RoomOptions {MaxPlayers = 8});
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Creating Room...";
    }
}
